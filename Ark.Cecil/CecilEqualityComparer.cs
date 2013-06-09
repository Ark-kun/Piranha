using Mono.Cecil;
using System.Collections.Generic;

namespace Ark.Cecil {
    public class CecilEqualityComparer : IEqualityComparer<IMetadataTokenProvider> {
        private CecilEqualityComparer() { }

        static CecilEqualityComparer _instance = new CecilEqualityComparer();

        public static IEqualityComparer<IMetadataTokenProvider> Default {
            get { return _instance; }
        }

        public static IEqualityComparer<T> GetDefault<T>() where T : IMetadataTokenProvider {
            return (IEqualityComparer<T>)_instance;
        }

        public bool Equals(IMetadataTokenProvider x, IMetadataTokenProvider y) {
            return Equals(x, y);
        }

        public static bool AreEqual(IMetadataTokenProvider x, IMetadataTokenProvider y) {
            if (x == null && y == null) {
                return true;
            }
            if (x == null || y == null) {
                return false;
            }

            x = (x as MethodReturnType) ?? x;
            y = (y as MethodReturnType) ?? y;

            var parameterX = x as ParameterDefinition; //ParameterReference is abstract, has no derived classes other than ParameterDefinition and doesn't contain enough information for comparison (it has index and type only).
            var parameterY = y as ParameterDefinition;
            if (parameterX != null && parameterY != null) {
                return AreEqual((MethodReference)parameterX.Method, (MethodReference)parameterY.Method) && parameterX.Index == parameterY.Index;
            }

            return x.ToString() == y.ToString() && (x.GetType().IsAssignableFrom(y.GetType()) || y.GetType().IsAssignableFrom(x.GetType())); //This seems to be enough and the rest of the types have sufficient overloaded .ToString() methods.
        }

        public int GetHashCode(IMetadataTokenProvider obj) {
            if (obj == null) {
                return 0;
            }

            obj = (obj as MethodReturnType) ?? obj;

            var parameter = obj as ParameterDefinition; //ParameterReference is abstract, has no derived classes other than ParameterDefinition and doesn't contain enough information for comparison (it has index and type only).
            if (parameter != null) {
                obj = (MethodReference)parameter.Method;
            }

            return obj.ToString().GetHashCode();

        }
    }
}