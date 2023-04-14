namespace Implementalist.Tools;

public class UserTool : Tool
{
    public override string Hit => "user";
    public override string SampleInput => "<question>";
    public override string Description => "Prompts the user for some input.";

    public override async Task<string> UseTool(Agent agent, string input)
    {
        if (agent == null)
        {
            return UI.ReadLine();
        }

        return await agent.owner.RespondToChild(input);
    }
}