using OpenAI_API.Chat;

namespace Implementalist.Tools;

public class AgentTool : Tool
{
    public override string Hit => "do";
    public override string SampleInput => "tell me what times I can see the new matrix movie on friday in Pittsburgh";
    public override string Description => "Creates a new AI agent to solve a more complex task than you can do in one step. You will only receive the output once the goal is completed. The new agent may choose to ask you questions via the [%USER%] command.";
    
    public override async Task<string> UseTool(Agent user, string goal)
    {
        var agent = new Agent(user, goal);
        return await agent.GetResult();
    }
}