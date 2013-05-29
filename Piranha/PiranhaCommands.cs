using CommandLine;
using CommandLine.Text;

namespace Piranha {
    class PiranhaCommands {
        public const string RemoveAllReferencesVerb = "remove-all-references";

        [VerbOption(RemoveAllReferencesVerb, HelpText = "Remove all references from the assembly")]
        public CommonOptions RemoveAllReferences { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb) {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
