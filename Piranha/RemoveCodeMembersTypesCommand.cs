using Ark.Piranha;
using CommandLine;
using Mono.Cecil;
using System.IO;

namespace Piranha {
    class RemoveCodeMembersTypesCommand : CommonCommand {
        public override void Execute() {
            string inputFileBase = Path.Combine(Path.GetDirectoryName(Input), Path.GetFileNameWithoutExtension(Input));
            bool hasSymbols = File.Exists(inputFileBase + ".pdb") || File.Exists(inputFileBase + ".mdb");
            Stream inputStream = File.OpenRead(Input);
            Stream outputStream = File.Create(Output);
            var readerParams = new ReaderParameters() { ReadSymbols = hasSymbols };
            var writerParams = new WriterParameters() { WriteSymbols = hasSymbols };

            var assemblyDef = AssemblyDefinition.ReadAssembly(inputStream, readerParams);

            var codeRemover = new RemoveMethodBodiesProcessor();
            codeRemover.ProcessAssembly(assemblyDef);

            var membersRemover = new RemovePrivateMembersProcessor(true) { MethodsToPreserve = codeRemover.UsedConstructors };
            membersRemover.ProcessAssembly(assemblyDef);

            new RemovePrivateTypesProcessor().ProcessAssembly(assemblyDef);

            assemblyDef.Write(outputStream, writerParams);
        }
    }
}
