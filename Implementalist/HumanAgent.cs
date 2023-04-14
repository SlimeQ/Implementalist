namespace Implementalist;

public class HumanAgent : Agent
{
    protected override async Task<string> Respond()
    {
        return UI.ReadLine();
    }

    public HumanAgent() : base(null, "")
    {
    }
}