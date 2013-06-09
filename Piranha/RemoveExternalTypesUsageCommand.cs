using Ark.DotNet;
using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RemoveExternalTypesUsageCommand : CommonCommand {
        [Option('p', "profile", HelpText = "Framework profile.")]
        public string Profile { get; set; }

        public override void Execute() {
            var frameworkProfile = Profile != null ? FrameworkProfile.Parse(Profile) : null;
            new RemoveExternalTypesUsageProcessor(frameworkProfile).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
