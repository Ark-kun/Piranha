using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Cecil {
    public class TypeReferenceEqualityComparer : IEqualityComparer<TypeReference> {
        private TypeReferenceEqualityComparer() { }

        static TypeReferenceEqualityComparer _instance = new TypeReferenceEqualityComparer();

        public static TypeReferenceEqualityComparer Default {
            get { return _instance; }
        }

        public bool Equals(TypeReference x, TypeReference y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }
            return x.FullName == y.FullName;
        }

        public int GetHashCode(TypeReference obj) {
            return obj.FullName.GetHashCode();
        }
    }
}
