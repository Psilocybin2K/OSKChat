using Microsoft.Extensions.Configuration;
using OSKTrayChat.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace OSKTrayChat.Services
{
    public interface IConfigurationService
    {
        string GetOpenAIApiKey();
        string GetOpenAIModelId();
        string GetOpenAIEmbeddingModelId();
        AgentPipelineDefinition GetPipelineConfiguration();
    }

    public class ConfigurationService : IConfigurationService
    {
        private readonly IConfiguration _configuration;
        private readonly string _pipelineConfigPath;

        public ConfigurationService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _pipelineConfigPath = _configuration["PipelineConfigPath"] ?? "pipeline.yaml";
        }

        public string GetOpenAIApiKey()
        {
            return _configuration["OpenAI:ApiKey"]
                ?? throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        public string GetOpenAIEmbeddingModelId()
        {
            return _configuration["OpenAI:EmbedModelId"]
                ?? throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        public string GetOpenAIModelId()
        {
            return _configuration["OpenAI:ModelId"] ?? "gpt-4";
        }

        public AgentPipelineDefinition GetPipelineConfiguration()
        {
            if (!File.Exists(_pipelineConfigPath))
            {
                throw new FileNotFoundException($"Pipeline configuration file not found: {_pipelineConfigPath}");
            }

            var yaml = File.ReadAllText(_pipelineConfigPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            return deserializer.Deserialize<AgentPipelineDefinition>(yaml);
        }
    }
}
