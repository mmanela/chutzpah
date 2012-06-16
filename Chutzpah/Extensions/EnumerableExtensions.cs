using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Extensions
{
    public static class EnumerableExtensions
    {
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> body)
        {
            var exceptions = new List<Exception>();
            foreach (var item in source)
            {
                try
                {
                    body(item);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Count > 0)
                throw new AggregateException(exceptions);
        }
    }
}
