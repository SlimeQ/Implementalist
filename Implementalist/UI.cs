using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Implementalist;

public static class UI
{
    private static int DataLine = Console.WindowHeight - 2;
    private static ConsoleColor BorderColor = ConsoleColor.DarkGreen;
    private static ConsoleColor PromptColor = ConsoleColor.Green;
    private static ConsoleColor LogColor = ConsoleColor.White;

    private static bool enabled = true;
    public static void Start()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;
        
        if (IsLinux)
        {
            enabled = false;
            return;
        }
        
        Console.Clear();
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        // Draw();

        Heartbeat();
        RunConsoleWidthListener();
    }

    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    
    private static async Task Heartbeat()
    {
        while (true)
        {
            // EraseConsole();
            // Draw();
            // await TimeSpan.FromSeconds(10).Wait();
            
            EraseConsole();
            Draw();
            DrawSilently(() =>
            {
                SetCursorPosition(0, logEndTop + 3);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(".");
            });
            await TimeSpan.FromSeconds(1).Wait();
            
            DrawSilently(() =>
            {
                Console.SetCursorPosition(0, logEndTop + 3);
                Console.Write(" ");
            });
            
            await TimeSpan.FromSeconds(9).Wait();
        }
    }
    
    public static async Task Wait(this TimeSpan timespan)
    {
        await Task.Delay((int)timespan.TotalMilliseconds);
    }

    private static async Task RunConsoleWidthListener()
    {
        var lastWidth = Console.WindowWidth;
        while (true)
        {
            await TimeSpan.FromSeconds(1f / 5).Wait();
            var windowWidth = Console.WindowWidth;
            if (windowWidth != lastWidth)
            {
                EraseConsole();
                Draw();
            }
            lastWidth = windowWidth;
        }
    }

    private static async void RunUpdate()
    {
        while (true)
        {
            Draw();
            await TimeSpan.FromSeconds(1 / 25f).Wait();
        }
    }

    private static string inputString;
    private static int inputCursor;

    private static List<string> history = new ()
    {
        "!arm",
        "!readonly",
        "!adminonly",
    };

    private static int historyIndex = history.Count - 1;
        

    public static string ReadLine()
    {
        if (!enabled)
        {
            return Console.ReadLine();
        }
        
        inputString = "";
        
        historyIndex = history.Count;
        history.Add(inputString);

        Draw();
        StringBuilder sb = new StringBuilder();
        while (true)
        { 
            SetCursorPosition(promptPosition.Left + inputCursor, promptPosition.Top);
            Console.CursorVisible = true;
            Console.ForegroundColor = ConsoleColor.DarkRed;
            // int ch = Console.In.Read();
            // int ch = ((TextReader)Console.In).Read(buffer, 0, 1);

            var key = Console.ReadKey(true);
            int ch = key.KeyChar;

            if (key.Key == ConsoleKey.Backspace)
            {
                if (inputCursor > 0)
                {
                    sb.Remove(inputCursor - 1, 1);
                    inputString = sb.ToString();
                    inputCursor--;
                }

                ch = 0;
            }
            if (key.Key == ConsoleKey.Delete)
            {

                if (inputCursor > 0)
                {
                    sb.Remove(inputCursor, 1);
                    inputString = sb.ToString();
                }

                ch = 0;
            }

            if (key.Key == ConsoleKey.Enter)
            {
                history[history.Count - 1] = inputString;
                
                inputString = "";
                inputCursor = 0;
                // EraseConsole();
                // DrawConsole();
                FillRow(BufferBottomLine-1, ' ');
                Console.ResetColor();
                Console.CursorLeft = Prompt.Length;
                return sb.ToString();
            }

            if (key.Key == ConsoleKey.Home)
            {
                inputCursor = 0;
            }
            else if (key.Key == ConsoleKey.End)
            {
                inputCursor = inputString.Length;
            }
            
            if (key.Key == ConsoleKey.LeftArrow)
            {
                if (key.Modifiers == ConsoleModifiers.Control)
                {
                    inputCursor--;
                    inputCursor = Math.Max(inputCursor, 0);
                    while (inputCursor > 0 && inputString[inputCursor-1] != ' ')
                    {
                        inputCursor--;
                    }
                }
                else
                {
                    inputCursor--;
                    inputCursor = Math.Max(inputCursor, 0);
                }
                ch = 0;
            }
            
            else if (key.Key == ConsoleKey.RightArrow)
            {
                if (key.Modifiers == ConsoleModifiers.Control)
                {
                    inputCursor++;
                    inputCursor = Math.Min(inputCursor, inputString.Length);
                    while (inputCursor < inputString.Length && inputString[inputCursor] != ' ')
                    {
                        inputCursor++;
                        inputCursor = Math.Min(inputCursor, inputString.Length);
                    }
                }
                else
                {
                    inputCursor++;
                    inputCursor = Math.Min(inputCursor, inputString.Length);
                }
                ch = 0;
            }

            if (key.Key == ConsoleKey.UpArrow)
            {
                historyIndex--;
                if (historyIndex < 0) historyIndex = 0;

                inputString = history[historyIndex];
                sb.Clear();
                sb.Append(inputString);
                if (inputCursor > inputString.Length) inputCursor = inputString.Length;
                
                ch = 0;
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                historyIndex++;
                if (historyIndex > history.Count-1) historyIndex = history.Count-1;

                inputString = history[historyIndex];
                sb.Clear();
                sb.Append(inputString);
                
                ch = 0;
            }

            if (ch != 0)
            {
                sb.Insert(inputCursor, (char)ch);
                inputString = sb.ToString();
                inputCursor++;

                historyIndex = history.Count - 1;
                history[historyIndex] = inputString;
            }
            // WriteLine(inputString);
            EraseConsole();
            Draw();
        }
    }

    private const string Prompt = " > ";
    private static bool isDrawing = false;
    public static void Draw()
    {
        if (consoleExists) return;

        consoleWidth = Console.WindowWidth-5;
        
        DrawSilently(() =>
        {
            FillRow(logEndTop, consoleWidth, '-', BorderColor);
            CleanWriteRow(logEndTop + 1, consoleWidth, Prompt + inputString, ConsoleColor.DarkRed);
            promptPosition = (Prompt.Length, logEndTop + 1);
            FillRow(logEndTop+2, consoleWidth, '-', BorderColor);
            PrintDataLayer();
            FillRow(logEndTop+4, consoleWidth, '-', BorderColor);
        });
        SetCursorPosition(promptPosition.Left + inputCursor, promptPosition.Top);
        consoleExists = true;
    }

    private static int DataLayerStart = 2 + ProgressBarLength + 5 + 2;
    
    private struct DataLayerCell
    {
        public ConsoleColor color;
        public int width = 8;
        public string body;
        
        public DataLayerCell()
        {
            color = ConsoleColor.Red;
            body = "";
            width = 8;
        }

        public DataLayerCell(string body, int width = 8, ConsoleColor color = ConsoleColor.Red)
        {
            this.color = color;
            this.body = body;
            this.width = width;
        }
    }
    private static void PrintDataLayer()
    {
        // DrawSilently(() =>
        // {
        //     DataLayerCell[] cells = new[]
        //     {
        //         new DataLayerCell(Scoob.armed ? "ARMED" : "UNARMED", 7, !Scoob.armed ? ConsoleColor.Green : ConsoleColor.Red),
        //         new DataLayerCell(Scoob.readOnly ? "READONLY" : "CANWRITE", 8, Scoob.readOnly ? ConsoleColor.Green : ConsoleColor.Red),
        //         new DataLayerCell(Scoob.adminOnly ? "ADMINONLY" : "PUBLIC", 9, Scoob.adminOnly ? ConsoleColor.Green : ConsoleColor.Red),
        //         
        //         new DataLayerCell($"Default: ${Scoob.defaultRateLimiter.CurrentBudget:F2}", 16, ConsoleColor.Green),
        //         new DataLayerCell($"Doghouse: ${Scoob.doghouseRateLimiter.CurrentBudget:F2}", 16, ConsoleColor.Green),
        //     };
        //     
        //     PrintProgressBar();
        //     
        //     SetCursorPosition(DataLayerStart, logEndTop + 3);
        //     Console.ResetColor();
        //
        //     for (int i = 0; i < cells.Length; i++)
        //     {
        //         Console.ForegroundColor = ConsoleColor.DarkGreen;
        //         Console.Write(" | ");
        //         
        //         var cell = cells[i];
        //         Console.ForegroundColor = cell.color;
        //         Console.Write(cell.body.PadLeft(cell.width));
        //     }
        // });
    }

    private static void WriteOnBottomLine(string text)
    {
        DrawSilently(() =>
        {
            Console.CursorTop = Console.WindowTop + Console.WindowHeight - 1;
            Console.Write(text);
        });
    }

    private static void FillRow(char character, ConsoleColor color = ConsoleColor.White)
    {
        FillRow(Console.CursorTop, character, color);
    }

    private static void FillRow(int row, char character, ConsoleColor color = ConsoleColor.White)
    {
        FillRow(row, Console.WindowWidth, ' ');
    }

    private static void FillRow(int row, int width, char character, ConsoleColor color = ConsoleColor.White)
    {
        DrawSilently(() =>
        {
            Console.ForegroundColor = color;
            SetCursorPosition(0, row);

            var line = new string(character, width - 1);
            Console.Write(line);
        });
    }

    private static void SetCursorPosition(int left, int top)
    {
        if (top >= Console.BufferHeight)
        {
            Console.BufferHeight = top + 1;
        }
        try
        {
            Console.SetCursorPosition(left, top);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Console.WriteLine($"tried to move cursor to ({left}, {top}), max is ({Console.BufferWidth}, {Console.BufferHeight})");
            Console.WriteLine(ex.ToString());
            Environment.Exit(0);
        }
    }

    private static void CleanWriteRow(int row, string startString, ConsoleColor color = ConsoleColor.White)
    {
        CleanWriteRow(row, Console.WindowWidth, startString, color);
    }

    private static void CleanWriteRow(int row, int width, string startString, ConsoleColor color = ConsoleColor.White)
    {
        DrawSilently(() =>
        {
            Console.ForegroundColor = color;
            SetCursorPosition(0, row);

            var line = startString + new string(' ', width - 1 - startString.Length);
            Console.Write(line);
        });
    }

    private static float currentProgress;

    private const int ProgressBarLength = 32;
    public static void PrintProgressBar(float normalized)
    {
        if (!enabled) return;
        
        currentProgress = normalized;
        PrintProgressBar();
    }
    private static void PrintProgressBar()
    {
        if (!enabled) return;
        
        DrawSilently(() =>
        {
            var pos = Console.GetCursorPosition();
            pos.Top = logEndTop + 3;
            pos.Left = 2;
            
            // Draw progress bar
            SetCursorPosition(pos.Left, pos.Top);

            Console.ForegroundColor = BorderColor;
            Console.CursorLeft = pos.Left + 0;
            Console.Write("[");
            Console.CursorLeft = pos.Left + ProgressBarLength;
            Console.Write("]");
            
            if (currentProgress < 0)
            {
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.CursorLeft = pos.Left + 1;
                Console.Write(new string('-', ProgressBarLength-1));
                Console.CursorLeft = pos.Left + ProgressBarLength + 1;
                Console.Write(" idle");
            }
            else
            {
                // Draw progress bar
                var alpha = Math.Clamp(currentProgress, 0f, 1);
                var chunksFilled = Math.Floor(alpha * (ProgressBarLength-1));
                
                int i = (int)(100f * Math.Clamp(currentProgress, 0f, 1));
                
                float onechunk = 30f / 100f;
            
                // Draw chunks  
                int position = 1;
            
                
                for (int j = 1; j < ProgressBarLength; j++)
                {
                    Console.CursorLeft = pos.Left + j;
                    if (j <= chunksFilled) 
                        Console.BackgroundColor = ConsoleColor.DarkYellow; 
                    else 
                        Console.ResetColor();
                    
                    Console.Write(" ");
                }

                //Draw totals  
                Console.CursorLeft = pos.Left + ProgressBarLength + 1;
                Console.ResetColor();
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($" {i}%".PadLeft(5)); //Print percentage complete    
            }
        });
    }

    public static void EndProgressBar()
    {
        if (!enabled) return;
        
        PrintProgressBar(-1);
    }
    
    public static List<string> WordWrap(string text, int width)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();
        
        const string forcedBreakZonePattern = @"\n";
        const string normalBreakZonePattern = @"\s+|(?<=[-,.;])|$";

        var forcedZones = Regex.Matches(text, forcedBreakZonePattern).ToList();
        var normalZones = Regex.Matches(text, normalBreakZonePattern).ToList();

        int start = 0;

        var lines = new List<string>();
        
        while (start < text.Length)
        {
            var zone = 
                forcedZones.Find(z => z.Index >= start && z.Index <= start + width) ??
                normalZones.FindLast(z => z.Index >= start && z.Index <= start + width);

            if (zone == null)
            {
                lines.Add(text.Substring(start, width));
                start += width;
            }
            else
            {
                lines.Add(text.Substring(start, zone.Index - start));
                start = zone.Index + zone.Length;
            }
        }

        return lines;
    }

    private static void DrawSilently(Action action)
    {
        var cursorVisible = GetCursorVisible();
        var cursorPos = Console.GetCursorPosition();
        var foregroundColor = Console.ForegroundColor;
        var backgroundColor = Console.BackgroundColor;

        Console.ResetColor();
        Console.CursorVisible = false;

        action.Invoke();

        SetCursorVisible(cursorVisible);
        SetCursorPosition(cursorPos.Left, cursorPos.Top);
        Console.ForegroundColor = foregroundColor;
        Console.BackgroundColor = backgroundColor;
    }

    private static void SetCursorVisible(bool visible)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.CursorVisible = visible;
            return;
        }
        if (IsLinux)
        {
            return;
        }
    }
    
    private static bool GetCursorVisible()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Console.CursorVisible;
        }
        if (IsLinux)
        {
            return true;
        }
        return true;
    }

    private static (int Left, int Top) promptPosition;

    private static int MaxLogLength = 30000;
    private static string log;
    
    private static int BufferBottomLine => Console.WindowTop + Console.WindowHeight - 1;

    private static int logEndTop;
    public static void WriteLine(string s="", ConsoleColor? color=null)
    {
        if (color == null)
        {
            color = LogColor;
        }
        
        if (!enabled)
        {
            Console.WriteLine(s);
            return;
        }
        
        // await WaitForNotDrawing();
        
        EraseConsole();
        DrawSilently(() =>
        {
            // Console.CursorTop = BufferBottomLine;
            SetCursorPosition(0, logEndTop);
            Console.ForegroundColor = color.Value;

            if (s != null && s.StartsWith("backend |"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }

            Console.WriteLine(s);
            logEndTop = Console.CursorTop;
        });
        Draw();


        // DrawConsole();

        // log += s + "\n";
        //
        // var sliceStart = log.Length - MaxLogLength;
        // if (sliceStart >= 0 && sliceStart < log.Length)
        // {
        //     log = log.Substring(sliceStart);
        // }
        //
        // DrawSilently(() =>
        // {
        //     DrawLog();
        //     DrawBorder(); 
        // });
    }
    
    public static void Write(string s="", ConsoleColor? color=null)
    {
        if (color == null)
        {
            color = LogColor;
        }
        
        if (!enabled)
        {
            Console.Write(s);
            return;
        }
        
        EraseConsole();
        DrawSilently(() =>
        {
            SetCursorPosition(0, logEndTop);
            Console.ForegroundColor = color.Value;

            if (s != null && s.StartsWith("backend |"))
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }

            Console.Write(s);
            logEndTop = Console.CursorTop;
        });
        Draw();
    }

    private static bool consoleExists;
    private static int consoleWidth;
    private static void EraseConsole()
    {
        if (!consoleExists) return;
        DrawSilently(() =>
        {
            Console.ResetColor();

            // var newWidth = Console.WindowWidth;
            // var lines = (consoleWidth * 5) / newWidth;
            
            for (int i = 0; i < 5; i++)
            {
                FillRow(logEndTop + i, consoleWidth, ' ');
            }
        });
        consoleExists = false;
    }

    public static void IndicateMessageReceived()
    {
        
    }

    private static void WriteAtPosition(string s, (int Left, int Top) pos)
    {
        var originalCursorVisible = Console.CursorVisible;
        var originalCursorPos = Console.GetCursorPosition();
        
        Console.CursorVisible = false;
        SetCursorPosition(pos.Left, pos.Top);
        Console.Write(s);

        Console.CursorVisible = originalCursorVisible;
        SetCursorPosition(originalCursorPos.Left, originalCursorPos.Top);
    }
}