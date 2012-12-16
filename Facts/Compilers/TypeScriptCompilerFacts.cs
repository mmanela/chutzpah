using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chutzpah.Compilers;
using Chutzpah.Compilers.JavaScriptEngines;
using Chutzpah.Compilers.TypeScript;
using Xunit;

namespace Chutzpah.Facts.Compilers
{
    public class TypeScriptCompilerFacts
    {
        [Fact]
        public void TypeScriptSmokeTest()
        {
            var code = @"var foo:number = 5;";
            var input = @"{""someFile.ts"":""" + code + @"""}";
            using (var fixture = new TypeScriptCompiler(new Lazy<IJavaScriptRuntime>(() => new IEJavaScriptRuntime())))
            {

                var result = fixture.Compile(input);
                Assert.False(String.IsNullOrWhiteSpace(result));
            }
        }

        [Fact]
        public void TypeScriptFailTest()
        {
            var code = "test.invlid.stuff/^/g!%%";
            var input = @"{""someFile.ts"":""" + code + @"""}";
            using (var fixture = new TypeScriptCompiler(new Lazy<IJavaScriptRuntime>(() => new IEJavaScriptRuntime())))
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
