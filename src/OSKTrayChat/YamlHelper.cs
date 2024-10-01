using System;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;

namespace OSKTrayChat
{
    public class YamlHelper
    {
        public static T DeserializeYaml<T>(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance) // Adjust naming convention if needed
                .Build();

            try
            {
                var result = deserializer.Deserialize<T>(yaml);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deserializing YAML: {ex.Message}");
                return default(T); // Return default value for the type
            }
        }
    }
}