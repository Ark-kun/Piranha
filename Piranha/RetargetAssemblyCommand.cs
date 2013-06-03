using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class RetargetAssemblyCommand : CommonCommand {
        [Option('p', "profile", Required = true, HelpText = "Framework profile.")]
        public string Profile { get; set; }

        [Option("remove-other-references", HelpText = "Remove references to libraries not in the framework profile.")]
        public bool RemoveOtherReferences { get; set; }

        public override void Execute() {
            new RetargetAssemblyProcessor(Profile, RemoveOtherReferences).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
