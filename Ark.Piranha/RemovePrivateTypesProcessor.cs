using Ark.Cecil;
using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ark.Piranha {
    public class RemovePrivateTypesProcessor : CecilProcessor {
        protected override void ProcessModuleTypes(ModuleDefinition moduleDef, IList<TypeDefinition> typeDefs) {
            foreach (var typeDef in typeDefs.ToList()) {
                if (!typeDef.IsPublic && typeDef.FullName != "<Module>") {
                    Trace.WriteLine(string.Format("Removing type {0}.", typeDef), "RemovePrivateTypes");
                    moduleDef.Types.Remove(typeDef);
                }
            }
            base.ProcessModuleTypes(moduleDef, typeDefs);
        }

        protected override void ProcessNestedTypes(TypeDefinition typeDef, IList<TypeDefinition> typeDefs) {
            foreach (var nestedTypeDef in typeDefs.ToList()) {
                if (!(nestedTypeDef.IsNestedPublic || nestedTypeDef.IsNestedFamily || nestedTypeDef.IsNestedFamilyOrAssembly)) {
                    Trace.WriteLine(string.Format("Removing type {0}.", nestedTypeDef), "RemovePrivateTypes");
                    typeDef.NestedTypes.Remove(nestedTypeDef);
                }
            }
            base.ProcessNestedTypes(typeDef, typeDefs);
        }
    }
}
