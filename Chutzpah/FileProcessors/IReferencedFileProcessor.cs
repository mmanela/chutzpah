using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Models;

namespace Chutzpah
{
    /// <summary>
    /// Interface of a class which can process a referenced file
    /// </summary>
    public interface IReferencedFileProcessor
    {
        void Process(ReferencedFile referencedFile);
    }
}
