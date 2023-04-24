using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SBSimulator.src.Word;
using static SBSimulator.src.Player;
using static SBSimulator.src.SBOptions;
using System.Runtime.CompilerServices;

namespace SBSimulator.src
{
    internal static class SBExtention
    {
        #region private fields
        static readonly Dictionary<List<string>, PlayerAbility> AbilSymbols = new()
        {
            [new() { "N", "n", "deb", "デバッガー", "でばっがー", "出歯" }] = PlayerAbility.Debugger,
            [new() { "A", "a", "brd", "はんしょく", "繁殖", "Hanshoku", "HANSHOKU" }] = PlayerAbility.Hanshoku,
            [new() { "Y", "y", "sed", "やどりぎ", "宿り木", "ヤドリギ", "宿木", "宿", "Yadorigi", "YADORIGI" }] = PlayerAbility.Yadorigi,
            [new() { "G", "g", "gbl", "グローバル", "ぐろーばる", "Global", "GLOBAL" }] = PlayerAbility.Global,
            [new() { "E", "e", "psn", "じょうねつ", "情熱", "Jounetsu", "JOUNETSU" }] = PlayerAbility.Jounetsu,
            [new() { "C", "c", "rar", "ロックンロール", "ろっくんろーる", "RocknRoll", "ROCKNROLL", "ロクロ", "轆轤", "ろくろ" }] = PlayerAbility.RocknRoll,
            [new() { "F", "f", "glt", "いかすい", "胃下垂", "Ikasui", "IKASUI" }] = PlayerAbility.Ikasui,
            [new() { "V", "v", "msl", "むきむき", "Mukimuki", "MUKIMUKI", "最強の特性", "最強特性" }] = PlayerAbility.MukiMuki,
            [new() { "H", "h", "mdc", "いしょくどうげん", "医食同源", "Ishoku", "ISHOKU", "いしょく", "医食" }] = PlayerAbility.Ishoku,
            [new() { "B", "b", "kar", "からて", "空手", "Karate", "KARATE" }] = PlayerAbility.Karate,
            [new() { "M", "m", "clk", "かちこち", "Kachikochi", "KACHIKOCHI", "sus" }] = PlayerAbility.Kachikochi,
            [new() { "Q", "q", "exp", "じっけん", "実験", "Jikken", "JIKKEN" }] = PlayerAbility.Jikken,
            [new() { "T", "t", "prc", "さきのばし", "先延ばし", "Sakinobashi", "SAKINOBASHI", "めざまし" }] = PlayerAbility.Sakinobashi,
            [new() { "P", "p", "gnt", "きょじん", "巨人", "Kyojin", "KYOJIN", "準最強特性" }] = PlayerAbility.Kyojin,
            [new() { "K", "k", "arm", "ぶそう", "武装", "Busou", "BUSOU", "富士山" }] = PlayerAbility.Busou,
            [new() { "L", "l", "lyr", "かさねぎ", "重ね着", "Kasanegi", "KASANEGI" }] = PlayerAbility.Kasanegi,
            [new() { "S", "s", "ins", "ほけん", "保険", "Hoken", "HOKEN", "じゃくてんほけん", "弱点保険", "じゃくほ", "弱保" }] = PlayerAbility.Hoken,
            [new() { "J", "j", "rev", "かくめい", "革命", "Kakumei", "KAKUMEI" }] = PlayerAbility.Kakumei,
            [new() { "D", "d", "ndl", "どくばり", "毒針", "Dokubari", "DOKUBARI" }] = PlayerAbility.Dokubari,
            [new() { "X", "x", "clc", "けいさん", "計算", "Keisan", "KEISAN" }] = PlayerAbility.Keisan,
            [new() { "Z", "z", "htm", "ずぼし", "図星", "Zuboshi", "ZUBOSHI" }] = PlayerAbility.Zuboshi,
            [new() { "R", "r", "fth", "しんこうしん", "信仰心", "Shinkoushin", "SHINKOUSHIN", "ドグマ" }] = PlayerAbility.Shinkoushin,
            [new() { "U", "u", "trn", "トレーニング", "とれーにんぐ", "Training", "TRAINING", "誰も使わない特性" }] = PlayerAbility.Training,
            [new() { "W", "w", "tph", "たいふういっか", "台風一過", "台風一家", "WZ", "wz", "WeathersZero", "天で話にならねぇよ..." }] = PlayerAbility.WZ,
            [new() { "O", "o", "orm", "おれのことばのもじすうがおおいほどいりょくがおおきくなるけんについて", "俺の言葉の文字数が多いほど威力が大きくなる件について", "おれもじ", "俺文字", "Oremoji", "OREMOJI" }] = PlayerAbility.Oremoji
        };
        static readonly Dictionary<List<string>, WordType> WordSymbols = new()
        {
            [new() { "N", "n", "のーまる", "ノーマル" }] = WordType.Normal
        };
        #endregion

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
        public static string AbilToString(this PlayerAbility abil)
        {
            return abil switch
            {
                Player.PlayerAbility.Empty => "ノーマル",
                Player.PlayerAbility.Debugger => "デバッガー",
                Player.PlayerAbility.Hanshoku => "はんしょく",
                Player.PlayerAbility.Yadorigi => "やどりぎ",
                Player.PlayerAbility.Global => "グローバル",
                Player.PlayerAbility.Jounetsu => "じょうねつ",
                Player.PlayerAbility.RocknRoll => "ロックンロール",
                Player.PlayerAbility.Ikasui => "いかすい",
                Player.PlayerAbility.MukiMuki => "むきむき",
                Player.PlayerAbility.Ishoku => "いしょくどうげん",
                Player.PlayerAbility.Karate => "からて",
                Player.PlayerAbility.Kachikochi => "かちこち",
                Player.PlayerAbility.Jikken => "じっけん",
                Player.PlayerAbility.Sakinobashi => "さきのばし",
                Player.PlayerAbility.Kyojin => "きょじん",
                Player.PlayerAbility.Busou => "ぶそう",
                Player.PlayerAbility.Kasanegi => "かさねぎ",
                Player.PlayerAbility.Hoken => "ほけん",
                Player.PlayerAbility.Kakumei => "かくめい",
                Player.PlayerAbility.Dokubari => "どくばり",
                Player.PlayerAbility.Keisan => "けいさん",
                Player.PlayerAbility.Zuboshi => "ずぼし",
                Player.PlayerAbility.Shinkoushin => "しんこうしん",
                Player.PlayerAbility.Training => "トレーニング",
                Player.PlayerAbility.WZ => "たいふういっか",
                Player.PlayerAbility.Oremoji => "俺文字",
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
        public static PlayerAbility StringToAbil(this string symbol)
        {
            foreach (List<string> symbols in AbilSymbols.Keys)
            {
                if (symbols.Contains(symbol))
                {
                    return AbilSymbols[symbols];
                }
            }
            return PlayerAbility.Empty;
        }
        public static List<T> Fill<T>(this List<T> list, T content)
        {
            for (var i = 0; i < list.Capacity; i++)
            {
                list.Add(content);
            }
            return list;
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
}
