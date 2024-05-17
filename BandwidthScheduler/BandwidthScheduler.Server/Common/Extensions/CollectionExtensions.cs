namespace BandwidthScheduler.Server.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static T? CompareOrDefault<T>(this IEnumerable<T> collection, Func<T, T, bool> firstIsDesired)
        {
            var output = default(T);
            var first = true;

            foreach (var item in collection)
            {
                if (first || firstIsDesired(item, output))
                {
                    output = item;
                }
                first = false;
            }

            return output;
        }

        public static T? CompareNumericOrDefault<T>(this IEnumerable<T> collection, Func<T, double> selector, Func<double, double, bool> firstIsDesired)
        {
            return collection.CompareOrDefault((f, s) => firstIsDesired(selector(f), selector(s)));
        }

        public static T? MaxOrDefault<T>(this IEnumerable<T> collection, Func<T, double> selector)
        {
            return collection.CompareNumericOrDefault(selector, (f, s) => f > s);
        }

        public static T? MinOrDefault<T>(this IEnumerable<T> collection, Func<T, double> selector)
        {
            return collection.CompareNumericOrDefault(selector, (f, s) => f < s);
        }

        public static Dictionary<K, T[]> ToDictionaryAggregate<T, K>(this IEnumerable<T> collection, Func<T, K> key) where K : notnull
        {
            var elementCounts = new Dictionary<K, int>();

            foreach (var element in collection)
            {
                if (!elementCounts.ContainsKey(key(element)))
                {
                    elementCounts.Add(key(element), 0);
                }

                elementCounts[key(element)]++;
            }

            var elementArrays = new Dictionary<K, T[]>();

            foreach (var element in elementCounts)
            {
                elementArrays.Add(element.Key, new T[element.Value]);
                elementCounts[element.Key] = 0;
            }

            foreach (var element in collection)
            {
                elementArrays[key(element)][elementCounts[key(element)]] = element;
                elementCounts[key(element)]++;
            }

            return elementArrays;
        }

        public static Dictionary<T, O> SelectDictionaryValue<T, K, O>(this Dictionary<T, K> dictionary, Func<K, O> transform) where T : notnull
        {
            var outputDictionary = new Dictionary<T, O>();

            foreach (var kv in dictionary)
            {
                outputDictionary.Add(kv.Key, transform(kv.Value));
            }

            return outputDictionary;
        }

        public static void Foreach<T>(this IEnumerable<T> collection, Action<T> action)
        { 
            foreach (var item in collection)
            { 
                action(item);
            }
        }

        public static void AddRange<T>(this HashSet<T> collection, IEnumerable<T> newValues)
        {
            foreach (var value in newValues)
            {
                collection.Add(value);
            }
        }
    }
}
