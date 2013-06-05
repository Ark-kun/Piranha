using Ark.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Ark.Piranha {
    public class CollectUsedTypesProcessor : CecilProcessor {
        HashSet<TypeReference> _usedTypeReferences = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);
        HashSet<TypeDefinition> _usedTypes;
        HashSet<TypeReference> _unresolvedTypes;

        public ISet<TypeDefinition> UsedTypes {
            get { return _usedTypes; }
        }

        public ISet<TypeReference> UnresolvedTypes {
            get { return _unresolvedTypes; }
        }

        public void DumpToFile(string fileName) {
            using (var usedTypesWriter = File.CreateText(fileName)) {

                foreach (string fullTypeName in _usedTypes.Select(typeDef => "[" + (typeDef.Module == null ? "?" : typeDef.Module.Assembly.FullName) + "]" + typeDef.FullName).OrderBy(tn => tn).Distinct()) {
                    usedTypesWriter.WriteLine(fullTypeName);
                }
                foreach (string fullTypeName in _unresolvedTypes.Select(typeRef => "{" + (typeRef.Scope == null ? "?" : typeRef.Scope.ToString()) + "}" + typeRef.FullName).OrderBy(tn => tn).Distinct()) {
                    usedTypesWriter.WriteLine(fullTypeName);
                }
            }
        }

        public override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            var targetFrameworkAttribute = assemblyDef.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");
            if (targetFrameworkAttribute != null) {
                var frameworkName = (string)targetFrameworkAttribute.ConstructorArguments.First().Value;
                var frameworkProfile = Ark.DotNet.FrameworkProfile.Parse(frameworkName);
                foreach (var moduleDef in assemblyDef.Modules) {
                    var resolver = moduleDef.AssemblyResolver as DefaultAssemblyResolver;
                    if (resolver != null) {
                        resolver.AddSearchDirectory(frameworkProfile.ReferencesDirectory);
                    }
                }
            }

            base.ProcessAssembly(assemblyDef);

            var processedTypes = new HashSet<TypeDefinition>();
            var unresolvedTypes = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);
            var unprocessedTypes = new Queue<TypeReference>(_usedTypeReferences);

            while (unprocessedTypes.Any()) {
                var typeRef = unprocessedTypes.Dequeue();

                if (typeRef == null) {
                    continue;
                }

                if (typeRef.IsGenericParameter) {
                    continue;
                }

                var typeSpec = typeRef as TypeSpecification;
                if (typeSpec != null) {
                    var elementType = typeSpec.ElementType;
                    Debug.Assert(elementType != null);
                    unprocessedTypes.Enqueue(elementType);

                    var genericInstanceTypeRef = typeRef as GenericInstanceType;
                    if (genericInstanceTypeRef != null) {
                        foreach (var genericArgument in genericInstanceTypeRef.GenericArguments) {
                            unprocessedTypes.Enqueue(genericArgument);
                        }
                    }

                    var requiredModifierTypeRef = typeRef as RequiredModifierType;
                    if (requiredModifierTypeRef != null) {
                        unprocessedTypes.Enqueue(requiredModifierTypeRef.ModifierType);
                    }

                    var optionalModifierTypeRef = typeRef as OptionalModifierType;
                    if (optionalModifierTypeRef != null) {
                        unprocessedTypes.Enqueue(optionalModifierTypeRef.ModifierType);
                    }

                    var functionPointerTypeRef = typeRef as FunctionPointerType;
                    if (functionPointerTypeRef != null) {
                        unprocessedTypes.Enqueue(functionPointerTypeRef.ReturnType);
                        foreach (var parameter in functionPointerTypeRef.Parameters) {
                            unprocessedTypes.Equals(parameter.ParameterType);
                            foreach (var customAttr in parameter.CustomAttributes) {
                                unprocessedTypes.Enqueue(customAttr.AttributeType);
                            }
                        }
                        foreach (var customAttr in functionPointerTypeRef.MethodReturnType.CustomAttributes) {
                            unprocessedTypes.Enqueue(customAttr.AttributeType);
                        }
                    }
                    continue;
                }

                var typeDef = typeRef as TypeDefinition;
                if (typeDef == null) {
                    typeDef = typeRef.TryResolve();
                    if (typeDef != null) {
                        unprocessedTypes.Enqueue(typeDef);
                    } else {
                        unresolvedTypes.Add(typeRef);
                        Debug.WriteLine(string.Format("Cannot resolve type {0}", typeRef.FullName));
                    }
                    continue;
                }

                processedTypes.Add(typeDef);
            }
            _usedTypes = processedTypes;
            _unresolvedTypes = unresolvedTypes;
        }

        public override void ProcessType(TypeDefinition typeDef) {
            _usedTypeReferences.Add(typeDef);
            if (typeDef.BaseType != null) {
                _usedTypeReferences.Add(typeDef.BaseType);
            }
            foreach (var interfaceRef in typeDef.Interfaces) {
                _usedTypeReferences.Add(interfaceRef);
            }
            base.ProcessType(typeDef);
        }

        public override void ProcessField(FieldDefinition fieldDef) {
            _usedTypeReferences.Add(fieldDef.FieldType);
            base.ProcessField(fieldDef);
        }

        public override void ProcessMethod(MethodDefinition methodDef) {
            _usedTypeReferences.Add(methodDef.ReturnType);
            foreach (var parameter in methodDef.Parameters) {
                _usedTypeReferences.Add(parameter.ParameterType);
            }
            if (methodDef.HasBody) {
                var body = methodDef.Body;
                foreach (var variable in body.Variables) {
                    _usedTypeReferences.Add(variable.VariableType);
                }
                foreach (var instruction in body.Instructions) {
                    if (instruction.OpCode == OpCodes.Newobj) {
                        var newObjTypeRef = ((MemberReference)instruction.Operand).DeclaringType;
                        _usedTypeReferences.Add(newObjTypeRef);
                    }
                    if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli || instruction.OpCode == OpCodes.Callvirt) {
                        var callMethodRef = instruction.Operand as MethodReference;
                        _usedTypeReferences.Add(callMethodRef.DeclaringType);
                        //TODO: Process method signature.
                    }
                }
            }
            base.ProcessMethod(methodDef);
        }

        public override void ProcessCustomAttribute(CustomAttribute attribute, IMetadataTokenProvider owner) {
            _usedTypeReferences.Add(attribute.AttributeType);
            base.ProcessCustomAttribute(attribute, owner);
        }
    }
}
