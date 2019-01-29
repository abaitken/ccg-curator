using System;
using System.Collections.Generic;
using System.Text;

namespace CCGCurator.Data
{
    internal static class EnumerableExtensions
    {
        public static string BuildCharacterSeparatedString<T>(this IEnumerable<T> collection, char separator = ',')
        {
            return BuildCharacterSeparatedString(collection, i => i.ToString());
        }

        public static string BuildCharacterSeparatedString<T>(this IEnumerable<T> collection,
            Func<T, string> valueSelector, char separator = ',')
        {
            var result = new StringBuilder();

            foreach (var item in collection)
            {
                if (result.Length != 0)
                    result.Append(separator);
                result.Append(valueSelector(item));
            }

            return result.ToString();
        }
    }
}