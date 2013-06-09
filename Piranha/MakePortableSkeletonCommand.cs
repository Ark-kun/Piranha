using Ark.DotNet;
using Ark.Piranha;
using CommandLine;

namespace Piranha {
    class MakePortableSkeletonCommand : CommonCommand {
        [Option('p', "profile", Required = true, HelpText = "Framework profile.")]
        public string Profile { get; set; }

        public override void Execute() {
            var profile = FrameworkProfile.Parse(Profile);
            new MakePortableSkeletonProcessor(profile).ProcessAssemblyFromFile(Input, Output);
        }
    }
}
