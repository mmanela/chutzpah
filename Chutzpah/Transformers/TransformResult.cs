using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chutzpah.Transformers
{
    /// <summary>
    /// The results of the transforms we ran on the test output.
    /// This is a mapping of transformer name to files it created
    /// </summary>
    public class TransformResult : Dictionary<string, ISet<string>>
    {
        public void AddResult(string transform, string path)
        {
            if (!this.ContainsKey(transform))
            {
                this[transform] = new HashSet<string>();
            }

            this[transform].Add(path);
        }
    }
}
