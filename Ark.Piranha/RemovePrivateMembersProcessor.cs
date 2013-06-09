using Mono.Cecil;
using Ark.Linq;
using System.Collections.Generic;
using System.Linq;
using Ark.Cecil;
using System.Diagnostics;

namespace Ark.Piranha {
    public class RemovePrivateMembersProcessor : CecilProcessor {
        bool _preserveFieldsOfStructs;

        public RemovePrivateMembersProcessor(bool preserveFieldsOfStructs = false) {
            _preserveFieldsOfStructs = preserveFieldsOfStructs;
        }

        public ICollection<FieldReference> FieldsToPreserve { get; set; }
        public ICollection<MethodReference> MethodsToPreserve { get; set; }

        protected override void ProcessType(TypeDefinition typeDef) {
            foreach (var removedInterface in typeDef.Interfaces.Where(ShouldRemoveInterface)) {
                Trace.WriteLine(string.Format("Removed implementation of interface {0}.", removedInterface), "RemovePrivateMembers");
            }
            typeDef.Interfaces.RemoveWhere(ShouldRemoveInterface);
            base.ProcessType(typeDef);
        }

        static bool ShouldRemoveInterface(TypeReference interfaceRef) {
            return interfaceRef.Resolve() == null || !interfaceRef.Resolve().IsPublic;
        }

        protected override void ProcessMethods(TypeDefinition typeDef, IList<MethodDefinition> methodDefs) {
            foreach (var removedMethod in methodDefs.Where(ShouldRemoveMethod)) {
                Trace.WriteLine(string.Format("Removed method {0}.", removedMethod), "RemovePrivateMembers");
            }
            methodDefs.RemoveWhere(ShouldRemoveMethod);

            base.ProcessMethods(typeDef, methodDefs);
        }

        bool ShouldRemoveMethod(MethodDefinition methodDef) {
            return !(
                   methodDef.IsPublic
                || methodDef.IsFamily
                || methodDef.IsFamilyOrAssembly
                || methodDef.Overrides.Any(over => over.DeclaringType.Resolve() != null && over.DeclaringType.Resolve().IsPublic)
                || MethodsToPreserve != null && MethodsToPreserve.Contains(methodDef)
                || methodDef.IsConstructor && (methodDef.IsFamilyAndAssembly || methodDef.IsAssembly) && ShouldPreserveInternalConstructor(methodDef)
                );
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
                    Trace.WriteLine(string.Format("Removing property {0}.", propertyDef), "RemovePrivateMembers");
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
                            Trace.WriteLine(string.Format("Removing information about the {0} override, because the overriden type {1} is not public.", methodDef, resolvedType), "RemovePrivateMembers");
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
