using Ark.Cecil;
using Ark.Linq;
using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Piranha {
    public class RemovePInvokeMethodsProcessor : CecilProcessor {
        protected override void ProcessMethods(TypeDefinition typeDef, IList<MethodDefinition> methodDefs) {
            methodDefs.RemoveWhere(methodDef => methodDef.HasPInvokeInfo);
            base.ProcessMethods(typeDef, methodDefs);
        }
    }
}
