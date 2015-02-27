using System.Collections.Generic;
namespace Chutzpah
{
    public static class ChutzpahMsBuildProps
    {
        public const string Chutzpah_OutputPath = "Chutzpah_OutputPath";

        public static IEnumerable<string> GetProps()
        {
            yield return Chutzpah_OutputPath;
        }

    }
}
