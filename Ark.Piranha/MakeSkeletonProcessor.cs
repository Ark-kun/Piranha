using Ark.Cecil;
using Mono.Cecil;

namespace Ark.Piranha {
    public class MakeSkeletonProcessor : CecilProcessor {
        bool _enableBreakingVerification;

        public MakeSkeletonProcessor(bool enableBreakingVerification = false) {
            _enableBreakingVerification = enableBreakingVerification;
        }

        protected override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            new RemoveAllResourcesProcessor().Process(assemblyDef);
            new EnsureParameterlessConstructorsProcessor().Process(assemblyDef);
            new RemoveMethodBodiesProcessor().Process(assemblyDef);
            new RemovePrivateMembersProcessor(!_enableBreakingVerification).Process(assemblyDef);
            new RemovePrivateTypesProcessor().Process(assemblyDef);
        }
    }
}
