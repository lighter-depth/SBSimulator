using static SBSimulator.Source.Word;
using static SBSimulator.Source.SBOptions;

namespace SBSimulator.Source;
internal static class SBExtention
{
    #region methods
    public static char GetRandomChar()
    {
        var charList = new List<char>
        {
            'あ', 'い', 'う', 'え', 'お',
            'か', 'き', 'く', 'け', 'こ',
            'さ', 'し', 'す', 'せ', 'そ',
            'た', 'ち', 'つ', 'て', 'と',
            'な', 'に', 'ね', 'の',
            'は', 'ひ', 'ふ', 'へ', 'ほ',
            'ま', 'み', 'む' ,'め', 'も',
            'や', 'ゆ', 'よ',
            'ら', 'り', 'る', 'れ', 'ろ',
            'わ'
        };
        return charList[new Random().Next(charList.Count)];
    }
    /// <summary>
    /// <see cref="WordType"/>列挙型を文字列に変換します。
    /// </summary>
    /// <param name="type">変換する<see cref="WordType"/>列挙型のインスタンス</param>
    /// <returns>変換された文字列のインスタンス</returns>
    public static string TypeToString(this WordType type)
    {
        return type switch
        {
            WordType.Empty => "",
            WordType.Normal => "ノーマル",
            WordType.Animal => "動物",
            WordType.Plant => "植物",
            WordType.Place => "地名",
            WordType.Emote => "感情",
            WordType.Art => "芸術",
            WordType.Food => "食べ物",
            WordType.Violence => "暴力",
            WordType.Health => "医療",
            WordType.Body => "人体",
            WordType.Mech => "機械",
            WordType.Science => "理科",
            WordType.Time => "時間",
            WordType.Person => "人物",
            WordType.Work => "工作",
            WordType.Cloth => "服飾",
            WordType.Society => "社会",
            WordType.Play => "遊び",
            WordType.Bug => "虫",
            WordType.Math => "数学",
            WordType.Insult => "暴言",
            WordType.Religion => "宗教",
            WordType.Sports => "スポーツ",
            WordType.Weather => "天気",
            WordType.Tale => "物語",
            _ => "天で話にならねぇよ..."
        };
    }
    /// <summary>
    /// 文字を<see cref="WordType"/>列挙型に変換します。
    /// </summary>
    /// <param name="symbol"><see cref="WordType"/>に変換する文字</param>
    /// <returns>変換された<see cref="WordType"/>列挙型のインスタンス</returns>
    public static WordType CharToType(this char symbol)
    {
        return symbol switch
        {
            'N' or 'n' => WordType.Normal,
            'A' or 'a' => WordType.Animal,
            'Y' or 'y' => WordType.Plant,
            'G' or 'g' => WordType.Place,
            'E' or 'e' => WordType.Emote,
            'C' or 'c' => WordType.Art,
            'F' or 'f' => WordType.Food,
            'V' or 'v' => WordType.Violence,
            'H' or 'h' => WordType.Health,
            'B' or 'b' => WordType.Body,
            'M' or 'm' => WordType.Mech,
            'Q' or 'q' => WordType.Science,
            'T' or 't' => WordType.Time,
            'P' or 'p' => WordType.Person,
            'K' or 'k' => WordType.Work,
            'L' or 'l' => WordType.Cloth,
            'S' or 's' => WordType.Society,
            'J' or 'j' => WordType.Play,
            'D' or 'd' => WordType.Bug,
            'X' or 'x' => WordType.Math,
            'Z' or 'z' => WordType.Insult,
            'R' or 'r' => WordType.Religion,
            'U' or 'u' => WordType.Sports,
            'W' or 'w' => WordType.Weather,
            'O' or 'o' => WordType.Tale,
            _ => WordType.Empty // I is not used
        };
    }
    /// <summary>
    /// 文字列を<see cref="WordType"/>列挙型に変換します。
    /// </summary>
    /// <param name="symbol"><see cref="WordType"/>に変換する文字列</param>
    /// <returns>変換された<see cref="WordType"/>列挙型のインスタンス</returns>
    public static WordType StringToType(this string symbol)
    {
        return symbol switch
        {
            "ノーマル" => WordType.Normal,
            "動物" => WordType.Animal,
            "植物" => WordType.Plant,
            "地名" => WordType.Place,
            "感情" => WordType.Emote,
            "芸術" => WordType.Art,
            "食べ物" => WordType.Food,
            "暴力" => WordType.Violence,
            "医療" => WordType.Health,
            "人体" => WordType.Body,
            "機械" => WordType.Mech,
            "理科" => WordType.Science,
            "時間" => WordType.Time,
            "人物" => WordType.Person,
            "工作" => WordType.Work,
            "服飾" => WordType.Cloth,
            "社会" => WordType.Society,
            "遊び" => WordType.Play,
            "虫" => WordType.Bug,
            "数学" => WordType.Math,
            "暴言" => WordType.Insult,
            "宗教" => WordType.Religion,
            "スポーツ" => WordType.Sports,
            "天気" => WordType.Weather,
            "物語" => WordType.Tale,
            _ => WordType.Empty
        };
    }
    /// <summary>
    /// <see cref="Player.PlayerState"/>列挙型を対応する文字列に変換します。
    /// </summary>
    /// <param name="state">変換する<see cref="Player.PlayerState"/>列挙型のインスタンス</param>
    /// <returns>変換した文字列のインスタンス</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string StateToString(this Player.PlayerState state)
    {
        return state switch
        {
            Player.PlayerState.Normal => "なし",
            Player.PlayerState.Poison => "毒",
            Player.PlayerState.Seed => "やどりぎ",
            Player.PlayerState.Poison | Player.PlayerState.Seed => "どく、やどりぎ",
            _ => throw new ArgumentException($"PlayerState \"{state}\" has not been found.")
        };
    }
    /// <summary>
    /// 文字列を論理値に変換します。
    /// </summary>
    /// <param name="str">変換する文字列のインスタンス</param>
    /// <param name="enabler">変換された論理値のインスタンス</param>
    /// <returns>変換が成功したかどうかを表すフラグ</returns>
    public static bool TryStringToEnabler(this string str, out bool enabler)
    {
        if (str is Program.ENABLE or "E" or "e" or "T" or "t")
        {
            enabler = true;
            return true;
        }
        if (str is Program.DISABLE or "D" or "d" or "F" or "f")
        {
            enabler = false;
            return true;
        }
        enabler = false;
        return false;
    }
    /// <summary>
    /// 指令した文字がワイルドカード文字であるかどうかを判定します。
    /// </summary>
    /// <param name="c">判定を受ける文字のインスタンス</param>
    /// <returns>文字がワイルドカードであるかどうかを表すフラグ</returns>
    public static bool IsWild(this char c)
    {
        return c is '*' or '＊';
    }
    /// <summary>
    /// 指定した文字列がワイルドカードを含むかどうかを判定します。
    /// </summary>
    /// <param name="name">判定を受ける文字列のインスタンス</param>
    /// <returns>文字列がワイルドカードを含むかどうかを表すフラグ</returns>
    public static bool IsWild(this string name)
    {
        return name.Contains('*') || name.Contains('＊');
    }
    /// <summary>
    /// 二つの文字がしりとりのルール上一致するかを判定します。
    /// </summary>
    /// <param name="previous">判定元になる文字</param>
    /// <param name="current">判定先の文字</param>
    /// <returns>文字がしりとりのルール上一致するかどうかを表すフラグ</returns>
    public static bool WordlyEquals(this char previous, char current)
    {
        if (previous == current
         || previous == 'ゃ' && current == 'や'
         || previous == 'ゅ' && current == 'ゆ'
         || previous == 'ょ' && current == 'よ'
         || previous == 'ぁ' && current == 'あ'
         || previous == 'ぃ' && current == 'い'
         || previous == 'ぅ' && current == 'う'
         || previous == 'ぇ' && current == 'え'
         || previous == 'ぉ' && current == 'お'
         || previous == 'っ' && current == 'つ'
         || previous == 'ぢ' && current == 'じ'
         || previous == 'づ' && current == 'ず'
         || previous == 'を' && current == 'お')
            return true;
        return false;
    }
    /// <summary>
    /// 指定した要素で空の<see cref="List{T}"/>を埋めます。
    /// </summary>
    /// <typeparam name="T">リストを埋める要素の型</typeparam>
    /// <param name="list">空のリスト</param>
    /// <param name="content">リストを埋める要素</param>
    /// <returns></returns>
    public static List<T> Fill<T>(this List<T> list, T content)
    {
        for (var i = 0; i < list.Capacity; i++)
        {
            list.Add(content);
        }
        return list;
    }
    /// <summary>
    /// <see cref="List{T}"/>の末尾に<see cref="AnnotatedString"/>要素を追加します。
    /// </summary>
    /// <param name="list">要素を追加するリスト</param>
    /// <param name="text">追加する文字列要素</param>
    /// <param name="notice">追加する文字列要素のアノテーション</param>
    public static void Add(this List<AnnotatedString> list, string text, Notice notice)
    {
        list.Add(new(text, notice));
    }
    /// <summary>
    /// <see cref="List{T}"/>の末尾に複数の<see cref="AnnotatedString"/>要素を追加します。
    /// </summary>
    /// <param name="list">要素を追加するリスト</param>
    /// <param name="msgs">追加する<see cref="AnnotatedString"/>のコレクション</param>
    public static void AddMany(this List<AnnotatedString> list, IEnumerable<AnnotatedString> msgs)
    {
        foreach (var msg in msgs)
            list.Add(msg);
    }
    /// <summary>
    /// 色付き文字列を出力します。
    /// </summary>
    /// <param name="cString">出力する<see cref="ColoredString"/>のインスタンス</param>
    public static void WriteLine(this (string, ConsoleColor) cString)
    {
        var (text, color) = cString;
        new ColoredString(text, color).WriteLine();
    }
    /// <summary>
    /// 色付き文字列を、末尾の改行を行わずに出力します。
    /// </summary>
    /// <param name="cString">出力する<see cref="ColoredString"/>のインスタンス</param>
    public static void Write(this (string, ConsoleColor) cString)
    {
        var (text, color) = cString;
        new ColoredString(text, color).Write();
    }
    #endregion
}
