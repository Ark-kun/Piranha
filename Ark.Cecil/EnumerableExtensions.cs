using System;
using System.Collections.Generic;

namespace Ark.Linq {
    public static partial class EnumerableExtensions {
        /// <summary>
        /// Performs the specified action on each element of the specified sequence.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of source.</typeparam>
        /// <param name="source">A sequence of values to invoke a transform function on.</param>
        /// <param name="action">The <see cref="T:System.Action`1" /> to perform on each element of array.</param>
        /// <exception cref="System.ArgumentNullException">source or action is null.</exception>
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action) {
            if (source == null) {
                throw new ArgumentNullException("source");
            }
            if (action == null) {
                throw new ArgumentNullException("action");
            }
            foreach (var element in source) {
                action(element);
            }
        }
    }
}
