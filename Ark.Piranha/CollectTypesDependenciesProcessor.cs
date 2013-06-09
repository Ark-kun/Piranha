using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ark.Piranha {
    /// <summary>
    /// For each type this processor collects a set of dependencies representing members that depend on that type.
    /// <remarks>
    /// The modopt and modreq types are recorded as the dependency of the member that uses the modifiers. Thus, the dependent member is removed when the dependency is broken (instead of removing the modifier from the modified type).
    /// </remarks>
    /// </summary>
    public class CollectTypesDependenciesProcessor : CecilProcessor {
        Dictionary<TypeReference, HashSet<TypeDependency>> _usedTypeReferences;
        Dictionary<TypeDefinition, HashSet<TypeDependency>> _resolvedTypesDependencies;
        Dictionary<TypeReference, HashSet<TypeDependency>> _unresolvedTypesDependencies;
        Dictionary<TypeReference, HashSet<TypeDependency>> _allTypesDependencies;
        FrameworkProfile _frameworkProfile;

        public CollectTypesDependenciesProcessor() { }

        public CollectTypesDependenciesProcessor(FrameworkProfile frameworkProfile) {
            _frameworkProfile = frameworkProfile;
        }

        public Dictionary<TypeDefinition, HashSet<TypeDependency>> ResolvedTypesDependencies {
            get { return _resolvedTypesDependencies; }
        }

        public Dictionary<TypeReference, HashSet<TypeDependency>> UnresolvedTypesDependencies {
            get { return _unresolvedTypesDependencies; }
        }

        public Dictionary<TypeReference, HashSet<TypeDependency>> AllTypesDependencies {
            get { return _allTypesDependencies; }
        }

        protected override void ProcessAssembly(AssemblyDefinition assemblyDef) {
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

            _usedTypeReferences = new Dictionary<TypeReference, HashSet<TypeDependency>>(CecilEqualityComparer.Default);
            base.ProcessAssembly(assemblyDef);
            var unprocessedTypes = new Queue<TypeReferenceAndDependencies>(_usedTypeReferences.Select(kv => (TypeReferenceAndDependencies)kv));
            _usedTypeReferences = null;

            var processedTypes = new Dictionary<TypeDefinition, HashSet<TypeDependency>>(CecilEqualityComparer.Default);
            var unresolvedTypes = new Dictionary<TypeReference, HashSet<TypeDependency>>(CecilEqualityComparer.Default);

            while (unprocessedTypes.Any()) {
                var typeDependencies = unprocessedTypes.Dequeue();
                var typeRef = typeDependencies.Type;
                var dependentMembers = typeDependencies.DependingMembers;

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
                    unprocessedTypes.Enqueue(new TypeReferenceAndDependencies(elementType, dependentMembers));

                    var genericInstanceTypeRef = typeRef as GenericInstanceType;
                    if (genericInstanceTypeRef != null) {
                        foreach (var genericArgument in genericInstanceTypeRef.GenericArguments) {
                            unprocessedTypes.Enqueue(new TypeReferenceAndDependencies(genericArgument, dependentMembers));
                        }
                    }

                    var requiredModifierTypeRef = typeRef as RequiredModifierType;
                    if (requiredModifierTypeRef != null) {
                        unprocessedTypes.Enqueue(new TypeReferenceAndDependencies(requiredModifierTypeRef.ModifierType, dependentMembers));
                    }

                    var optionalModifierTypeRef = typeRef as OptionalModifierType;
                    if (optionalModifierTypeRef != null) {
                        unprocessedTypes.Enqueue(new TypeReferenceAndDependencies(optionalModifierTypeRef.ModifierType, dependentMembers));
                    }

                    var functionPointerTypeRef = typeRef as FunctionPointerType;
                    if (functionPointerTypeRef != null) {
                        unprocessedTypes.Enqueue(new TypeReferenceAndDependencies(functionPointerTypeRef.ReturnType, dependentMembers));
                        foreach (var parameter in functionPointerTypeRef.Parameters) {
                            unprocessedTypes.Equals(parameter.ParameterType);
                            foreach (var customAttr in parameter.CustomAttributes) {
                                unprocessedTypes.Enqueue(new TypeReferenceAndDependencies(customAttr.AttributeType, dependentMembers));
                            }
                        }
                        foreach (var customAttr in functionPointerTypeRef.MethodReturnType.CustomAttributes) {
                            unprocessedTypes.Enqueue(new TypeReferenceAndDependencies(customAttr.AttributeType, dependentMembers));
                        }
                    }
                    continue;
                }

                var typeDef = typeRef as TypeDefinition;
                if (typeDef == null) {
                    typeDef = typeRef.TryResolve();
                    if (typeDef != null) {
                        unprocessedTypes.Enqueue(new TypeReferenceAndDependencies(typeDef, dependentMembers));
                    } else {
                        AddDependencies(unresolvedTypes, typeDependencies);
                        Trace.WriteLine(string.Format("Warning: Couldn't resolve type {0}", typeRef.FullName), "CollectTypesDependencies");
                    }
                    continue;
                }

                AddDependencies(processedTypes, new TypeDefinitionAndDependencies(typeDef, dependentMembers));
            }
            _resolvedTypesDependencies = processedTypes;
            _unresolvedTypesDependencies = unresolvedTypes;

            _allTypesDependencies = new Dictionary<TypeReference, HashSet<TypeDependency>>(_unresolvedTypesDependencies, CecilEqualityComparer.Default);
            foreach (var resolvedTypeDependencies in _resolvedTypesDependencies) {
                _allTypesDependencies.Add(resolvedTypeDependencies.Key, resolvedTypeDependencies.Value);
            }
        }

        void AddDependencies(Dictionary<TypeReference, HashSet<TypeDependency>> storage, TypeReferenceAndDependencies addition) {
            var typeRef = addition.Type;
            HashSet<TypeDependency> members = null;
            if (!storage.TryGetValue(typeRef, out members)) {
                members = new HashSet<TypeDependency>();
                storage.Add(typeRef, members);
            }
            var addedMembers = addition.DependingMembers;
            members.UnionWith(addedMembers);
        }

        void AddDependencies(Dictionary<TypeDefinition, HashSet<TypeDependency>> storage, TypeDefinitionAndDependencies addition) {
            var typeDef = addition.Type;
            HashSet<TypeDependency> members = null;
            if (!storage.TryGetValue(typeDef, out members)) {
                members = new HashSet<TypeDependency>();
                storage.Add(typeDef, members);
            }
            var addedMembers = addition.DependingMembers;
            members.UnionWith(addedMembers);
        }

        void AddDependency(TypeReference dependentTypeRef, TypeDependency dependingMember) {
            HashSet<TypeDependency> members = null;
            if (!_usedTypeReferences.TryGetValue(dependentTypeRef, out members)) {
                members = new HashSet<TypeDependency>();
                _usedTypeReferences.Add(dependentTypeRef, members);
            }
            members.Add(dependingMember);
        }

        protected override void ProcessExportedType(ExportedType exportedType) {
            var exportedTypeDef = exportedType.TryResolve();
            if (exportedTypeDef != null) {
                AddDependency(exportedTypeDef, new ExportedTypeDependency(exportedType, exportedTypeDef.Module));
            } else {
                Trace.WriteLine(string.Format("Strange: Couldn't resolve the exported type {0}.", exportedType), "CollectTypesDependencies");
            }

            base.ProcessExportedType(exportedType);
        }

        protected override void ProcessType(TypeDefinition typeDef) {
            //ProcessFoundType(typeDef, typeDef); //? type self-dependency?
            if (typeDef.BaseType != null) {
                AddDependency(typeDef.BaseType, new BaseClassDependency(typeDef));
            }
            foreach (var interfaceRef in typeDef.Interfaces) {
                AddDependency(interfaceRef, new InterfaceDependency(typeDef, interfaceRef));
            }
            base.ProcessType(typeDef);
        }

        protected override void ProcessField(FieldDefinition fieldDef) {
            AddDependency(fieldDef.FieldType, new FieldDependency(fieldDef));
            base.ProcessField(fieldDef);
        }

        protected override void ProcessProperty(PropertyDefinition propertyDef) {
            AddDependency(propertyDef.PropertyType, new PropertyDependency(propertyDef));
            base.ProcessProperty(propertyDef);
        }

        protected override void ProcessEvent(EventDefinition eventDef) {
            AddDependency(eventDef.EventType, new EventDependency(eventDef));
            base.ProcessEvent(eventDef);
        }

        protected override void ProcessMethod(MethodDefinition methodDef) {
            AddDependency(methodDef.ReturnType, new MethodDependency(methodDef));
            foreach (var parameter in methodDef.Parameters) {
                AddDependency(parameter.ParameterType, new MethodDependency(methodDef));
            }
            if (methodDef.HasBody) {
                var body = methodDef.Body;
                foreach (var variable in body.Variables) {
                    AddDependency(variable.VariableType, new MethodDependency(methodDef));
                }
                foreach (var instruction in body.Instructions) {
                    if (instruction.OpCode == OpCodes.Newobj) {
                        var newObjTypeRef = ((MemberReference)instruction.Operand).DeclaringType;
                        AddDependency(newObjTypeRef, new MethodDependency(methodDef));
                    }
                    if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Calli || instruction.OpCode == OpCodes.Callvirt) {
                        var callMethodRef = instruction.Operand as MethodReference;
                        AddDependency(callMethodRef.DeclaringType, new MethodDependency(methodDef));
                        //TODO: Process method signature.
                    }
                }
            }
            base.ProcessMethod(methodDef);
        }


        protected override void ProcessCustomAttribute(CustomAttribute attribute, ICustomAttributeProvider owner) {
            AddDependency(attribute.AttributeType, new AttributeDependency(owner, attribute));
            base.ProcessCustomAttribute(attribute, owner);
        }
    }
}
