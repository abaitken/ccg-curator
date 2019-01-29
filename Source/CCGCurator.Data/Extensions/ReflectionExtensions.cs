using System;
using System.Collections.Generic;
using System.Linq;

namespace CCGCurator.Data
{
    static class ReflectionExtensions
    {
        public static T GetCustomAttribute<T>(this Type type, bool inherit = true)
            where T : Attribute
        {
            var attributes = type.GetCustomAttributes(typeof(T), inherit);
            if (attributes.Length == 0)
                return null;
            if (attributes.Length != 1)
                throw new InvalidOperationException("Type has more than one attribute");

            return (T) attributes[0];
        }

        public static IEnumerable<T> GetCustomAttributes<T>(this Type type, bool inherit = true)
            where T : Attribute
        {
            var attributes = type.GetCustomAttributes(typeof(T), inherit);
            if (attributes.Length == 0)
                return Enumerable.Empty<T>();

            return attributes.Cast<T>();
        }
    }
}