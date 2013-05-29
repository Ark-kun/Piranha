using System;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Collections {
    public static class CollectionExtensions {
        public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate) {
            var itemsToRemove = collection.Where(predicate).ToList();
            foreach (var item in itemsToRemove) {
                collection.Remove(item);
            }
        }
    }
}
