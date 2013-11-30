using System.IO;

namespace Chutzpah.VS2012.TestAdapter
{
    public static class ChutzpahTracingHelper
    {
        public static void Toggle(bool enable)
        {
            var path = Path.Combine(Path.GetTempPath(), Chutzpah.Constants.LogFileName);
            if (enable)
            {
                ChutzpahTracer.AddFileListener(path);
            }
            else
            {
                ChutzpahTracer.RemoveFileListener(path);
            }       
        }
    }
}