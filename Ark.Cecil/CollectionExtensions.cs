using System;
using System.Collections.Generic;
using System.Linq;

namespace Ark.Linq {
    public static class CollectionExtensions {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items) {
            foreach (var item in items) {
                collection.Add(item);
            }
        }

        public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate) {
            var list = collection as IList<T>;
            if (list != null) {
                for (int i = list.Count - 1; i >= 0; --i) {
                    if (predicate(list[i])) {
                        list.RemoveAt(i);
                    }
                }
            } else {
                var itemsToRemove = collection.Where(predicate).ToList();
                foreach (var item in itemsToRemove) {
                    collection.Remove(item);
                }
            }
        }

        public static List<T> Exclude<T>(this ICollection<T> collection, Func<T, bool> predicate) {
            var removedItems = new List<T>();
            var list = collection as IList<T>;
            if (list != null) {
                for (int i = list.Count - 1; i >= 0; --i) {
                    if (predicate(list[i])) {
                        removedItems.Add(list[i]);
                        list.RemoveAt(i);
                    }
                }
            } else {
                var itemsToRemove = collection.Where(predicate).ToList();
                foreach (var item in itemsToRemove) {
                    removedItems.Add(item);
                    collection.Remove(item);
                }
            }
            return removedItems;
        }

        public static void ReversedForEach<TSource>(this IList<TSource> list, Action<TSource> action) {
            if (list == null) {
                throw new ArgumentNullException("source");
            }
            if (action == null) {
                throw new ArgumentNullException("action");
            }
            for (int i = list.Count - 1; i >= 0; --i) {
                action(list[i]);
            }
        }
    }
}
