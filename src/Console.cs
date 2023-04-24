using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.ConsoleColor;

namespace SBSimulator.src
{
    class ConsoleEventLoop
    {
        public delegate void ConsoleEventHandler(string order);
        public event ConsoleEventHandler OnOrdered = delegate { };

        public ConsoleEventLoop() { }
        public ConsoleEventLoop(ConsoleEventHandler onOrdered)
        {
            OnOrdered += onOrdered;
        }
        public Task Start(CancellationToken ct)
        {
            return Task.Run(() => EventLoop(ct), ct);
        }
        void EventLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                string order = Console.ReadLine() ?? string.Empty;
                OnOrdered(order);
            }
        }
    }
    class Window
    {
        public ColoredString HelpField { get; set; } = string.Empty;
        public string StatusFieldPlayer1 { get; set; } = string.Empty;
        public string StatusFieldPlayer2 { get; set; } = string.Empty;
        public string WordFieldPlayer1 { get; set; } = string.Empty;
        public string WordFieldPlayer2 { get; set; } = string.Empty;
        public MessageBox Message { get; set; } = MessageBox.Empty;
        static string LINE
        {
            get
            {
                string result = string.Empty;
                for (var i = 0; i < Console.WindowWidth; i++)
                {
                    result += "-";
                }
                return result;
            }
        }
        public void WriteLine()
        {
            Console.Clear();
            HelpField.WriteLine();
            Console.WriteLine(LINE);
            Console.WriteLine(StatusFieldPlayer1);
            Console.WriteLine(StatusFieldPlayer2);
            Console.WriteLine(LINE);
            Console.WriteLine("  " + WordFieldPlayer1);
            Console.WriteLine();
            Console.WriteLine("  " + WordFieldPlayer2);
            Console.WriteLine(LINE);
            Message.WriteLine();
            Console.WriteLine(LINE);
        }
        public Window()
        {
            HelpField = new ColoredString("\"help\" と入力するとヘルプを表示します", Green);
            Message = new MessageBox();
        }
    }
    class MessageBox
    {
        public static MessageBox Empty => new() { Content = new List<ColoredString>(10).Fill(string.Empty), Log = new() };
        public List<ColoredString> Content { get; private set; } = new List<ColoredString>(10).Fill(string.Empty);
        public MessageLog Log { get; private set; } = new MessageLog();
        public MessageBox()
        {
            Content = new List<ColoredString>(10).Fill(string.Empty);
            Log = new();
        }
        public void Append(ColoredString s)
        {
            Content.Add(s);
            Content.RemoveAt(0);
            Log.Append(s);
        }
        public void Append(string text, ConsoleColor color)
        {
            Append(new ColoredString(text, color));
        }
        public void Clear()
        {
            Content = new List<ColoredString>(10).Fill(string.Empty);
        }
        public void WriteLine()
        {
            foreach (ColoredString s in Content)
            {
                s.WriteLine();
            }
        }
    }
    class MessageLog
    {
        public List<ColoredString> Content { get; private set; } = new() { string.Empty };
        public MessageLog()
        {
            Content = new() { string.Empty };
        }
        public void Append(ColoredString s)
        {
            Content.Add(s);
        }
        public void Append(string text, ConsoleColor color)
        {
            Append(new ColoredString(text, color));
        }
        public void AppendMany(IEnumerable<ColoredString> strs)
        {
            foreach (var s in strs)
            {
                Content.Add(s);
            }
        }
        public void Clear()
        {
            Content = new() { string.Empty };
        }
        public void WriteLine()
        {
            foreach (ColoredString s in Content)
            {
                s.WriteLine();
            }
        }
    }
    class ColoredString
    {
        public string Text { get; set; } = string.Empty;
        public ConsoleColor Color { get; set; } = White;
        public ColoredString(string text, ConsoleColor color) => (Text, Color) = (text, color);
        public ColoredString() : this(string.Empty, White) { }
        public void WriteLine()
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = Color;
            Console.WriteLine(Text);
            Console.ForegroundColor = defaultColor;
        }
        public void Write()
        {
            var defaultColor = Console.ForegroundColor;
            Console.ForegroundColor = Color;
            Console.Write(Text);
            Console.ForegroundColor = defaultColor;
        }
        public static implicit operator ColoredString(string text) => new(text, White);
    }
}
