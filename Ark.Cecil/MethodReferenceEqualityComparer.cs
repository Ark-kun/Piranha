using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Cecil {
    public class MethodReferenceEqualityComparer : IEqualityComparer<MethodReference> {
        private MethodReferenceEqualityComparer() { }

        static MethodReferenceEqualityComparer _instance = new MethodReferenceEqualityComparer();

        public static MethodReferenceEqualityComparer Default {
            get { return _instance; }
        }

        public bool Equals(MethodReference x, MethodReference y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }
            return x.FullName == y.FullName;
        }

        public int GetHashCode(MethodReference obj) {
            return obj.FullName.GetHashCode();
        }
    }
}
