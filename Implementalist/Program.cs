using Implementalist;
using OpenAI_API;
using OpenAI_API.Chat;

public static class Program
{
    private const string SystemMessage = @"You are an AI agent with a particular set of tools that can be used towards a given goal.

Tools are used with the following syntax:
```
[%TOOL%] All text up to the following newline will be the tool's input
```

The following are the tools at your disposal:
```
$tool_list
```

You may use any tool at any point in your messages, and the response will be given in the next User message. 
If you do not use any tools in a message, it will be assumed that you are finished with your task and you will be destroyed.
Any comments made outside of a [%USER%] command will be discarded and never shown to the user.

Your task is to accept a given goal and, after many steps, give a single message as output.

GOAL: $goal
";

    public static string BuildSystemMessage(string goal)
    {
        return SystemMessage
            .Replace("$tool_list", Tool.BuildToolList())
            .Replace("$goal", goal);
    }

    public static OpenAIAPI openAIClient;
    private static HumanAgent human;
    public static async Task Main(params string[] args)
    {
        UI.Start();
        
        Secrets.Load();
        openAIClient = new OpenAIAPI(Secrets.OPENAI_API_KEY);
        human = new HumanAgent();

        // while (true)
        // {
        //     var input = UI.ReadLine();
        //     UI.WriteLine($"{Secrets.LINUX_USER}@{Secrets.LINUX_HOST}$");
        //     var result = (await Tool.TryUse(human, $"[%BASH%] {input}")).result;
        //     UI.WriteLine(result);
        // }
        
        // await RunCommand("[%LOGIN%]");
        // await RunCommand("[%BASH%] pwd");
        // await RunCommand("[%BASH%] ls");
        // await RunCommand("[%BASH%] sudo apt update");
        // await RunCommand("[%BASH%] curl https://www.google.com/");
        // await RunCommand("[%LOGOUT%]");


        var goal = @"1. download this repo (git@github.com:SlimeQ/Scooby.git). You already have ssh access. 
2. Investigate its contents
3. Build it
4. Deploy it as a service.";
        
        UI.WriteLine(BuildSystemMessage(goal));
        
        var finalAnswer = (await Tool.TryUse(human, $"[%DO%] {goal}")).result;
        // var finalAnswer = (await Tool.TryUse(human, $"[%BASH%] curl https://www.google.com/")).result;
        UI.WriteLine("----------------------");
        UI.WriteLine($"{finalAnswer}");
        UI.WriteLine("----------------------");
    }

    private static async Task<string> RunCommand(string command, bool askPermission=false)
    {
        UI.WriteLine($"> {command}");
        if (!askPermission || await PromptForApproval())
        {
            string result = null;
            var response = await Tool.TryUse(human, command);
            if (response.result != null)
            {
                result = response.result;
                UI.WriteLine(response.result);
            }
            else
            {
                UI.WriteLine("Bad result!");
            }

            return result;
        }

        return null;
    }
    
    public static async Task<bool> PromptForApproval(string prompt=null)
    {
        UI.WriteLine(prompt);
        while (true)
        {
            UI.WriteLine("Do you approve this action? (Y / N)");
            var s = UI.ReadLine();
            UI.WriteLine(s);
            switch (s.ToLower()[0])
            {
                case 'y'    : return true;
                case 'n'    : return false;
                default: continue;
            }
        }
    }
}