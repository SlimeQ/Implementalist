using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;

namespace Implementalist.Tools
{
    public class BashTool : Tool
    {
        private const bool PromptForSudo = true;
        
        public override string Hit => "bash";
        public override string SampleInput => "<command>";
        public override string Description => "Runs a bash command on the host machine. It is running Ubuntu 22.04. You will need human intervention to use sudo. Avoid interactive prompts whenever possible.";
        // public override bool WritesOutputToUI => true;

        public override async Task<string> UseTool(Agent user, string input)
        {
            if (!user.IsLoggedIn) await user.Login(Secrets.LINUX_HOST, Secrets.LINUX_USER, Secrets.LINUX_PASS);
            if (input == "exit")
            {
                user.Logout();
                return "0";
            }

            var blacklistResult = IsBlacklisted(input);
            if (blacklistResult != null)
            {
                return blacklistResult;
            }

            return await user.RunCommand(input);
        }

        private string[] blacklistedCommands = new[]
        {
            "nano",
            "vi",
            "vim"
        };
        private string IsBlacklisted(string command)
        {
            var splitCommand = command.Split(" ");
            foreach (var cmd in blacklistedCommands)
            {
                if (splitCommand.Contains(cmd))
                {
                    return $"{cmd} is BLACKLISTED by the agent infrastructure. Find a different way to do this.";
                }
            }
            return null;
        }
        
        private async Task<string> ExecuteLocalCommand(string input)
        {
            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"{input}\"";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            Console.WriteLine(e.Data);
                        }
                    };

                    process.Start();

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    if (input.StartsWith("sudo"))
                    {
                        Console.Write("Please enter your password: ");
                        StringBuilder password = new StringBuilder();
                        while (true)
                        {
                            ConsoleKeyInfo key = Console.ReadKey(true);
                            if (key.Key == ConsoleKey.Enter)
                            {
                                Console.WriteLine();
                                break;
                            }
                            password.Append(key.KeyChar);
                        }
                        using (StreamWriter sw = process.StandardInput)
                        {
                            sw.WriteLine(password.ToString());
                        }
                    }

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        return $"Error (exit code {process.ExitCode})";
                    }

                    return $"Command executed successfully (exit code {process.ExitCode})";
                }
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
        
        private async Task<string> ExecuteRemoteCommand(string input)
        {
            try
            {
                StringBuilder result = new StringBuilder();

                using (var sshClient = new SshClient(Secrets.LINUX_HOST, Secrets.LINUX_USER, Secrets.LINUX_PASS))
                {
                    sshClient.Connect();

                    if (input.StartsWith("sudo"))
                    {
                        // ... (previous sudo handling code, unchanged)
                    }

                    using (var shellStream = sshClient.CreateShellStream("custom", 80, 24, 800, 600, 1024))
                    {
                        var reader = new StreamReader(shellStream);
                        var writer = new StreamWriter(shellStream) { AutoFlush = true };

                        // Ignore initial messages from the remote server.
                        while (true)
                        {
                            string line = await reader.ReadLineAsync();
                            if (line == null || line.EndsWith("Last login:"))
                            {
                                break;
                            }
                        }

                        writer.WriteLine(input);
                        writer.WriteLine("echo 'END_OF_COMMAND_OUTPUT'");

                        while (true)
                        {
                            string line = await reader.ReadLineAsync();
                            if (line == null || line == "END_OF_COMMAND_OUTPUT")
                            {
                                break;
                            }
                            result.AppendLine(line);
                        }
                    }

                    sshClient.Disconnect();
                }

                return result.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }

    }
}
