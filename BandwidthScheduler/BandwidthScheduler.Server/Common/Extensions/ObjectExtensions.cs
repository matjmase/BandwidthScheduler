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

        public static void NullifyObjectDepth(this object obj, int depth = 0)
        {
            Type t = obj.GetType();

            // Loop through the properties.
            PropertyInfo[] props = t.GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo p = props[i];
                // If a property is DateTime or DateTime?, set DateTimeKind to DateTimeKind.Utc.
                if (p.PropertyType.IsClass)
                {
                    if (depth != 0)
                    {
                        object? nestedObj = p.GetValue(obj, null);

                        if (nestedObj != null)
                        {
                            nestedObj.NullifyObjectDepth(depth - 1);
                        }
                    }
                    // Same check for nullable DateTime.
                    else
                    {
                        p.SetValue(obj, null);
                    }
                }
            }
        }
    }
}
