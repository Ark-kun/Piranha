using Ark.Cecil;
using Ark.DotNet;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Piranha {
    public class RetargetReferencesProcessor : CecilProcessor {
        IDictionary<string, AssemblyNameReference> _assemblyReplacements;
        bool _removeOthers;

        public RetargetReferencesProcessor(FrameworkProfile frameworkProfile, bool removeOthers = false)
            : this(frameworkProfile.GetFrameworkAssemblies(), removeOthers) {
        }

        public RetargetReferencesProcessor(IEnumerable<AssemblyNameReference> assemblyReplacements, bool removeOthers = false)
            : this(assemblyReplacements.ToDictionary(asm => asm.Name, asm => asm), removeOthers) {
        }

        public RetargetReferencesProcessor(IDictionary<string, AssemblyNameReference> assemblyReplacements, bool removeOthers = false) {
            _assemblyReplacements = assemblyReplacements;
            _removeOthers = removeOthers;
        }

        protected override void ProcessAssemblyReferences(ModuleDefinition moduleDef, IList<AssemblyNameReference> assemblyNameRefs) {
            for (int i = assemblyNameRefs.Count - 1; i >= 0; --i) {
                AssemblyNameReference replacement = null;
                if (_assemblyReplacements.TryGetValue(assemblyNameRefs[i].Name, out replacement)) {
                    assemblyNameRefs[i].Version = replacement.Version;
                    assemblyNameRefs[i].PublicKeyToken = replacement.PublicKeyToken;
                    assemblyNameRefs[i].IsRetargetable = replacement.IsRetargetable;
                } else {
                    if (_removeOthers) {
                        assemblyNameRefs.RemoveAt(i);
                    }
                }
            }
            base.ProcessAssemblyReferences(moduleDef, assemblyNameRefs);
        }
    }
}
