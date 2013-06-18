using Ark.Piranha;
using System.IO;

namespace Piranha {
    class ListApiCommand : CommonCommand {
        public override void Execute() {
            var processor = new ListApiProcessor();
            processor.ProcessAssemblyFromFile(Input, null);
            using (var writer = File.CreateText(Output)) {
                processor.Dump(writer);
            }
        }
    }
}
