using System.Collections;
using System.Reflection;

namespace BandwidthScheduler.Server.Common.Extensions
{
    public static class ObjectExtensions
    {
        public static void ExplicitlyMarkDateTimesAsUtc(this object obj)
        {
            Type t = obj.GetType();

            // Loop through the properties.
            PropertyInfo[] props = t.GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo p = props[i];
                // If a property is DateTime or DateTime?, set DateTimeKind to DateTimeKind.Utc.
                if (p.PropertyType == typeof(DateTime))
                {
                    DateTime date = (DateTime)p.GetValue(obj, null);
                    date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                    p.SetValue(obj, date, null);
                }
                // Same check for nullable DateTime.
                else if (p.PropertyType == typeof(Nullable<DateTime>))
                {
                    DateTime? date = (DateTime?)p.GetValue(obj, null);
                    if (date.HasValue)
                    {
                        DateTime? newDate = DateTime.SpecifyKind(date.Value, DateTimeKind.Utc);
                        p.SetValue(obj, newDate, null);
                    }
                }
            }
        }

        public static void NullifyRedundancy(this object obj)
        {
            NullifyRedundancyRecursive(obj, new HashSet<Type>());
        }

        private static void NullifyRedundancyRecursive(object obj, HashSet<Type> seenTypes)
        {
            Type t = obj.GetType();
            seenTypes.Add(t);

            PropertyInfo[] props = t.GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo p = props[i];

                if (p.PropertyType.IsClass)
                {
                    if (seenTypes.Contains(p.PropertyType))
                    {
                        p.SetValue(obj, null);
                    }
                    else
                    {
                        var value = p.GetValue(obj);

                        if (value != null)
                        {
                            NullifyRedundancyRecursive(value, seenTypes);
                        }
                    }
                }
                else
                { 
                    var enumerableType = GetAnyElementType(p.PropertyType);

                    if (enumerableType != null && enumerableType.IsClass)
                    {
                        if (seenTypes.Contains(enumerableType))
                        {
                            p.SetValue(obj, null);
                        }
                        else
                        {
                            if (p.PropertyType.IsArray)
                            {
                                foreach (var item in (Array)p.GetValue(obj))
                                {
                                    NullifyRedundancyRecursive(item, seenTypes);
                                }
                            }
                            else
                            {
                                foreach (var item in (IEnumerable)p.GetValue(obj))
                                {
                                    NullifyRedundancyRecursive(item, seenTypes);
                                }
                            }
                        }
                    }
                }
            }

            seenTypes.Remove(t);
        }

        public static Type GetAnyElementType(Type type)
        {
            // Type is Array
            // short-circuit if you expect lots of arrays 
            if (type.IsArray)
                return type.GetElementType();

            // type is IEnumerable<T>;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            // type implements/extends IEnumerable<T>;
            var enumType = type.GetInterfaces()
                                    .Where(t => t.IsGenericType &&
                                           t.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .Select(t => t.GenericTypeArguments[0]).FirstOrDefault();
            return enumType ?? type;
        }
    }
}
