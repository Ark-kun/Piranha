using Mono.Cecil;
using Ark.Linq;
using System.Collections.Generic;
using System.Linq;
using Ark.Cecil;
using System.Diagnostics;

namespace Ark.Piranha {
    public class RemovePrivateMembersProcessor : CecilProcessor {
        bool _preserveFieldsOfStructs;
        bool _leaveSomeInternalConstructorsWithParameters;

        public RemovePrivateMembersProcessor(bool preserveFieldsOfStructs = false, bool leaveSomeInternalConstructorsWithParameters = true) {
            _preserveFieldsOfStructs = preserveFieldsOfStructs;
            _leaveSomeInternalConstructorsWithParameters = leaveSomeInternalConstructorsWithParameters;
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

        bool ShouldPreserveInternalConstructor(MethodDefinition methodDef) {
            return !methodDef.HasParameters || (_leaveSomeInternalConstructorsWithParameters && methodDef.DeclaringType.GetParameterlessConstructor() == null && !methodDef.DeclaringType.Methods.Any(m => m.IsConstructor && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly)));
        }

        protected override void ProcessField(FieldDefinition fieldDef) {
            if (!fieldDef.IsPublic && !fieldDef.IsFamily && !(fieldDef.DeclaringType.IsValueType && _preserveFieldsOfStructs) && !(FieldsToPreserve != null && FieldsToPreserve.Contains(fieldDef))) {
                Trace.WriteLine(string.Format("Removing field {0}.", fieldDef), "RemovePrivateMembers");
                fieldDef.DeclaringType.Fields.Remove(fieldDef);
            }
            base.ProcessField(fieldDef);
        }

        protected override void ProcessProperty(PropertyDefinition propertyDef) {
            if (propertyDef.GetMethod != null && propertyDef.GetMethod.Module == null) {
                propertyDef.GetMethod = null;
            }
            if (propertyDef.SetMethod != null && propertyDef.SetMethod.Module == null) {
                propertyDef.SetMethod = null;
            }
            propertyDef.OtherMethods.RemoveWhere(methodDef => methodDef.Module == null);

            if (propertyDef.GetMethod == null && propertyDef.SetMethod == null && !propertyDef.OtherMethods.Any()) {
                Trace.WriteLine(string.Format("Removing property {0}.", propertyDef), "RemovePrivateMembers");
                propertyDef.DeclaringType.Properties.Remove(propertyDef);
            }

            base.ProcessProperty(propertyDef);
        }

        protected override void ProcessEvent(EventDefinition eventDef) {
            if (eventDef.AddMethod != null && eventDef.AddMethod.Module == null) {
                eventDef.AddMethod = null;
            }
            if (eventDef.RemoveMethod != null && eventDef.RemoveMethod.Module == null) {
                eventDef.RemoveMethod = null;
            }
            eventDef.OtherMethods.RemoveWhere(methodDef => methodDef.Module == null);

            if (eventDef.AddMethod == null && eventDef.RemoveMethod == null && !eventDef.OtherMethods.Any()) {
                Trace.WriteLine(string.Format("Removing event {0}.", eventDef), "RemovePrivateMembers");
                eventDef.DeclaringType.Events.Remove(eventDef);
            }

            base.ProcessEvent(eventDef);
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

            if (ShouldRemoveMethod(methodDef)) {
                Trace.WriteLine(string.Format("Removing method {0}.", methodDef), "RemovePrivateMembers");
                methodDef.DeclaringType.Methods.Remove(methodDef);
            }            

            base.ProcessMethod(methodDef);
        }
    }
}
