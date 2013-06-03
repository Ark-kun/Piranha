using Ark.Cecil;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ark.Piranha {
    public class CollectUsedTypesProcessor : CecilProcessor {
        HashSet<TypeReference> _usedTypes = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);

        public ISet<TypeReference> UsedTypes {
            get { return _usedTypes; }
        }

        public IEnumerable<TypeReference> UnresolvedTypes {
            get { return _usedTypes.Where(type => type.TryResolve() == null); }
        }

        public void DumpToFile(string fileName) {
            using (var usedTypesWriter = File.CreateText(fileName)) {
                foreach (string fullTypeName in _usedTypes.Select(typeRef => (typeRef.TryResolve() != null ? "[" + (typeRef.Module == null ? "?" : typeRef.Module.Assembly.FullName) + "]" : "{" + (typeRef.Scope == null ? "?" : typeRef.Scope.ToString()) + "}") + typeRef.FullName).OrderBy(tn => tn).Distinct()) {
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

            var processedTypes = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);
            var unprocessedTypes = _usedTypes;
            do {
                var newTypes = new HashSet<TypeReference>(TypeReferenceEqualityComparer.Default);

                //Removing  generic parameter types
                unprocessedTypes.RemoveWhere(type => type.IsGenericParameter);

                //Replacing the type references with the resolved type definitions
                var notResolvedTypes = (
                        from unresolvedType in unprocessedTypes
                        let resolvedType = unresolvedType.TryResolve()
                        where resolvedType != null && resolvedType != unresolvedType
                        select new { UnresolvedType = unresolvedType, ResolvedType = resolvedType }
                    ).ToList();
                foreach (var notResolvedTypePair in notResolvedTypes) {
                    unprocessedTypes.Remove(notResolvedTypePair.UnresolvedType);
                    newTypes.Add(notResolvedTypePair.ResolvedType);
                }

                //Replacing array types with element types
                var arrays = unprocessedTypes.Where(type => type.IsArray).ToList();
                foreach (var array in arrays) {
                    unprocessedTypes.Remove(array);
                    newTypes.Add(array.GetElementType());
                }

                //Removing generic type instances and adding their generic types and arguments
                var genericInstances = unprocessedTypes.Where(type => type.IsGenericInstance).ToList();
                foreach (GenericInstanceType genericInstance in genericInstances) {
                    unprocessedTypes.Remove(genericInstance);
                    var genericType = genericInstance.TryResolve();
                    if (genericType != null) {
                        newTypes.Add(genericType);
                    } else {
                        System.Diagnostics.Debug.WriteLine(string.Format("Strange: Generic instance type {0} cannot be resolved.", genericInstance));
                    }
                    foreach (var genericArgument in genericInstance.GenericArguments) {
                        newTypes.Add(genericArgument);
                    }
                }

                processedTypes.UnionWith(unprocessedTypes);
                newTypes.ExceptWith(processedTypes);
                unprocessedTypes = newTypes;
            } while (unprocessedTypes.Count > 0);
            _usedTypes = processedTypes;
        }

        public override void ProcessType(TypeDefinition typeDef) {
            _usedTypes.Add(typeDef);
            if (typeDef.BaseType != null) {
                _usedTypes.Add(typeDef.BaseType);
            }
            foreach(var interfaceRef in typeDef.Interfaces) {
                _usedTypes.Add(interfaceRef);
            }
            base.ProcessType(typeDef);
        }

        public override void ProcessField(FieldDefinition fieldDef) {
            _usedTypes.Add(fieldDef.FieldType);
            base.ProcessField(fieldDef);
        }

        public override void ProcessMethod(MethodDefinition methodDef) {
            _usedTypes.Add(methodDef.ReturnType);
            foreach (var parameter in methodDef.Parameters) {
                _usedTypes.Add(parameter.ParameterType);
            }
            if (methodDef.HasBody) {
                var body = methodDef.Body;
                foreach (var variable in body.Variables) {
                    _usedTypes.Add(variable.VariableType);
                }
                foreach (var instruction in body.Instructions) {
                    if (instruction.OpCode == OpCodes.Newobj) {
                        var newObjTypeRef = ((MemberReference)instruction.Operand).DeclaringType;
                        _usedTypes.Add(newObjTypeRef);
                    }
                    if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli || instruction.OpCode == OpCodes.Callvirt) {
                        var callMethodRef = instruction.Operand as MethodReference;
                        _usedTypes.Add(callMethodRef.DeclaringType);
                    }
                }
            }
            base.ProcessMethod(methodDef);
        }

        public override void ProcessCustomAssemblyAttribute(CustomAttribute attribute) {
            _usedTypes.Add(attribute.AttributeType);
            base.ProcessCustomAssemblyAttribute(attribute);
        }

        public override void ProcessCustomModuleAttribute(CustomAttribute attribute) {
            _usedTypes.Add(attribute.AttributeType);
            base.ProcessCustomModuleAttribute(attribute);
        }

        public override void ProcessCustomTypeAttribute(CustomAttribute attribute) {
            _usedTypes.Add(attribute.AttributeType);
            base.ProcessCustomTypeAttribute(attribute);
        }

        public override void ProcessCustomEventAttribute(CustomAttribute attribute) {
            _usedTypes.Add(attribute.AttributeType);
            base.ProcessCustomEventAttribute(attribute);
        }

        public override void ProcessCustomFieldAttribute(CustomAttribute attribute) {
            _usedTypes.Add(attribute.AttributeType);
            base.ProcessCustomFieldAttribute(attribute);
        }

        public override void ProcessCustomPropertyAttribute(CustomAttribute attribute) {
            _usedTypes.Add(attribute.AttributeType);
            base.ProcessCustomPropertyAttribute(attribute);
        }

        public override void ProcessCustomMethodAttribute(CustomAttribute attribute) {
            _usedTypes.Add(attribute.AttributeType);
            base.ProcessCustomMethodAttribute(attribute);
        }
    }
}
