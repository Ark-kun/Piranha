using Ark.Cecil;
using Mono.Cecil;

namespace Ark.Piranha {
    public class MakeSkeletonProcessor : CecilProcessor {
        bool _disableBreakingVerification;

        public MakeSkeletonProcessor(bool disableBreakingVerification = false) {
            _disableBreakingVerification = disableBreakingVerification;
        }

        protected override void ProcessAssembly(AssemblyDefinition assemblyDef) {
            new RemoveAllResourcesProcessor().Process(assemblyDef);
            new EnsureParameterlessConstructorsProcessor().Process(assemblyDef);
            new RemoveMethodBodiesProcessor().Process(assemblyDef);
            new RemovePrivateMembersProcessor(_disableBreakingVerification, false).Process(assemblyDef);
            new RemovePrivateTypesProcessor().Process(assemblyDef);
        }
    }
}
