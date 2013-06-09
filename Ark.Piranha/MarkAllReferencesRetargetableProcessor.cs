using Ark.Cecil;
using Mono.Cecil;

namespace Ark.Piranha {
    public class MarkAllReferencesRetargetableProcessor : CecilProcessor {
        protected override void ProcessAssemblyReference(AssemblyNameReference assemblyNameRef) {
            assemblyNameRef.IsRetargetable = true;
            base.ProcessAssemblyReference(assemblyNameRef);
        }
    }
}
