using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Liminal.Platform.Experimental.Extensions
{
    public static class GameObjectLinq
    {
        #region Utilities

        [ThreadStatic]
        private static Dictionary<Type, IList> _cachedLists;

        private static List<T> GetCachedList<T>()
        {
            if (_cachedLists == null)
                _cachedLists = new Dictionary<Type, IList>();

            IList list;
            if (!_cachedLists.TryGetValue(typeof(T), out list))
            {
                list = new List<T>();
                _cachedLists[typeof(T)] = list;
            }

            list.Clear();
            return (List<T>)list;
        }

        private static void ReleaseCachedList<T>(List<T> list)
        {
            if (_cachedLists == null)
                _cachedLists = new Dictionary<Type, IList>();

            list.Clear();
            _cachedLists[typeof(T)] = list;
        }

        #endregion

        public static IEnumerable<TSource> SelectComponent<TSource>(this IEnumerable<GameObject> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var tempList = GetCachedList<TSource>();
            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                for (int i = 0; i < tempList.Count; ++i)
                {
                    yield return tempList[i];
                }
            }

            ReleaseCachedList(tempList);
        }

        public static IEnumerable<TResult> SelectComponent<TSource, TResult>(this IEnumerable<GameObject> source, Func<TSource, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var tempList = GetCachedList<TSource>();
            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                for (int i = 0; i < tempList.Count; ++i)
                {
                    yield return selector(tempList[i]);
                }
            }

            ReleaseCachedList(tempList);
        }

        public static IEnumerable<TSource> WhereComponent<TSource>(this IEnumerable<GameObject> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var tempList = GetCachedList<TSource>();
            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                for (int i = 0; i < tempList.Count; ++i)
                {
                    var c = tempList[i];
                    if (predicate(c))
                        yield return c;
                }
            }

            ReleaseCachedList(tempList);
        }

        public static int CountComponent<TSource>(this IEnumerable<GameObject> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            int count = 0;
            var tempList = GetCachedList<TSource>();
            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                count += tempList.Count;
            }

            ReleaseCachedList(tempList);
            return count;
        }

        public static int CountComponent<TSource>(this IEnumerable<GameObject> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            int count = 0;
            var tempList = GetCachedList<TSource>();
            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                for (int i = 0; i < tempList.Count; ++i)
                {
                    var c = tempList[i];
                    if (predicate(c))
                        count++;
                }
            }

            ReleaseCachedList(tempList);
            return count;
        }

        public static TSource FirstComponent<TSource>(this IEnumerable<GameObject> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            foreach (var go in source)
            {
                if (go == null)
                    continue;

                var c = go.GetComponent<TSource>();
                if (c != null)
                    return c;
            }

            throw new InvalidOperationException("Source contains no elements.");
        }

        public static TSource FirstComponent<TSource>(this IEnumerable<GameObject> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            foreach (var go in source)
            {
                if (go == null)
                    continue;

                var c = go.GetComponent<TSource>();
                if (c != null && predicate(c))
                    return c;
            }

            throw new InvalidOperationException("Source contains no elements.");
        }

        public static TSource FirstOrDefaultComponent<TSource>(this IEnumerable<GameObject> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            foreach (var go in source)
            {
                if (go == null)
                    continue;

                var c = go.GetComponent<TSource>();
                if (c != null)
                    return c;
            }

            return default(TSource);
        }

        public static TSource FirstOrDefaultComponent<TSource>(this IEnumerable<GameObject> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            foreach (var go in source)
            {
                if (go == null)
                    continue;

                var c = go.GetComponent<TSource>();
                if (c != null && predicate(c))
                    return c;
            }

            return default(TSource);
        }

        public static TSource SingleComponent<TSource>(this IEnumerable<GameObject> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var tempList = GetCachedList<TSource>();
            TSource single = default(TSource);
            bool found = false;

            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                if (tempList.Count > 0)
                {
                    if (found || tempList.Count > 1)
                    {
                        ReleaseCachedList(tempList);
                        throw new InvalidOperationException("Source contains more than one element");
                    }

                    single = tempList[0];
                    found = true;
                }
            }

            ReleaseCachedList(tempList);

            if (found)
                return single;

            throw new InvalidOperationException("Source contains no elements.");
        }

        public static TSource SingleComponent<TSource>(this IEnumerable<GameObject> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var tempList = GetCachedList<TSource>();
            TSource single = default(TSource);
            bool found = false;

            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                if (tempList.Count > 0)
                {
                    for (int i = 0; i < tempList.Count; ++i)
                    {
                        var c = tempList[i];
                        if (!predicate(c))
                            continue;

                        if (found)
                        {
                            ReleaseCachedList(tempList);
                            throw new InvalidOperationException("Source contains more than one element");
                        }

                        single = c;
                        found = true;
                    }
                }
            }

            ReleaseCachedList(tempList);

            if (found)
                return single;

            throw new InvalidOperationException("Source contains no elements.");
        }

        public static TSource SingleOrDefaultComponent<TSource>(this IEnumerable<GameObject> source)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var tempList = GetCachedList<TSource>();
            TSource single = default(TSource);
            bool found = false;

            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                if (tempList.Count > 0)
                {
                    if (found || tempList.Count > 1)
                    {
                        ReleaseCachedList(tempList);
                        throw new InvalidOperationException("Source contains more than one element");
                    }

                    single = tempList[0];
                    found = true;
                }
            }

            ReleaseCachedList(tempList);

            if (found)
                return single;

            return default(TSource);
        }

        public static TSource SingleOrDefaultComponent<TSource>(this IEnumerable<GameObject> source, Func<TSource, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var tempList = GetCachedList<TSource>();
            TSource single = default(TSource);
            bool found = false;

            foreach (var go in source)
            {
                if (go == null)
                    continue;

                go.GetComponents(tempList);
                if (tempList.Count > 0)
                {
                    for (int i = 0; i < tempList.Count; ++i)
                    {
                        var c = tempList[i];
                        if (!predicate(c))
                            continue;

                        if (found)
                        {
                            ReleaseCachedList(tempList);
                            throw new InvalidOperationException("Source contains more than one element");
                        }

                        single = c;
                        found = true;
                    }
                }
            }

            ReleaseCachedList(tempList);

            if (found)
                return single;

            return default(TSource);
        }
    }
}