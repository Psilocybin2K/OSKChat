using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using OSKTrayChat.Models;
using OSKTrayChat.Services;

namespace OSKTrayChat.Agents
{
    [Experimental("SKEXP0010")]
    public class TestCaseWritingAgent : ITestCaseWritingAgent
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;
        private readonly ILogger<TestCaseWritingAgent> _logger;
        private readonly IConfigurationService _configService;
        private ChatHistory _history;

        public TestCaseWritingAgent(
            ILogger<TestCaseWritingAgent> logger,
            ILogger<TestCaseInvocationFilter> loggerFilter,
            ILogger<TestCaseWritingFunctions> loggerWritingFunctions,
            IConfigurationService configService)
        {
            _logger = logger;
            _configService = configService;

            var pipelineConfig = _configService.GetPipelineConfiguration();
            var builder = CreateKernelBuilder();

            builder.Services.AddSingleton<IFunctionInvocationFilter, TestCaseInvocationFilter>(i => new TestCaseInvocationFilter(loggerFilter));
            builder.Services.AddSingleton(i => new TestCaseWritingFunctions(loggerWritingFunctions));

            _kernel = builder.Build();
            _kernel.FunctionInvocationFilters.Add(new TestCaseInvocationFilter(loggerFilter));

            _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            _history = new ChatHistory("You are a helpful assistant specialized in writing test cases.");
        }

        public async Task<ReadOnlyMemory<float>[]> GenerateEmbeddings(params string[] inputs)
        {
            var embeddingGenerator = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            var embeddings = await embeddingGenerator.GenerateEmbeddingsAsync(inputs, _kernel);

            return embeddings.ToArray();
        }

        public async IAsyncEnumerable<string> InvokeStreamAsync(string prompt)
        {
            _history.AddUserMessage(prompt);

            var fullResponse = new StringBuilder();
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            try
            {
                await foreach (var content in _chatCompletionService.GetStreamingChatMessageContentsAsync(_history, executionSettings, _kernel))
                {
                    fullResponse.Append(content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InvokeStreamAsync");
                throw;
            }

            // Yielding outside of the try block
            foreach (var content in fullResponse.ToString().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                yield return content;
            }

            _history.AddAssistantMessage(fullResponse.ToString());
        }

        public async Task<string> InvokeAsync(string prompt)
        {
            try
            {
                _history.AddUserMessage(prompt);

                var result = await _chatCompletionService.GetChatMessageContentAsync(_history);
                var resultString = result?.ToString() ?? string.Empty;

                _logger.LogInformation("AI Response: {Response}", resultString);

                _history.AddAssistantMessage(resultString);

                return resultString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InvokeAsync");
                return $"An error occurred: {ex.Message}";
            }
        }

        public void ClearHistory()
        {
            _history.Clear();
        }

        private IKernelBuilder CreateKernelBuilder()
        {
            var apiKey = _configService.GetOpenAIApiKey();
            var modelId = _configService.GetOpenAIModelId();
            var embedModelId = _configService.GetOpenAIEmbeddingModelId();

            return Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(modelId: modelId, apiKey: apiKey)
                .AddOpenAITextEmbeddingGeneration(modelId: embedModelId, apiKey);
        }
    }
}