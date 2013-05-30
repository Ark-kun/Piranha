using Ark.Cecil;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Piranha {
    public class RetargetReferencesProcessor : CecilProcessor {
        IDictionary<string, AssemblyNameReference> _assemblyReplacements;
        bool _removeOthers;

        public RetargetReferencesProcessor(IEnumerable<AssemblyNameReference> assemblyReplacements, bool removeOthers = false)
            : this(assemblyReplacements.ToDictionary(asm => asm.Name, asm => asm), removeOthers) {
        }

        public RetargetReferencesProcessor(IDictionary<string, AssemblyNameReference> assemblyReplacements, bool removeOthers = false) {
            _assemblyReplacements = assemblyReplacements;
            _removeOthers = removeOthers;
        }

        public override void ProcessAssemblyReferences(ModuleDefinition moduleDef, IList<AssemblyNameReference> assemblyNameRefs) {
            for (int i = assemblyNameRefs.Count - 1; i >= 0; --i) {
                AssemblyNameReference replacement = null;
                if (_assemblyReplacements.TryGetValue(assemblyNameRefs[i].Name, out replacement)) {
                    assemblyNameRefs[i] = replacement;
                    //assemblyNameRefs.RemoveAt(i);
                    //assemblyNameRefs.Add(replacement);
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
