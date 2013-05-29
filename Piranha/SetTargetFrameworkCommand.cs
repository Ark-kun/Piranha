using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class SetTargetFrameworkCommand : CommonCommand {
        [Option('p', "profile", HelpText = "Framework profile.")]
        public string Profile { get; set; }

        public override void Execute() {
            new SetTargetFrameworkProcessor(Profile).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
