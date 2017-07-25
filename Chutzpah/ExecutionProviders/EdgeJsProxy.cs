using System;
using System.Collections.Generic;
using Chutzpah.Models;
using EdgeJs;
using System.Threading.Tasks;

namespace Chutzpah
{
    public interface IEdgeJsProxy
    {
        Func<object, Task<object>> CreateFunction(string code);
    }

    public class EdgeJsProxy : IEdgeJsProxy
    {
        public Func<object, Task<object>> CreateFunction(string code)
        {
            return Edge.Func(code);
        }
    }
}