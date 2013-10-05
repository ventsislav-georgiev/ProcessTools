#region

using System;
using System.Collections.Generic;

#endregion

namespace ProcessTools.Core.DynamicDataDisplay
{
    public interface IFilter<T>
    {
        IList<T> Filter(IList<T> toFilter);
    }

    public class MaxSizeFilter : IFilter<PerformanceInfo>
    {
        private readonly TimeSpan _length = TimeSpan.FromSeconds(10);

        public IList<PerformanceInfo> Filter(IList<PerformanceInfo> c)
        {
            if (c.Count == 0)
                return new List<PerformanceInfo>();

            DateTime end = c[c.Count - 1].Time;

            int startIndex = 0;
            for (int i = 0; i < c.Count; i++)
            {
                if (end - c[i].Time <= _length)
                {
                    startIndex = i;
                    break;
                }
            }

            var res = new List<PerformanceInfo>(c.Count - startIndex);
            for (int i = startIndex; i < c.Count; i++)
            {
                res.Add(c[i]);
            }
            return res;
        }
    }
}