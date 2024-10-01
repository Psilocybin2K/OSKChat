using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSKTrayChat.Models
{
    public class AgentPipelineStep
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string Instruction { get; set; }
        public AgentPipeline[] ChildPipelines { get; set; }
    }
    public class AgentPipeline
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public AgentPipelineStep[] Steps { get; set; }
    }

    public class AgentPipelineDefinition
    {
        public AgentPipeline[] Pipelines { get; set; }
    }

}
