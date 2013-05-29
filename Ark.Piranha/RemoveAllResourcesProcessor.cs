using Ark.Cecil;
using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Piranha {
    public class RemoveAllResourcesProcessor : CecilProcessor {
        public override void ProcessResources(ModuleDefinition moduleDef, IList<Resource> resources) {
            resources.Clear();
            base.ProcessResources(moduleDef, resources);
        }
    }
}
