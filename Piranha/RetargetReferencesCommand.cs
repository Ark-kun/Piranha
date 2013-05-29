using Ark.DotNet;
using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RetargetReferencesCommand : CommonCommand {
        [Option('p', "profile", Required = true, HelpText = "Framework profile.")]
        public string Profile { get; set; }

        [Option("remove-others", HelpText = "Remove references to libraries not in the framework profile.")]
        public bool RemoveOthers { get; set; }

        public override void Execute() {
            var profile = FrameworkProfile.Parse(Profile);
            new RetargetReferencesProcessor(profile.GetFrameworkAssemblies(), RemoveOthers).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
