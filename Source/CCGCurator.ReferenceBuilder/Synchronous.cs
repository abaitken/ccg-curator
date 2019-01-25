using System;
using System.Collections.Generic;

namespace CCGCurator.ReferenceBuilder
{
    static class Synchronous
    {
        public static void ForEach<TSource>(IEnumerable<TSource> source, Action<TSource> body)
        {
            foreach (var item in source)
            {
                body(item);
            }
        }
    }
}