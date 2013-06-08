using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Cecil {
    public class MemberReferenceEqualityComparer : IEqualityComparer<MemberReference> {
        private MemberReferenceEqualityComparer() { }

        static MemberReferenceEqualityComparer _instance = new MemberReferenceEqualityComparer();

        public static MemberReferenceEqualityComparer Default {
            get { return _instance; }
        }

        public bool Equals(MemberReference x, MemberReference y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }
            return x.FullName == y.FullName;
        }

        public int GetHashCode(MemberReference obj) {
            return obj.FullName.GetHashCode();
        }
    }
}
