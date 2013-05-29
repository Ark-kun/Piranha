using Ark.Cecil;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Piranha {
    public class RemovePrivateTypesProcessor : CecilProcessor {

        public override void ProcessModuleTypes(ModuleDefinition moduleDef, IList<TypeDefinition> typeDefs) {
            foreach (var typeDef in typeDefs.ToList()) {
                if (!typeDef.IsPublic) {
                    moduleDef.Types.Remove(typeDef);
                }
            }
            base.ProcessModuleTypes(moduleDef, typeDefs);
        }

        public override void ProcessNestedTypes(TypeDefinition typeDef, IList<TypeDefinition> typeDefs) {
            foreach (var nestedTypeDef in typeDefs.ToList()) {
                if (!nestedTypeDef.IsPublic) {
                    typeDef.NestedTypes.Remove(nestedTypeDef);
                }
            }
            base.ProcessNestedTypes(typeDef, typeDefs);
        }
    }
}
