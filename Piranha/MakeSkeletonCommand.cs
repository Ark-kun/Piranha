using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class MakeSkeletonCommand : CommonCommand {
        [Option("enable-breaking-verification", HelpText = "Enabling operations that can make the resulting assembly unverifiable.")]
        public bool EnableBreakingVerification { get; set; }

        public override void Execute() {
            new MakeSkeletonProcessor(EnableBreakingVerification).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
