using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Ark.Piranha {
    public class CollectUsedTypesProcessor : CecilProcessor {
        HashSet<TypeReference> _usedTypeReferences;
        HashSet<TypeDefinition> _usedTypes;
        HashSet<TypeReference> _unresolvedTypes;
        FrameworkProfile _frameworkProfile;

        public CollectUsedTypesProcessor() { }

        public CollectUsedTypesProcessor(FrameworkProfile frameworkProfile) {
            _frameworkProfile = frameworkProfile;
        }

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
            if (_frameworkProfile == null) {
                _frameworkProfile = assemblyDef.GuessAssemblyProfile();
            }
            if (_frameworkProfile != null) {
                foreach (var moduleDef in assemblyDef.Modules) {
                    var resolver = moduleDef.AssemblyResolver as DefaultAssemblyResolver;
                    if (resolver != null) {
                        resolver.AddSearchDirectory(_frameworkProfile.ReferencesDirectory);
                    }
                }
            }

            _usedTypeReferences = new HashSet<TypeReference>(CecilEqualityComparer.Default);
            base.ProcessAssembly(assemblyDef);
            var unprocessedTypes = new Queue<TypeReference>(_usedTypeReferences);
            _usedTypeReferences = null;

            var processedTypes = new HashSet<TypeDefinition>();
            var unresolvedTypes = new HashSet<TypeReference>(CecilEqualityComparer.Default);

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

        void ProcessFoundType(TypeReference typeRef) {
            _usedTypeReferences.Add(typeRef);
        }

        public override void ProcessExportedType(ExportedType exportedType) {
            var exportedTypeDef = exportedType.TryResolve();
            if (exportedTypeDef != null) {
                ProcessFoundType(exportedTypeDef);
            } else {
                Trace.WriteLine(string.Format("Strange: Couldn't resolve the exported type {0}.", exportedType), "CollectUsedTypes");
            }
            base.ProcessExportedType(exportedType);
        }

        public override void ProcessType(TypeDefinition typeDef) {
            ProcessFoundType(typeDef);
            if (typeDef.BaseType != null) {
                ProcessFoundType(typeDef.BaseType);
            }
            foreach (var interfaceRef in typeDef.Interfaces) {
                ProcessFoundType(interfaceRef);
            }
            base.ProcessType(typeDef);
        }

        public override void ProcessField(FieldDefinition fieldDef) {
            ProcessFoundType(fieldDef.FieldType);
            base.ProcessField(fieldDef);
        }

        public override void ProcessProperty(PropertyDefinition propertyDef) {
            ProcessFoundType(propertyDef.PropertyType);
            base.ProcessProperty(propertyDef);
        }

        public override void ProcessEvent(EventDefinition eventDef) {
            ProcessFoundType(eventDef.EventType);
            base.ProcessEvent(eventDef);
        }

        public override void ProcessMethod(MethodDefinition methodDef) {
            ProcessFoundType(methodDef.ReturnType);
            foreach (var parameter in methodDef.Parameters) {
                ProcessFoundType(parameter.ParameterType);
            }
            if (methodDef.HasBody) {
                var body = methodDef.Body;
                foreach (var variable in body.Variables) {
                    ProcessFoundType(variable.VariableType);
                }
                foreach (var instruction in body.Instructions) {
                    if (instruction.OpCode == OpCodes.Newobj) {
                        var newObjTypeRef = ((MemberReference)instruction.Operand).DeclaringType;
                        ProcessFoundType(newObjTypeRef);
                    }
                    if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli || instruction.OpCode == OpCodes.Callvirt) {
                        var callMethodRef = instruction.Operand as MethodReference;
                        ProcessFoundType(callMethodRef.DeclaringType);
                        //TODO: Process method signature.
                    }
                }
            }
            base.ProcessMethod(methodDef);
        }

        public override void ProcessCustomAttribute(CustomAttribute attribute, ICustomAttributeProvider owner) {
            ProcessFoundType(attribute.AttributeType);
            base.ProcessCustomAttribute(attribute, owner);
        }
    }
}
