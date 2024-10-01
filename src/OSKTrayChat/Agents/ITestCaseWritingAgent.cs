using System.Collections.Generic;
using System.Threading.Tasks;

namespace OSKTrayChat.Agents
{
    public interface ITestCaseWritingAgent
    {
        Task<string> InvokeAsync(string prompt);
        IAsyncEnumerable<string> InvokeStreamAsync(string prompt);
    }
}