using System;
using System.IO;

namespace Chutzpah.Models
{
    /// <summary>
    /// Determines what types of files we are testing
    /// </summary>
    [Flags]
    public enum TestingMode
    {
        JavaScript = 1,
        CoffeeScript = 2,
        TypeScript = 4,
        AllExceptHTML = 8,
        HTML = 16,
        All = HTML | JavaScript | TypeScript | CoffeeScript
    }
}