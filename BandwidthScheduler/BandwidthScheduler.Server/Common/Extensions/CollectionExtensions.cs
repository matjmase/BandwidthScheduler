namespace BandwidthScheduler.Server.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static T CompareOrDefault<T>(this IEnumerable<T> collection, Func<T, T, bool> firstIsDesired)
        { 
            var output = default(T);
            var first = true;

            foreach(var item in collection)
            {
                if (first || firstIsDesired(item, output))
                {
                    output = item;  
                }
                first = false;
            }

            return output;
        }

        public static T CompareNumericOrDefault<T>(this IEnumerable<T> collection, Func<T, double> selector, Func<double, double, bool> firstIsDesired)
        { 
            return collection.CompareOrDefault((f,s) => firstIsDesired(selector(f), selector(s)));
        }

        public static T MaxOrDefault<T>(this IEnumerable<T> collection, Func<T, double> selector)
        {   
            return collection.CompareNumericOrDefault(selector, (f,s) => f > s);
        }

        public static T MinOrDefault<T>(this IEnumerable<T> collection, Func<T, double> selector)
        {
            return collection.CompareNumericOrDefault(selector, (f, s) => f < s);
        }
    }
}
