using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piranha {
    class CommonOptions {
        [Option('i', "input", HelpText = "Input assembly file." )]
        public string Input { get; set; }

        [Option('o', "output", HelpText = "Output assembly file.")]
        public string Output { get; set; }
    }
}
