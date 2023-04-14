using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Implementalist;
public class Log
{
    public static void Verbose(string message) =>
        UI.WriteLine(message);

    public static void Error(string message) =>
        UI.WriteLine(message, ConsoleColor.DarkRed);
}

public static class StringExt
{
    public static string StringBeforeLastRegEx(this string str, Regex regex)
    {
        var matches = regex.Matches(str);

        return matches.Count > 0
            ? str.Substring(0, matches.Last().Index)
            : str;

    }

    public static bool EndsWithRegEx(this string str, Regex regex)
    {
        var matches = regex.Matches(str);

        return
            matches.Count > 0 &&
            str.Length == (matches.Last().Index + matches.Last().Length);
    }

    public static string StringAfter(this string str, string substring)
    {
        var index = str.IndexOf(substring, StringComparison.Ordinal);

        return index >= 0
            ? str.Substring(index + substring.Length)
            : "";
    }

    public static string[] GetLines(this string str) =>
        Regex.Split(str, "\r\n|\r|\n");
}

public static class UtilExt
{
    public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> func) 
    {
        foreach (var item in sequence)
        {
            func(item);
        }
    }
}

public class SSH
{
    SshClient sshClient;
    ShellStream shell;
    string pwd = "";
    string lastCommand = "";

    static Regex prompt = new Regex("[a-zA-Z0-9_.-]*\\@[a-zA-Z0-9_.-]*\\:\\~[#$] ", RegexOptions.Compiled);
    static Regex pwdPrompt = new Regex("\\[sudo\\] password for .*\\:", RegexOptions.Compiled);
    static Regex promptOrPwd = new Regex(prompt + "|" + pwdPrompt);
    public bool IsLoggedIn => sshClient != null;

    public string Connect(string host, int port, string user, string pwd)
    {
        // Log.Verbose($"Connect Ssh: {user}@{host}:{port}");

        var connectionInfo =
            new ConnectionInfo(
                host,
                port,
                user,
                new PasswordAuthenticationMethod(user, pwd));

        this.pwd = pwd;
        sshClient = new SshClient(connectionInfo);
        sshClient.Connect();

        var terminalMode = new Dictionary<TerminalModes, uint>();
        terminalMode.Add(TerminalModes.ECHO, 53);

        shell = sshClient.CreateShellStream("", 0, 0, 0, 0, 4096, terminalMode);

        try
        {
            var intro = Expect(prompt);
            return intro;
        }
        catch (Exception ex)
        {
            Log.Error("Exception - " + ex.Message);
            throw;
        }
    }

    public void Disconnect()
    {
        // Log.Verbose($"Ssh Disconnect");

        sshClient?.Disconnect();
        sshClient = null;
    }

    void WriteLine(string commandLine)
    {
        // Console.ForegroundColor = ConsoleColor.Green;
        // Log.Verbose("> " + commandLine);
        // Console.ResetColor(); 

        lastCommand = commandLine;

        shell.WriteLine(commandLine);
    }

    string Expect(Regex expect, double timeoutSeconds = 60.0)
    {
        var result = shell.Expect(expect, TimeSpan.FromSeconds(timeoutSeconds));

        if (result == null)
        {
            result = shell.Read();
            // throw new Exception($"Timeout {timeoutSeconds}s executing {lastCommand}");
        }

        result = result.Contains(lastCommand) ? result.StringAfter(lastCommand) : result;
        result = result.StringBeforeLastRegEx(prompt);
        result = result.Trim();

        // result.GetLines().ForEach(x => Log.Verbose(x));

        return result;
    }

    public Func<string, Task<string>> InteractivePrompt;
    
    public async Task<string> Execute(string commandLine, double timeoutSeconds = 30.0)
    {
        Exception exception = null;
        var result = "";
        var errorMessage = "failed";
        var errorCode = "exception";

        try
        {
            WriteLine(commandLine);
            bool hasHitPrompt = false;
            do
            {
                var startExpecting = DateTime.Now;
                result = Expect(promptOrPwd, timeoutSeconds);
                if (DateTime.Now >= startExpecting + TimeSpan.FromSeconds(timeoutSeconds))
                {
                    // timeout
                    UI.WriteLine(result);
                    if (InteractivePrompt != null)
                        shell.WriteLine(await InteractivePrompt.Invoke(result));
                }
                else
                {
                    hasHitPrompt = true;
                }
            } while (!hasHitPrompt);

            if (result.EndsWithRegEx(pwdPrompt))
            {
                if (await Program.PromptForApproval(result))
                {
                    WriteLine(pwd);
                    result = Expect(prompt);
                }
                else
                {
                    WriteLine("garbage");
                }
            }

            WriteLine("echo $?");
            errorCode = Expect(prompt);

            if (errorCode == "0")
            {
                return result;    
            }
            else if (result.Length > 0)
            {
                errorMessage = result;
            }
        }
        catch (Exception ex)
        {
            exception = ex;
            errorMessage = ex.Message;
        }

        // UI.WriteLine(new Exception($"Ssh error: {errorMessage}, code: {errorCode}, command: {commandLine}", exception).ToString());
        return errorMessage;
    }
}