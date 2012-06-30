using System;

namespace Chutzpah.Models
{
    /// <summary>
    /// Determines if we are testing JavaScript files (and creating harnesses for them), testing html test harnesses directly or both
    /// </summary>
    [Flags]
    public enum TestingMode
    {
        JavaScript = 1,
        HTML = 2,
        All = JavaScript | HTML
    }
}