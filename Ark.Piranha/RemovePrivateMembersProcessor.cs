using Mono.Cecil;
using Ark.Linq;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Cecil {
    public class RemovePrivateMembersProcessor : CecilProcessor {
        bool _preserveFieldsOfStructs;

        public RemovePrivateMembersProcessor(bool preserveFieldsOfStructs = false) {
            _preserveFieldsOfStructs = preserveFieldsOfStructs;
        }

        public ICollection<FieldReference> FieldsToPreserve { get; set; }
        public ICollection<MethodReference> MethodsToPreserve { get; set; }

        public override void ProcessMethods(TypeDefinition typeDef, IList<MethodDefinition> methodDefs) {
            methodDefs.RemoveWhere(methodDef => !methodDef.IsPublic && !methodDef.IsFamily && !(MethodsToPreserve != null && MethodsToPreserve.Contains(methodDef)));
            base.ProcessMethods(typeDef, methodDefs);
        }

        public override void ProcessFields(TypeDefinition typeDef, IList<FieldDefinition> fieldDefs) {
            fieldDefs.RemoveWhere(fieldDef => !fieldDef.IsPublic && !fieldDef.IsFamily && !(typeDef.IsValueType && _preserveFieldsOfStructs) && !(FieldsToPreserve != null && FieldsToPreserve.Contains(fieldDef)));
            base.ProcessFields(typeDef, fieldDefs);
        }

        public override void ProcessProperties(TypeDefinition typeDef, IList<PropertyDefinition> propertyDefs) {
            base.ProcessProperties(typeDef, propertyDefs);
            foreach (var propertyDef in typeDef.Properties.ToList()) {
                if (propertyDef.GetMethod == null && propertyDef.SetMethod == null) {
                    typeDef.Properties.Remove(propertyDef);
                }
            }
        }

        public override void ProcessProperty(PropertyDefinition propertyDef) {
            if (propertyDef.GetMethod != null && propertyDef.GetMethod.Module == null) {
                propertyDef.GetMethod = null;
            }
            if (propertyDef.SetMethod != null && propertyDef.SetMethod.Module == null) {
                propertyDef.SetMethod = null;
            }
            base.ProcessProperty(propertyDef);
        }
    }
}
