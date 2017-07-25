using System.Threading.Tasks;
using System;

namespace Chutzpah.Models
{
    public class EdgeJsStringSource : TestCaseSource<string>
    {
        private readonly Func<Func<object, Task<object>>, Task<object>> invoker;

        public EdgeJsStringSource(Func<Func<object, Task<object>>, Task<object>> invoker, int timeout) : base(timeout)
        {
            this.invoker = invoker;
        }

        public override async Task<object> Open()
        {
            var onMessage = (Func<object, Task<object>>)((message) =>
            {
                var stringMessage = message as string;
                if (!string.IsNullOrEmpty(stringMessage))
                {
                    Emit(stringMessage);
                    Console.WriteLine($"Got this message: {stringMessage}");
                }
                return Task.FromResult<object>(null);
            });


            await invoker(onMessage);
            return null;
        }
    }
}