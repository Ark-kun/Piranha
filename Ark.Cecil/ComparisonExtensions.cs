using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Cecil {
    public static class ComparisonExtensions {
        public static bool IsEqualTo(this ArrayType a, ArrayType b) {
            if (a.Rank != b.Rank) {
                return false;
            }
            return true;
        }

        public static bool IsEqualTo(this GenericInstanceType a, GenericInstanceType b) {
            if (a.GenericArguments.Count != b.GenericArguments.Count) {
                return false;
            }
            for (int i = 0; i < a.GenericArguments.Count; i++) {
                if (!IsEqualTo(a.GenericArguments[i], b.GenericArguments[i])) {
                    return false;
                }
            }
            return true;
        }

        public static bool IsEqualTo(this GenericParameter a, GenericParameter b) {
            return (a.Position == b.Position);
        }

        public static bool IsEqualTo(this IModifierType a, IModifierType b) {
            return IsEqualTo(a.ModifierType, b.ModifierType);
        }

        public static bool IsEqualTo(this TypeReference a, TypeReference b) {
            if (object.ReferenceEquals(a, b)) {
                return true;
            }
            if (a == null && b == null) {
                return true;
            }
            if ((a == null) || (b == null)) {
                return false;
            }
            //if (a.etype != b.etype) {
            //    return false;
            //}
            if (a.IsGenericParameter) {
                return IsEqualTo((GenericParameter)a, (GenericParameter)b);
            }
            //if (a.IsTypeSpecification()) {
            //    return AreSame((TypeSpecification)a, (TypeSpecification)b);
            //}
            return ((!(a.Name != b.Name) && !(a.Namespace != b.Namespace)) && IsEqualTo(a.DeclaringType, b.DeclaringType));
        }

        public static bool IsEqualTo(this TypeSpecification a, TypeSpecification b) {
            if (!IsEqualTo(a.ElementType, b.ElementType)) {
                return false;
            }
            if (a.IsGenericInstance) {
                return IsEqualTo((GenericInstanceType)a, (GenericInstanceType)b);
            }
            if (a.IsRequiredModifier || a.IsOptionalModifier) {
                return IsEqualTo((IModifierType)a, (IModifierType)b);
            }
            if (a.IsArray) {
                return IsEqualTo((ArrayType)a, (ArrayType)b);
            }
            return true;
        }

        public static bool IsEqualTo(this IList<ParameterDefinition> a, IList<ParameterDefinition> b) {
            int count = a.Count;
            if (count != b.Count) {
                return false;
            }
            if (count != 0) {
                for (int i = 0; i < count; i++) {
                    if (!IsEqualTo(a[i].ParameterType, b[i].ParameterType)) {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}