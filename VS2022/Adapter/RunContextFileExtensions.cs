using System.Collections.Generic;
using System.ComponentModel.Composition;
using Chutzpah.Extensions;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace Chutzpah.VS2022.TestAdapter
{
    [Export(typeof(IRunFromContextFileExtensions))]
    public class RunContextFileExtensions : IRunFromContextFileExtensions
    {
        public IEnumerable<string> FileTypes
        {
            get
            {
                return TestingModeExtensions.ExtensionMap[Models.TestingMode.All];
            }
        }
    }

}