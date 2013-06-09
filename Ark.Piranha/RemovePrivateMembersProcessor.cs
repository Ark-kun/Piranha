using Mono.Cecil;
using Ark.Linq;
using System.Collections.Generic;
using System.Linq;
using Ark.Cecil;

namespace Ark.Piranha {
    public class RemovePrivateMembersProcessor : CecilProcessor {
        bool _preserveFieldsOfStructs;

        public RemovePrivateMembersProcessor(bool preserveFieldsOfStructs = false) {
            _preserveFieldsOfStructs = preserveFieldsOfStructs;
        }

        public ICollection<FieldReference> FieldsToPreserve { get; set; }
        public ICollection<MethodReference> MethodsToPreserve { get; set; }

        protected override void ProcessType(TypeDefinition typeDef) {
            typeDef.Interfaces.RemoveWhere(interfaceRef => !interfaceRef.Resolve().IsPublic);
            base.ProcessType(typeDef);
        }

        protected override void ProcessMethods(TypeDefinition typeDef, IList<MethodDefinition> methodDefs) {
            methodDefs.RemoveWhere(methodDef => !(
                   methodDef.IsPublic
                || methodDef.IsFamily
                || methodDef.IsFamilyOrAssembly
                || methodDef.Overrides.Any(over => over.DeclaringType.Resolve().IsPublic)
                || MethodsToPreserve != null && MethodsToPreserve.Contains(methodDef)
                || methodDef.IsConstructor && (methodDef.IsFamilyAndAssembly || methodDef.IsAssembly) && ShouldPreserveInternalConstructor(methodDef)
            ));
            base.ProcessMethods(typeDef, methodDefs);
        }

        static bool ShouldPreserveInternalConstructor(MethodDefinition methodDef) {            
            return !methodDef.HasParameters || methodDef.DeclaringType.GetParameterlessConstructor() == null;
        }

        protected override void ProcessFields(TypeDefinition typeDef, IList<FieldDefinition> fieldDefs) {
            fieldDefs.RemoveWhere(fieldDef => !fieldDef.IsPublic && !fieldDef.IsFamily && !(typeDef.IsValueType && _preserveFieldsOfStructs) && !(FieldsToPreserve != null && FieldsToPreserve.Contains(fieldDef)));
            base.ProcessFields(typeDef, fieldDefs);
        }

        protected override void ProcessProperties(TypeDefinition typeDef, IList<PropertyDefinition> propertyDefs) {
            base.ProcessProperties(typeDef, propertyDefs);
            foreach (var propertyDef in typeDef.Properties.ToList()) {
                if (propertyDef.GetMethod == null && propertyDef.SetMethod == null) {
                    typeDef.Properties.Remove(propertyDef);
                }
            }
        }

        protected override void ProcessProperty(PropertyDefinition propertyDef) {
            if (propertyDef.GetMethod != null && propertyDef.GetMethod.Module == null) {
                propertyDef.GetMethod = null;
            }
            if (propertyDef.SetMethod != null && propertyDef.SetMethod.Module == null) {
                propertyDef.SetMethod = null;
            }
            base.ProcessProperty(propertyDef);
        }

        protected override void ProcessMethod(MethodDefinition methodDef) {
            //methodDef.Overrides.RemoveWhere(overrideRef => overrideRef.DeclaringType.i)
            var overrides = methodDef.Overrides;
            for (int i = overrides.Count - 1; i >= 0; --i) {
                var declaringType = overrides[i].DeclaringType;
                if (declaringType != null) {
                    var resolvedType = declaringType.TryResolve();
                    if (resolvedType != null) {
                        if (!resolvedType.IsPublic) {
                            overrides.RemoveAt(i);
                        }
                    } else {
                        //?
                    }
                } else {
                }
            }

            base.ProcessMethod(methodDef);
        }
    }
}
