using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace OSKTrayChat.Agents
{
    public class TestCaseInvocationFilter : IFunctionInvocationFilter
    {
        private readonly ILogger<TestCaseInvocationFilter> _logger;

        public TestCaseInvocationFilter(ILogger<TestCaseInvocationFilter> logger)
        {
            _logger = logger;
        }

        public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            _logger.LogInformation("Invoking function: {FunctionName}", context.Function.Name);
            return next(context);
        }

        public KernelArguments OnFunctionInvoking(KernelArguments arguments)
        {
            _logger.LogInformation("Function invoking with arguments: {Arguments}", string.Join(", ", arguments));
            return arguments;
        }
    }
}