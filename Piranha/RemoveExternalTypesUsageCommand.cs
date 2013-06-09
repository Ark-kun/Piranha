using Ark.DotNet;
using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RemoveExternalTypesUsageCommand : CommonCommand {
        [Option('p', "profile", HelpText = "Framework profile.")]
        public string Profile { get; set; }

        [Option('r', "remove-non-retargetable", HelpText = "Remove from referenced non-retargetable assemblies.")]
        public bool RemoveNonRetargetable { get; set; }

        public override void Execute() {
            var frameworkProfile = Profile != null ? FrameworkProfile.Parse(Profile) : null;
            new RemoveExternalTypesUsageProcessor(frameworkProfile, RemoveNonRetargetable).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
