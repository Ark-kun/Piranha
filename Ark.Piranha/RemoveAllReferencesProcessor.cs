using Ark.Cecil;
using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Piranha {
    public class RemoveAllReferencesProcessor : CecilProcessor {
        public override void ProcessAssemblyReferences(ModuleDefinition moduleDef, IList<AssemblyNameReference> assemblyNameRefs) {
            assemblyNameRefs.Clear();
            base.ProcessAssemblyReferences(moduleDef, assemblyNameRefs);
        }
        
        public override void ProcessModuleReferences(ModuleDefinition moduleDef, IList<ModuleReference> moduleRefs) {
            moduleRefs.Clear();
            base.ProcessModuleReferences(moduleDef, moduleRefs);
        }
    }
}
