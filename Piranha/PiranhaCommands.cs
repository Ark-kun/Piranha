using CommandLine;
using CommandLine.Text;

namespace Piranha {
    class PiranhaCommands {
        [VerbOption("remove-all-references", HelpText = "Remove all references from the assembly")]
        public RemoveAllReferencesOptions RemoveAllReferences { get; set; }

        [HelpVerbOption]
        public string GetUsage(string verb) {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
