using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chutzpah.Utility
{
    public class ValidationHelper
    {
        private const string ValidProxyPattern = @".*:[\d]+$";
        public static bool IsValidProxySetting(string value)
        {
            return (!string.IsNullOrEmpty(value) &&
                System.Text.RegularExpressions.Regex.IsMatch(value, ValidProxyPattern));
        }
    }
}
