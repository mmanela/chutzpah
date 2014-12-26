using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Extensions
{
    public static class VSTSExtensions
    {
        public enum TestRunItemType
        {
            ResultSummary,
            Results,
            TestDefinition,
            TestEntries,
            TestLists,
            Times,
            TestSettings
        }

        public static T GetInstance<T>(this object[] items, TestRunItemType type)
        {
            return items[(int)type] is T ? (T)items[(int)type] : default(T);
        }
    }
}
