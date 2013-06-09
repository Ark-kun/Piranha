using Ark.Cecil;
using Ark.Linq;
using Ark.DotNet;
using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ark.Piranha {
    public class RemoveExternalTypesUsageProcessor : CecilProcessor {
        DefaultAssemblyResolver _assemblyResolver = new DefaultAssemblyResolver();
        FrameworkProfile _frameworkProfile;
        bool _removeNonRetargetable;

        public RemoveExternalTypesUsageProcessor(bool removeNonRetargetable = false) {
            _removeNonRetargetable = removeNonRetargetable;
        }

        public RemoveExternalTypesUsageProcessor(FrameworkProfile frameworkProfile, bool removeNonRetargetable = false) {
            _frameworkProfile = frameworkProfile;
            _removeNonRetargetable = removeNonRetargetable;
        }

        protected override ReaderParameters GetDefaultReaderParameters() {
            return new ReaderParameters() { MetadataResolver = new ReferenceSearchingMetadataResolver(_assemblyResolver) };
        }

        public override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            if (_frameworkProfile == null) {
                _frameworkProfile = assemblyDef.GuessAssemblyProfile();
            }
            if (_frameworkProfile != null) {
                _assemblyResolver.AddSearchDirectory(_frameworkProfile.ReferencesDirectory);
            }
            var typesDependenciesCollector = new CollectTypesDependenciesProcessor(_frameworkProfile);
            typesDependenciesCollector.ProcessAssembly(assemblyDef);

            var goodAssemblyNames = assemblyDef.Modules.SelectMany(asmDef => asmDef.AssemblyReferences);
            if(_removeNonRetargetable) {
                goodAssemblyNames = goodAssemblyNames.Where(asmRef => asmRef.IsRetargetable);
            }
            if (_frameworkProfile != null) {
                goodAssemblyNames = goodAssemblyNames.Concat(_frameworkProfile.GetFrameworkAssemblies());
            }

            var goodModules = new HashSet<ModuleDefinition>(CecilEqualityComparer.Default);
            goodModules.AddRange(assemblyDef.Modules);
            goodModules.AddRange(goodAssemblyNames.Select(_assemblyResolver.TryResolve).Where(asmDef => asmDef != null).SelectMany(asmDef => asmDef.Modules));

            var allTypesDependencies = typesDependenciesCollector.AllTypesDependencies;
            var typeDependenciesToRemove = new Queue<TypeReferenceAndDependencies>(allTypesDependencies.Where(
                kv => {
                    var typeRef = kv.Key;
                    var typeDef = typeRef.TryResolve();
                    return typeDef == null || !goodModules.Contains(typeDef.Module);
                }).Select(kv => (TypeReferenceAndDependencies)kv));

            var removedDependencies = new HashSet<TypeDependency>();
            while (typeDependenciesToRemove.Any()) {
                var typeDependencies = typeDependenciesToRemove.Dequeue();
                var typeRef = typeDependencies.Type;
                var dependencies = typeDependencies.DependingMembers;
                Trace.WriteLine(string.Format("Removing dependencies on type {0}:", typeRef), "RemoveExternalTypesUsage");
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
            }

            base.ProcessAssembly(assemblyDef);
        }
    }
}
