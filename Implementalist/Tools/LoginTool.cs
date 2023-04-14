namespace Implementalist.Tools;

public class LoginTool : Tool
{
    public override string Hit => "login";
    public override string SampleInput => "";
    public override string Description => $"Login to the host machine.";
    
    public override async Task<string> UseTool(Agent agent, string input)
    {
        if (agent.IsLoggedIn)
        {
            return "Already logged in";
        }
        return await agent.Login(Secrets.LINUX_HOST, Secrets.LINUX_USER, Secrets.LINUX_PASS);
    }
}