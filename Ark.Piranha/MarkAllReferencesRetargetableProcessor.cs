using Mono.Cecil;

namespace Ark.Cecil {
    public class MarkAllReferencesRetargetableProcessor : CecilProcessor {
        public override void ProcessAssemblyReference(AssemblyNameReference assemblyNameRef) {
            assemblyNameRef.IsRetargetable = true;
            base.ProcessAssemblyReference(assemblyNameRef);
        }
    }
}
