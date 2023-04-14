using System.Text.RegularExpressions;
using Implementalist.Tools;
using System.Reflection;

namespace Implementalist;

public abstract class Tool
{
    // returning this from a command will make the command succeed without sending a matrix message. This is important for commands that update a single message many times
    public const string SilentCompletionToken = "[|SILENT|]";
    
    protected static List<Tool> allTools = new ();
    public virtual bool WritesOutputToUI => false;

    static Tool() 
    { 
        Type parentType = typeof(Tool);
        Assembly assembly = Assembly.GetExecutingAssembly();
        Type[] types = assembly.GetTypes();
        IEnumerable<Type> subclasses = types.Where(t => t.IsSubclassOf(parentType));
        
        foreach (var subclass in subclasses)
        {
            var obj = (Tool)Activator.CreateInstance(subclass);
            allTools.Add(obj);
        }
    }
    
    public static async Task<(string result, Tool usedTool)> TryUse(Agent agent, string inputText)
    {
        if (ParseToolCommand(inputText, out var hit, out var arguments))
        {
            hit = hit.ToUpper();
            foreach (var tool in allTools)
            {
                if (tool.Hit.Trim().ToUpper() == hit)
                {
                    return (await tool.UseTool(agent, arguments), tool);
                }
            }
        }
        
        return (null, null);
    }

    private static bool ParseToolCommand(string input, out string command, out string arguments)
    {
        // Regex pattern to match "[%KEYWORD%]" and the following text
        string pattern = @"\[%(.+?)%\](.*)";

        // Perform regex match
        Match match = Regex.Match(input, pattern);

        if (match.Success)
        {
            // Extract matched groups
            command = match.Groups[1].Value;
            arguments = match.Groups[2].Value.Trim();
            return true;
        }

        command = null;
        arguments = null;
        return false;
    }
    
    public static string BuildToolList()
    {
        var report = "";
        foreach (var tool in allTools)
        {
            if (report.Length > 0) report += "\n";
            report += $"[%{tool.Hit.ToUpper()}%] {tool.SampleInput}\n";
            report += "\n";
            report += $"{tool.Description}\n";
        }

        return report;
    }

    public abstract string Hit { get; }
    public abstract string SampleInput { get; }
    public abstract string Description { get; }

    public virtual async Task<string> UseTool(Agent agent, string input) => null;

}