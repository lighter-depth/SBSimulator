using static System.ConsoleColor;
using static SBSimulator.Source.Notice;

namespace SBSimulator.Source;
/// <summary>
/// コンソール ウインドウに表示する情報を管理するクラスです。
/// </summary>
class Window
{
    /// <summary>
    /// ウインドウ最上部に表示する情報
    /// </summary>
    public ColoredString HelpField { get; set; } = string.Empty;
    /// <summary>
    /// プレイヤー１の状態を管理するフィールド
    /// </summary>
    public string StatusFieldPlayer1 { get; set; } = string.Empty;
    /// <summary>
    /// プレイヤー２の状態を管理するフィールド
    /// </summary>
    public string StatusFieldPlayer2 { get; set; } = string.Empty;
    /// <summary>
    /// プレイヤー１の単語を管理するフィールド
    /// </summary>
    public string WordFieldPlayer1 { get; set; } = string.Empty;
    /// <summary>
    /// プレイヤー２の単語を管理するフィールド
    /// </summary>
    public string WordFieldPlayer2 { get; set; } = string.Empty;
    /// <summary>
    /// ウインドウ中央部に表示する<see cref="MessageBox"/>の情報
    /// </summary>
    public MessageBox Message { get; set; } = MessageBox.Empty;
    /// <summary>
    /// フィールド間の区切りに使用する線
    /// </summary>
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
    /// <summary>
    /// コンソール ウインドウに情報を出力します。
    /// </summary>
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
/// <summary>
/// 複数の色付き文字列を管理するためのクラスです。
/// </summary>
class MessageBox
{
    /// <summary>
    /// 空の<see cref="MessageBox"/>要素を表します。
    /// </summary>
    public static MessageBox Empty => new() { Content = new List<ColoredString>(10).Fill(string.Empty), Log = new() };
    /// <summary>
    /// メッセージボックスの本体
    /// </summary>
    public List<ColoredString> Content { get; private set; } = new List<ColoredString>(10).Fill(string.Empty);
    /// <summary>
    /// 追加された色付き文字列を管理するログ
    /// </summary>
    public MessageLog Log { get; private set; } = new MessageLog();
    public MessageBox()
    {
        Content = new List<ColoredString>(10).Fill(string.Empty);
        Log = new();
    }
    /// <summary>
    /// <see cref="Content"/>の末尾に要素を追加し、先頭から要素を削除します。
    /// </summary>
    public void Append(ColoredString s)
    {
        Content.Add(s);
        Content.RemoveAt(0);
    }
    /// <summary>
    /// <see cref="Content"/>の末尾に要素を追加し、先頭から要素を削除します。
    /// </summary>
    public void Append(string text, ConsoleColor color)
    {
        Append(new ColoredString(text, color));
    }
    /// <summary>
    /// <see cref="Content"/>に複数の要素を追加します。
    /// </summary>
    public void AppendMany(IEnumerable<ColoredString> items)
    {
        foreach(var i in items)
            Append(i);
    }
    /// <summary>
    /// <see cref="Content"/>の中身を空にします。
    /// </summary>
    public void Clear()
    {
        Content = new List<ColoredString>(10).Fill(string.Empty);
    }
    /// <summary>
    /// <see cref="Content"/>を出力します。
    /// </summary>
    public void WriteLine()
    {
        foreach (ColoredString s in Content)
        {
            s.WriteLine();
        }
    }
}
/// <summary>
/// ログとして保存される色付き文字列の情報を管理するクラスです。
/// </summary>
class MessageLog
{
    /// <summary>
    /// ログの本体
    /// </summary>
    public List<ColoredString> Content { get; private set; } = new() { string.Empty };
    public MessageLog()
    {
        Content = new() { string.Empty };
    }
    /// <summary>
    /// <see cref="Content"/>の末尾に要素を追加します。
    /// </summary>
    public void Append(ColoredString s)
    {
        Content.Add(s);
    }
    /// <summary>
    /// <see cref="Content"/>の末尾に要素を追加します。
    /// </summary>
    public void Append(string text, ConsoleColor color)
    {
        Append(new ColoredString(text, color));
    }
    /// <summary>
    /// <see cref="Content"/>の末尾に複数の要素を追加します。
    /// </summary>
    public void AppendMany(IEnumerable<ColoredString> strs)
    {
        foreach (var s in strs)
        {
            Content.Add(s);
        }
    }
    /// <summary>
    /// <see cref="Content"/>のを空にします。
    /// </summary>
    public void Clear()
    {
        Content = new() { string.Empty };
    }
    /// <summary>
    /// <see cref="Content"/>を出力します。
    /// </summary>
    public void WriteLine()
    {
        foreach (ColoredString s in Content)
        {
            s.WriteLine();
        }
    }
}
/// <summary>
/// 色付き文字列を表すクラスです。
/// </summary>
class ColoredString
{
    /// <summary>
    /// 文字列の本体
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// 文字列の色
    /// </summary>
    public ConsoleColor Color { get; set; } = White;
    public ColoredString(string text, ConsoleColor color) => (Text, Color) = (text, color);
    public ColoredString() : this(string.Empty, White) { }
    /// <summary>
    /// 色付き文字列を出力します。
    /// </summary>
    public void WriteLine()
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = Color;
        Console.WriteLine(Text);
        Console.ForegroundColor = defaultColor;
    }
    /// <summary>
    /// 色付き文字列を、末尾の改行を行わずに出力します。
    /// </summary>
    public void Write()
    {
        var defaultColor = Console.ForegroundColor;
        Console.ForegroundColor = Color;
        Console.Write(Text);
        Console.ForegroundColor = defaultColor;
    }
    public override string ToString()
    {
        return Text + " " + Color;
    }
    public static implicit operator ColoredString(string text) => new(text, White);
}
/// <summary>
/// アノテーションの種類を表します。
/// </summary>
public enum Notice
{
    /// <summary>
    /// 情報を持たないアノテーションです。
    /// </summary>
    None, 
    /// <summary>
    /// コマンド入力の不正を表します。
    /// </summary>
    Warn,
    /// <summary>
    /// アクションの失敗を表します。
    /// </summary>
    Caution, 
    /// <summary>
    /// 汎用的なアノテーションです。
    /// </summary>
    General, 
    /// <summary>
    /// タイプ相性に関する情報を示します。
    /// </summary>
    PropInfo, 
    /// <summary>
    /// 急所に関する情報を示します。
    /// </summary>
    CritInfo, 
    /// <summary>
    /// 攻撃力や防御力のバフ・デバフに関する情報を示します。
    /// </summary>
    BufInfo, 
    /// <summary>
    /// 回復に関する情報を示します。
    /// </summary>
    HealInfo, 
    /// <summary>
    /// 特殊なとくせいの発動に関する情報を示します。
    /// </summary>
    InvokeInfo, 
    /// <summary>
    /// ログに表示するプレイヤーの情報を示します。
    /// </summary>
    LogInfo, 
    /// <summary>
    /// ログに表示するアクションの情報を示します。
    /// </summary>
    LogActionInfo, 
    /// <summary>
    /// ゲーム システムに関する情報を示します。
    /// </summary>
    SystemInfo, 
    /// <summary>
    /// ゲーム オプションに関する情報を示します。
    /// </summary>
    SettingInfo, 
    /// <summary>
    /// 状態異常に関する情報など、補助的な情報を示します。
    /// </summary>
    AuxInfo, 
    /// <summary>
    /// プレイヤーの死亡に関する情報を示します。
    /// </summary>
    DeathInfo
}
/// <summary>
/// アノテーション付き文字列を表すクラスです。
/// </summary>
class AnnotatedString
{
    /// <summary>
    /// アノテーションを受ける文字列
    /// </summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// アノテーションの種類
    /// </summary>
    public Notice Notice { get; set; } = None;
    /// <summary>
    /// アノテーションの種類がログにのみ作用するかどうかの判定
    /// </summary>
    public bool IsLog => Notice is LogInfo or LogActionInfo;
    public AnnotatedString(string text, Notice notice) => (Text, Notice) = (text, notice);
    public override string ToString()
    {
        return Text + " " + Notice;
    }
    public static explicit operator ColoredString(AnnotatedString ns)
    {
        return new ColoredString(ns.Text, ns.Notice switch
        {
            None => White,
            Warn => Red,
            Caution => Yellow,
            General => Yellow,
            PropInfo => Magenta,
            CritInfo => Magenta,
            BufInfo => Blue,
            HealInfo => Green,
            InvokeInfo => Cyan,
            LogInfo => DarkYellow,
            LogActionInfo => DarkCyan,
            SystemInfo => Cyan,
            SettingInfo => DarkGreen,
            AuxInfo => DarkGreen,
            DeathInfo => DarkGreen,
            _ => Gray
        });
    }
    public static implicit operator AnnotatedString((string, Notice) t) => new(t.Item1, t.Item2);
}