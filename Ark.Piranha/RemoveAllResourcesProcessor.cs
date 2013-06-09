using Ark.Cecil;
using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Piranha {
    public class RemoveAllResourcesProcessor : CecilProcessor {
        protected override void ProcessResources(ModuleDefinition moduleDef, IList<Resource> resources) {
            resources.Clear();
            base.ProcessResources(moduleDef, resources);
        }
    }
}
