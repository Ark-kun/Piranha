using Ark.Cecil;
using Ark.Piranha;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Piranha {
    class Program {
        static void Main(string[] args) {
            Trace.Listeners.Add(new ConsoleTraceListener(true));
            if (!CommandLine.Parser.Default.ParseArguments(args, new PiranhaCommands(),
                (v, o) => {
                    var command = o as CommonCommand;
                    if (command != null) {
                        command.Execute();
                    }
                })) {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }
        }
    }
}
