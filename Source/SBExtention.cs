using static SBSimulator.Source.Word;
using static SBSimulator.Source.SBOptions;

namespace SBSimulator.Source;

internal static class SBExtention
{
    #region methods
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
    public static bool IsWild(this char c)
    {
        return c is '*' or '＊';
    }
    public static bool IsWild(this string name)
    {
        return name.Contains('*') || name.Contains('＊');
    }
    public static SBMode StringToMode(this string symbol)
    {
        return symbol switch
        {
            "D" or "d" or "DEFAULT" or Program.DEFAULT_MODE => SBMode.Default,
            "C" or "c" or "CLASSIC" or Program.CLASSIC_MODE => SBMode.Classic,
            "S" or "s" or "AGEOFSEED" or Program.AOS_MODE => SBMode.AgeOfSeed,
            _ => SBMode.Empty
        };
    }
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
    public static string ModeToString(this SBMode mode)
    {
        return mode switch
        {
            SBMode.Default => Program.DEFAULT_MODE,
            SBMode.Classic => Program.CLASSIC_MODE,
            SBMode.AgeOfSeed => Program.AOS_MODE,
            _ => "EMPTY"
        };
    }
    public static List<T> Fill<T>(this List<T> list, T content)
    {
        for (var i = 0; i < list.Capacity; i++)
        {
            list.Add(content);
        }
        return list;
    }
    public static void Add(this List<ColoredString> list, string text, ConsoleColor color)
    {
        list.Add(new(text, color));
    }
    public static void WriteLine(this (string, ConsoleColor) cString)
    {
        var (text, color) = cString;
        new ColoredString(text, color).WriteLine();
    }
    public static void Write(this (string, ConsoleColor) cString)
    {
        var (text, color) = cString;
        new ColoredString(text, color).Write();
    }
    #endregion
}
