using Ark.Cecil;
using Ark.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Ark.Piranha {
    public class RemovePInvokeMethodsProcessor : CecilProcessor {
        protected override void ProcessMethods(TypeDefinition typeDef, IList<MethodDefinition> methodDefs) {
            foreach (var removedMethod in methodDefs.Where(methodDef => methodDef.HasPInvokeInfo)) {
                Trace.WriteLine(string.Format("Removed P/Invoke method {0}.", removedMethod), "RemovePInvokeMethods");
            }
            methodDefs.RemoveWhere(methodDef => methodDef.HasPInvokeInfo);
            base.ProcessMethods(typeDef, methodDefs);
        }
    }
}
