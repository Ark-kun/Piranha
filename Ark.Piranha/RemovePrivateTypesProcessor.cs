using Ark.Cecil;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Piranha {
    public class RemovePrivateTypesProcessor : CecilProcessor {
        protected override void ProcessModuleTypes(ModuleDefinition moduleDef, IList<TypeDefinition> typeDefs) {
            foreach (var typeDef in typeDefs.ToList()) {
                if (!typeDef.IsPublic && typeDef.FullName != "<Module>") {
                    moduleDef.Types.Remove(typeDef);
                }
            }
            base.ProcessModuleTypes(moduleDef, typeDefs);
        }

        protected override void ProcessNestedTypes(TypeDefinition typeDef, IList<TypeDefinition> typeDefs) {
            foreach (var nestedTypeDef in typeDefs.ToList()) {
                if (!(nestedTypeDef.IsNestedPublic || nestedTypeDef.IsNestedFamily || nestedTypeDef.IsNestedFamilyOrAssembly)) {
                    typeDef.NestedTypes.Remove(nestedTypeDef);
                }
            }
            base.ProcessNestedTypes(typeDef, typeDefs);
        }
    }
}
