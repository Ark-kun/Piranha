using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class MakeSkeletonCommand : CommonCommand {
        [Option("disable-breaking-verification", HelpText = "Disable operations that can make the resulting assembly unverifiable.")]
        public bool DisableBreakingVerification { get; set; }

        public override void Execute() {
            new MakeSkeletonProcessor(DisableBreakingVerification).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
