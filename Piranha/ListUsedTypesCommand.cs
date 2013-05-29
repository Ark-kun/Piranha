using Ark.Piranha;

namespace Piranha {
    class ListUsedTypesCommand : CommonCommand {
        public override void Execute() {
            var processor = new CollectUsedTypesProcessor();
            processor.ProcessAssemblyFromFile(Input, null);
            processor.DumpToFile(Output);
        }
    }
}
