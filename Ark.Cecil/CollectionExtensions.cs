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

        public static int RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate) {
            var hashSet = collection as HashSet<T>;
            if (hashSet != null) {
                return hashSet.RemoveWhere(predicate);
            }
            var sortedSet = collection as SortedSet<T>;
            if (hashSet != null) {
                return hashSet.RemoveWhere(predicate);
            }
            var list = collection as IList<T>;
            if (list != null) {
                int removedCount = 0;
                for (int i = 0; i < list.Count; i++) {
                    if (predicate(list[i])) {
                        removedCount++;
                    } else if(removedCount > 0) {
                        list[i - removedCount] = list[i];
                    }
                }
                for (int i = 0; i < removedCount; i++) {
                    list.RemoveAt(list.Count - 1);
                }
                return removedCount;
            } else {
                var itemsToRemove = collection.Where(predicate).ToList();
                foreach (var item in itemsToRemove) {
                    collection.Remove(item);
                }
                return itemsToRemove.Count;
            }
        }

        public static List<T> Exclude<T>(this ICollection<T> collection, Func<T, bool> predicate) {
            var list = collection as IList<T>;
            if (list != null) {
                var removedItems = new List<T>();
                for (int i = 0; i < list.Count; i++) {
                    if (predicate(list[i])) {
                        removedItems.Add(list[i]);
                    } else if(removedItems.Count > 0) {
                        list[i - removedItems.Count] = list[i];
                    }
                }
                for (int i = 0; i < removedItems.Count; i++) {
                    list.RemoveAt(list.Count - 1);
                }
                return removedItems;
            } else {
                var itemsToRemove = collection.Where(predicate).ToList();
                
                var hashSet = collection as HashSet<T>;
                if (hashSet != null) {
                    hashSet.RemoveWhere(predicate);
                    return itemsToRemove;
                }

                var sortedSet = collection as SortedSet<T>;
                if (hashSet != null) {
                    hashSet.RemoveWhere(predicate);
                    return itemsToRemove;
                }

                foreach (var item in itemsToRemove) {
                    collection.Remove(item);
                }
                return itemsToRemove;
            }
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
