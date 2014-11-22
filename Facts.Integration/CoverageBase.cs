using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Facts.Integration
{
    public class CoverageBase
    {
        protected TestOptions WithCoverage(params Action<CoverageOptions>[] mods)
        {
            var opts = new TestOptions
            {
                CoverageOptions = new CoverageOptions
                {
                    Enabled = true,
                    ExcludePatterns = new[] { "*chai.js*" },
                }
            };
            mods.ToList().ForEach(a => a(opts.CoverageOptions));
            return opts;
        }
    }
}
