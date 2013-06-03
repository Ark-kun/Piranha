using Ark.Cecil;
using Mono.Cecil;

namespace Ark.Piranha {
    public class MakeSkeletonProcessor : CecilProcessor {
        bool _enableBreakingVerification;

        public MakeSkeletonProcessor(bool enableBreakingVerification) {
            _enableBreakingVerification = enableBreakingVerification;
        }

        public override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            new EnsureParameterlessConstructorsProcessor().ProcessAssembly(assemblyDef);
            new RemoveMethodBodiesProcessor().ProcessAssembly(assemblyDef);
            new RemovePrivateMembersProcessor(!_enableBreakingVerification).ProcessAssembly(assemblyDef);
            new RemovePrivateTypesProcessor().ProcessAssembly(assemblyDef);
        }
    }
}
