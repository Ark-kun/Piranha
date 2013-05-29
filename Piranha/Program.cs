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
            var commands = new PiranhaCommands();
            string verb = null;
            CommonOptions options = null;
            if (!CommandLine.Parser.Default.ParseArguments(args, commands,
                    (v, o) => {
                        verb = v;
                        options = (CommonOptions)o;
                    }
                )) {
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }
            if (verb == PiranhaCommands.RemoveAllReferencesVerb) {
                new RemoveAllReferencesProcessor().ProcessAssemblyFromFile(options.Input, options.Output);
            }
            if (verb == PiranhaCommands.MarkAllReferencesRetargetableVerb) {
                new MarkAllReferencesRetargetableProcessor().ProcessAssemblyFromFile(options.Input, options.Output);
            }
        }

        static void OldMain(string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("Usage: piranha.exe <library>");
                Environment.Exit(1);
            }
            string inputFileName = args[0];
            //string inputFileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string inputFileBase = Path.Combine(Path.GetDirectoryName(inputFileName), Path.GetFileNameWithoutExtension(inputFileName));
            string outputFileBase = inputFileBase + ".skeleton";
            string outputFileName = outputFileBase + ".skeleton.dll";

            var inputStream = File.OpenRead(inputFileName);
            var outputStream = File.Create(outputFileName);

            bool useSymbols = File.Exists(inputFileBase + ".pdb") || File.Exists(inputFileBase + ".mdb");
            var assemblyDef = AssemblyDefinition.ReadAssembly(inputStream, new ReaderParameters() { ReadSymbols = useSymbols });
            //assemblyDef.Name.Name += " (Skeleton)";

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 0);

            var usedConstructors = new HashSet<MethodReference>(MethodReferenceEqualityComparer.Default);

            //Step 1: Removing all bodies.
            var codeRemover = new RemoveMethodBodiesProcessor();
            codeRemover.ProcessAssembly(assemblyDef);

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 1);

            //Step 2 and 3: Removing all private members
            var membersRemover = new RemovePrivateMembersProcessor(true) { MethodsToPreserve = codeRemover.UsedConstructors };
            membersRemover.ProcessAssembly(assemblyDef);

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 3);

            //Step 4: Removing all private types            
            new RemovePrivateTypesProcessor().ProcessAssembly(assemblyDef);

            DumpAssemblyAndUsageLists(assemblyDef, outputFileBase, 4);

            assemblyDef.Write(outputStream);
        }

        static void DumpAssemblyAndUsageLists(AssemblyDefinition assemblyDef, string fileNameBase, int step) {
            var oldName = assemblyDef.Name.Name;
            assemblyDef.Name.Name += " " + step.ToString();
            assemblyDef.Write(fileNameBase + "." + step.ToString() + ".dll");
            assemblyDef.Name.Name = oldName;

            var usedTypesCollector = new CollectUsedTypesProcessor();
            usedTypesCollector.ProcessAssembly(assemblyDef);
            usedTypesCollector.DumpToFile(fileNameBase + ".usedTypes." + step.ToString() + ".txt");
        }
    }
}
