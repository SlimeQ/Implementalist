namespace Implementalist.Tools;

public class LogoutTool : Tool
{
    public override string Hit => "logout";
    public override string SampleInput => "";
    public override string Description => $"Log out of the host machine.";

    public override async Task<string> UseTool(Agent agent, string input)
    {
        if (agent.IsLoggedIn)
        {
            agent.Logout();
            return "Logged out";
        }

        return "Not logged in";
    }
}