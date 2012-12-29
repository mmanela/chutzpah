using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Compilers;
using Chutzpah.Compilers.CoffeeScript;
using Chutzpah.Compilers.JavaScriptEngines;
using Xunit;

namespace Chutzpah.Facts.Compilers
{
    class CoffeeScriptCompilerFacts
    {
        [Fact]
        public void CoffeeScriptSmokeTest()
        {
            var input = @"v = x*5 for x in [1...10]";
            using (var fixture = new CoffeeScriptCompiler(new Lazy<IJavaScriptRuntime>(() => new IEJavaScriptRuntime())))
            {

                var result = fixture.Compile(input);
                Assert.False(String.IsNullOrWhiteSpace(result));
            }
        }

        [Fact]
        public void CoffeeScriptFailTest()
        {
            var input = "test.invlid.stuff/^/g!%%";
            using (var fixture = new CoffeeScriptCompiler(new Lazy<IJavaScriptRuntime>(() => new IEJavaScriptRuntime())))
            {

                bool shouldDie = false;

                try
                {
                    var result = fixture.Compile(input);
                    if (result.StartsWith("ENGINE FAULT"))
                        shouldDie = true;
                    else Console.WriteLine(result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ex: " + ex.Message);
                    shouldDie = true;
                }

                Assert.True(shouldDie);
            }
        }
    }
}
