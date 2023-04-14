using System.Text;
using AI.Dev.OpenAI.GPT;
using OpenAI_API;
using OpenAI_API.Chat;
using Renci.SshNet;

namespace Implementalist;

public class Agent
{
    private static int nextAgentId;

    public Agent owner;
    private int id;
    private string goal;
    private List<ChatMessage> messages;
    public Agent(Agent owner, string goal)
    {
        id = nextAgentId++;
        this.owner = owner;
        this.goal = goal;
        messages = new List<ChatMessage>
        {
            new ChatMessage()
            {
                Role = ChatMessageRole.System, 
                Content = Program.BuildSystemMessage(goal)
            }
        };
    }
    
    public async Task<string> GetResult()
    {
        // Use chat completion API
        string finalAnswer = null;
        while (finalAnswer == null)
        {
            var request = new ChatRequest()
            {
                Model = "gpt-3.5-turbo",
                Messages = messages,
                Temperature = 0.7,
                MaxTokens = 500,
                NumChoicesPerMessage = 1,
                StopSequence = null
            };
            
            // Consume the asynchronous enumerable returned by StreamChatEnumerableAsync
            var result = await Program.openAIClient.Chat.CreateChatCompletionAsync(request);
            var message = result.Choices[0].Message.Content;
            messages.Add(new ChatMessage() { Role = ChatMessageRole.Assistant, Content = message });
            UI.WriteLine($"<agent {id}> {message}", ConsoleColor.DarkGreen);

            foreach (var line in message.Split("\n"))
            {
                var toolResponse = await Tool.TryUse(this, line);
                if (toolResponse.result != null)
                {
                    var truncated = TruncateToTokensReverse(toolResponse.result, 2000);
                    if (truncated.Length != toolResponse.result.Length)
                    {
                        toolResponse.result = $"... {truncated}";
                    }

                    if (toolResponse.usedTool.Hit == "end")
                    {
                        return toolResponse.result;
                    }
                    
                    if (!toolResponse.usedTool.WritesOutputToUI)
                        UI.WriteLine(toolResponse.result);
                    
                    messages.Add(new ChatMessage()
                    {
                        Role = ChatMessageRole.User,
                        Name = toolResponse.usedTool.Hit.ToUpper(),
                        Content = toolResponse.result
                    });
                }
            }
        }
        UI.WriteLine($"Final Answer: {finalAnswer}");
        return finalAnswer;
    }
    
    public static int CountTokens(string input)
    {
        List<int> tokens = GPT3Tokenizer.Encode(input);
        var tokenizerEstimate = tokens.Count;
        return tokenizerEstimate;
    }
    
    public static string TruncateToTokens(string input, int maxTokens)
    {
        if (string.IsNullOrEmpty(input) || maxTokens <= 0)
        {
            return "";
        }
    
        int tokens = 0;
        StringBuilder truncatedContent = new StringBuilder();
    
        for (int i = 0; i < input.Length && tokens < maxTokens; i++)
        {
            char c = input[i];
            truncatedContent.Append(c);
            CountTokens(truncatedContent.ToString());
            
            if (char.IsWhiteSpace(c) || char.IsSeparator(c))
            {
                tokens++;
            }
        }
    
        return truncatedContent.ToString();
    }
    public static string TruncateToTokensReverse(string input, int maxTokens)
    {
        if (string.IsNullOrEmpty(input) || maxTokens <= 0)
        {
            return "";
        }

        if (CountTokens(input) <= maxTokens)
        {
            return input;
        }
    
        int tokens = 0;
        StringBuilder truncatedContent = new StringBuilder();
        // string truncatedContent = "";
    
        for (int i = input.Length-1; i >= 0 && tokens < maxTokens; i--)
        {
            char c = input[i];
            truncatedContent.Insert(0, c);
            tokens = CountTokens(truncatedContent.ToString());
        }
    
        return truncatedContent.ToString();
    }
    
    // private SshClient SSHClient { get; set; }
    // private ShellStream sshStream;
    // private StreamWriter sshWriter;
    // private StreamReader sshReader;

    private SSH sshClient;
    
    public async Task<string> Login(string host, string user, string pass)
    {
        sshClient = new SSH();
        sshClient.InteractivePrompt = async (lastChunk) =>
        {
            messages.Add(new ChatMessage()
            {
                Role = ChatMessageRole.User,
                Content = lastChunk,
                Name = "BASH"
            });
            return await Respond();
        };
        return sshClient.Connect(host, 22, user, pass);

        // SSHClient = new SshClient(host, user, pass);
        // SSHClient.Connect();
        // sshStream = SSHClient.CreateShellStream("custom", 80, 24, 800, 600, 1024);
        // return sshStream.Read(); // await CaptureCommandOutput();
    }

    // private async Task<string> CaptureCommandOutput(string command=null)
    // {
    //     UI.WriteLine("_________CAPTURING_________");
    //     
    //     var response = "";
    //     bool isFirst = command != null;
    //     while (true) // (response.Length == 0 || !sshReader.EndOfStream)
    //     {
    //         string line = sshStream.ReadLine();
    //         UI.WriteLine(line);
    //         if (isFirst)
    //         {
    //             if (string.IsNullOrWhiteSpace(line)) continue;
    //             if (line == command || line.EndsWith($"$ {command}"))
    //             {
    //                 isFirst = false;
    //                 continue;
    //             }
    //         }
    //
    //         if (line != null && line.Contains($"{Secrets.LINUX_USER}@{Secrets.LINUX_HOST}") && line.Trim().EndsWith("$"))
    //         {
    //             // we hit prompt
    //             UI.WriteLine("^_^_^_^_^_^_^_^_^_^_^_^_^_^");
    //             return response.Trim();
    //         }
    //         
    //         if (response.Length > 0) response += "\n";
    //         response += line;
    //     }
    //     UI.WriteLine("___________________________");
    //     return response.Trim();
    // }

    public void Logout()
    {
        sshClient.Disconnect();
        sshClient = null;
        
        // if (sshStream != null)
        // {
        //     sshStream.Dispose();
        //     sshStream = null;
        // }
        //
        // if (SSHClient != null)
        // {
        //     SSHClient.Disconnect();
        //     SSHClient.Dispose();
        //     SSHClient = null;
        // }
    }

    public bool IsLoggedIn => sshClient != null && sshClient.IsLoggedIn;

    public async Task<string> RunCommand(string bashCommand)
    {
        if (sshClient != null)
        {
            return await sshClient.Execute(bashCommand, 30);
        }
        //
        // if (SSHClient != null)
        // {
        //     StringBuilder result = new StringBuilder();
        //     string input;
        //
        //     if (bashCommand.StartsWith("sudo"))
        //     {
        //         UI.WriteLine($"> {bashCommand}");
        //     //     if (await Program.PromptForApproval())
        //     //     {
        //     //         bashCommand = $"echo {Secrets.LINUX_PASS} | {bashCommand} -S";
        //     //     }
        //     //     else
        //     //     {
        //     //         return "DENIED";
        //     //     }
        //     }
        //
        //     var garbage = sshStream.Read();
        //     UI.WriteLine($"garbage={garbage}");
        //     sshStream.WriteLine(bashCommand);
        //     // var garbage = await sshReader.ReadToEndAsync();
        //     while (!sshStream.EndOfStream)
        //     {
        //         var line = await sshReader.ReadLineAsync();
        //         UI.WriteLine($"|?> {line}");
        //         if (line.StartsWith("[sudo] password for") || line.StartsWith("Sorry, try again"))
        //         {
        //             UI.WriteLine(line);
        //             if (await Program.PromptForApproval())
        //             {
        //
        //                 sshWriter.Flush();
        //                 sshWriter.WriteLineAsync(Secrets.LINUX_PASS);
        //                 var password = await sshReader.ReadToEndAsync();
        //                 UI.WriteLine($"password={password}");
        //                 garbage = await sshReader.ReadToEndAsync();
        //                 UI.WriteLine($"garbage={garbage}");
        //                 break;
        //             }
        //         }
        //     }
        //     return await CaptureCommandOutput(bashCommand);
        // }
        else
        {
            return "ERROR: NOT LOGGED IN";
        }
    }

    public async Task<string> RespondToChild(string question)
    {
        var fullMessage = $"AGENT: {question}";
        UI.WriteLine(fullMessage);
        messages.Add(new ChatMessage(ChatMessageRole.Assistant,fullMessage));
        return await Respond();
    }

    protected virtual async Task<string> Respond()
    {
        var request = new ChatRequest()
        {
            Model = "gpt-3.5-turbo",
            Messages = messages,
            Temperature = 0.7,
            MaxTokens = 500,
            NumChoicesPerMessage = 1,
            StopSequence = null
        };
        
        // Consume the asynchronous enumerable returned by StreamChatEnumerableAsync
        var result = await Program.openAIClient.Chat.CreateChatCompletionAsync(request);
        var message = result.Choices[0].Message.Content;
        messages.Add(new ChatMessage() { Role = ChatMessageRole.Assistant, Content = message });
        UI.WriteLine($"<agent {id}> {message}");
        return message;
    }
}