using Ark.Cecil;
using Ark.Linq;
using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ark.Piranha {
    public class ListApiProcessor : CecilProcessor {
        List<MemberReference> _members = new List<MemberReference>();

        public void Dump(TextWriter writer) {
            _members.OrderBy(member => (member.DeclaringType ?? member).FullName).ThenBy(member => member.Name).Select(m => m.FullName).ForEach(writer.WriteLine);
        }

        protected override void ProcessType(TypeDefinition typeDef) {
            if (typeDef.IsPublic || typeDef.IsNestedPublic || typeDef.IsNestedFamily || typeDef.IsNestedFamilyOrAssembly) {
                _members.Add(typeDef);
            }
            base.ProcessType(typeDef);
        }

        protected override void ProcessField(FieldDefinition fieldDef) {
            if (fieldDef.IsPublic || fieldDef.IsFamily || fieldDef.IsFamilyOrAssembly) {
                _members.Add(fieldDef);
            }
            base.ProcessField(fieldDef);
        }

        protected override void ProcessMethod(MethodDefinition methodDef) {
            if (methodDef.IsPublic || methodDef.IsFamily || methodDef.IsFamilyOrAssembly) {
                _members.Add(methodDef);
            }
            base.ProcessMethod(methodDef);
        }
    }
}
