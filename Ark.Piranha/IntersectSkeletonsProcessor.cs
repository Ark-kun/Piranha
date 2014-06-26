using Ark.Cecil;
using Ark.Linq;
using Ark.DotNet;
using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ark.Piranha {
    public class IntersectSkeletonsProcessor : CecilProcessor {
        DefaultAssemblyResolver _assemblyResolver = new FrameworkAssemblyResolver();
        IEnumerable<AssemblyDefinition> _intersectingAssemblies;
        Dictionary<TypeReference, HashSet<TypeDependency>> _typesDependencies;

        HashSet<TypeDefinition> _typesToRemove = new HashSet<TypeDefinition>(CecilEqualityComparer.Default);

        public IntersectSkeletonsProcessor(IEnumerable<AssemblyDefinition> intersectingAssemblies) {
            _intersectingAssemblies = intersectingAssemblies;
        }

        protected override ReaderParameters GetDefaultReaderParameters() {
            return new ReaderParameters() { MetadataResolver = new ReferenceSearchingMetadataResolver(_assemblyResolver) };
        }

        protected override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            base.ProcessAssembly(assemblyDef);

            var typesDependenciesCollector = new CollectTypesDependenciesProcessor();
            typesDependenciesCollector.Process(assemblyDef);
            _typesDependencies = typesDependenciesCollector.AllTypesDependencies;

            var allTypesDependencies = typesDependenciesCollector.AllTypesDependencies;
            var typeDependenciesToRemove = new Queue<TypeReferenceAndDependencies>(allTypesDependencies.Where(kv => _typesToRemove.Contains(kv.Key)).Select(kv => (TypeReferenceAndDependencies)kv));

            var removedDependencies = new HashSet<TypeDependency>();
            while (typeDependenciesToRemove.Any()) {
                var typeDependencies = typeDependenciesToRemove.Dequeue();
                var typeRef = typeDependencies.Type;
                var dependencies = typeDependencies.DependingMembers;
                Trace.WriteLine(string.Format("Removing dependencies on type {0}:", typeRef), "IntersectSkeletons");
                Trace.Indent();
                foreach (var dependency in dependencies) {
                    if (!removedDependencies.Contains(dependency)) {
                        dependency.Break();
                        removedDependencies.Add(dependency);

                        var baseClassDependency = dependency as BaseClassDependency;
                        if (baseClassDependency != null) {
                            var removedClass = baseClassDependency.DerivedClass;
                            if (allTypesDependencies.ContainsKey(removedClass)) {
                                var removedClassDependencies = allTypesDependencies[removedClass];
                                typeDependenciesToRemove.Enqueue(new TypeReferenceAndDependencies(removedClass, removedClassDependencies));
                            }
                        }
                    }
                }

                var typeDef = typeRef.TryResolve(); ;
                Trace.WriteLine(string.Format("Removing type {0}, because it doesn't exist in one of the assemblies.", typeRef), "IntersectSkeletons");
                if (typeDef.IsNested) {
                    typeDef.DeclaringType.NestedTypes.Remove(typeDef);
                } else {
                    typeDef.Module.Types.Remove(typeDef);
                }
            }
        }

        protected override void ProcessEvent(EventDefinition eventDef) {
            if (!_intersectingAssemblies.All(
                assembly => assembly.Modules.Any(
                   module => module.TryResolve(eventDef) != null
                ))) {
                Trace.WriteLine(string.Format("Removing event {0}, because it doesn't exist in one of the assemblies.", eventDef), "IntersectSkeletons");
                eventDef.DeclaringType.Events.Remove(eventDef);
                return;
            }
            base.ProcessEvent(eventDef);
        }

        protected override void ProcessProperty(PropertyDefinition propertyDef) {
            if (!_intersectingAssemblies.All(
                assembly => assembly.Modules.Any(
                   module => module.TryResolve(propertyDef) != null
                ))) {
                Trace.WriteLine(string.Format("Removing property {0}, because it doesn't exist in one of the assemblies.", propertyDef), "IntersectSkeletons");
                propertyDef.DeclaringType.Properties.Remove(propertyDef);
                return;
            }
            base.ProcessProperty(propertyDef);
        }

        protected override void ProcessMethod(MethodDefinition methodDef) {
            if (!_intersectingAssemblies.All(
                assembly => assembly.Modules.Any(
                   module => module.TryResolve(methodDef) != null
                ))) {
                Trace.WriteLine(string.Format("Removing method {0}, because it doesn't exist in one of the assemblies.", methodDef), "IntersectSkeletons");
                methodDef.DeclaringType.Methods.Remove(methodDef);
                return;
            }
            base.ProcessMethod(methodDef);
        }

        protected override void ProcessField(FieldDefinition fieldDef) {
            if (!_intersectingAssemblies.All(
                assembly => assembly.Modules.Any(
                   module => module.TryResolve(fieldDef) != null
                ))) {
                Trace.WriteLine(string.Format("Removing field {0}, because it doesn't exist in one of the assemblies.", fieldDef), "IntersectSkeletons");
                fieldDef.DeclaringType.Fields.Remove(fieldDef);
                return;
            }
            base.ProcessField(fieldDef);
        }

        protected override void ProcessType(TypeDefinition typeDef) {
            if (!_intersectingAssemblies.All(
                assembly => assembly.Modules.Any(
                    module => module.TryResolve(typeDef) != null
                ))) {
                _typesToRemove.Add(typeDef);
                return;
            }
            base.ProcessType(typeDef);
        }
    }
}