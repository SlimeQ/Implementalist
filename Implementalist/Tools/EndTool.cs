namespace Implementalist.Tools;

public class EndTool : Tool
{
    public override string Hit => "end";
    public override string SampleInput => "<final_answer>";
    public override string Description => "Declares the current goal complete, with a final answer that will be delivered to the user.";

    public override async Task<string> UseTool(Agent agent, string input)
    {
        return input;
    }
}