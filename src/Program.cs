using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static SBSimulator.src.Word;
using static SBSimulator.src.Player.PlayerAbility;
using static SBSimulator.src.SBOptions;
using System.Text.RegularExpressions;
using Umayadia.Kana;
using SBSimulator.src;

namespace SBSimulator.src
{
    class Program
    {
        #region static fields
        static ConsoleEventLoop eventLoop = new();
        static readonly Window w = new();
        static Player p1 = new();
        static Player p2 = new();
        static bool IsP1sTurn { get; set; } = true;
        static bool IsSuspended { get; set; } = false;
        static Player CurrentP => IsP1sTurn ? p1 : p2;
        static Player OtherP => IsP1sTurn ? p2 : p1;
        static List<string> UsedWords = new();
        static List<string> NoTypeWords = new();
        static readonly string DicDir = GetDicPath();
        static readonly string NoTypeWordsPath = DicDir + @"\no type\no-type-words.csv";
        static readonly string NoTypeWordExPath = DicDir + @"\no type\no-type-word-extension.csv";
        static readonly string TypedWordsPath = DicDir + @"\typed";
        static List<string> NoTypeWordEx = new();
        static Dictionary<string, List<WordType>> TypedWords = new();
        static Task DictionaryImportTask;
        static Dictionary<string, Action<object, CancellationTokenSource>> OrderFunctions => new()
        {
            [CHANGE] = OnChangeOrdered,
            [OPTION] = OnOptionOrdered,
            [SHOW] = OnShowOrdered,
            [RESET] = OnResetOrdered,
            [EXIT] = OnExitOrdered,
            [HELP] = OnHelpOrdered,
            [ADD] = OnAddOrdered,
            [REMOVE] = OnRemoveOrdered,
        };
        #endregion

        #region constants
        const string EXIT = "exit";
        const string RESET = "reset";
        const string ACTION = "action";
        const string CHANGE = "change";
        const string OPTION = "option";
        internal const string ENABLE = "enable";
        internal const string DISABLE = "disable";
        const string SHOW = "show";
        const string HELP = "help";
        const string ADD = "__add";
        const string REMOVE = "__remove";
        const string WARNING = "入力が不正です。";
        const string SET_MAX_HP = "SetMaxHP";
        const string INFINITE_SEED = "InfiniteSeed";
        const string INFINITE_CURE = "InfiniteCure";
        const string ABIL_CHANGE = "AbilChange";
        const string SET_ABIL_COUNT = "SetAbilCount";
        const string SET_MAX_CURE_COUNT = "SetMaxCureCount";
        const string SET_MAX_FOOD_COUNT = "SetMaxFoodCount";
        const string SET_SEED_DMG = "SetSeedDmg";
        const string SET_MAX_SEED_TURN = "SetMaxSeedTurn";
        const string SET_CRIT_DMG_MULTIPLIER = "SetCritDmgMultiplier";
        const string SET_INS_BUF_QTY = "SetInsBufQty";
        const string SET_MODE = "SetMode";
        const string Player1 = "Player1";
        const string Player2 = "Player2";
        const string STATUS = "status";
        const string OPTIONS = "options";
        const string LOG = "log";
        internal const string DEFAULT_MODE = "Default";
        internal const string CLASSIC_MODE = "Classic";
        internal const string AOS_MODE = "AgeOfSeed";
        const string STRICT = "strict";
        const string INFER = "inference";
        #endregion
        static Program()
        {
            DictionaryImportTask = Task.Run(ImportDictionary);
        }

        #region methods
        static void Main()
        {
            try
            {
                IsSuspended = false;
                SetMode(SBMode.Default);
                SetUp();
                Console.WriteLine("辞書を読み込み中...\n\nしばらくお待ちください...");
                while (!DictionaryImportTask.IsCompleted) { }
                Refresh();
                w.WriteLine();
                var cts = new CancellationTokenSource();
                eventLoop = new ConsoleEventLoop(order => OnOrdered(order, cts));
                Task.Run(() => eventLoop.Start(cts.Token)).Wait();
                ExitApp();
            }
            catch (Exception exc)
            {
                Console.Clear();
                new ColoredString("\n\n\n---予期せぬエラーが発生しました---\n\n\n", ConsoleColor.Red).WriteLine();
                new ColoredString(exc.Message + "\n" + exc.StackTrace, ConsoleColor.Yellow).WriteLine();
                new ColoredString("\n\n---この画面のスクリーンショットを開発者に送信してください---\n\n開発者のtwitterアカウント: ", ConsoleColor.Cyan).Write();
                new ColoredString("https://twitter.com/lighter_depth", ConsoleColor.DarkGreen).WriteLine();
                Console.WriteLine("\n\n\n\n任意のキーを押してアプリケーションを終了します. . . ");
                Console.ReadLine();
            }

        }
        static void SetMode(SBMode mode)
        {
            if (mode is SBMode.Default)
            {
                p1.MaxHP = 60;
                p1.ModifyMaxHP();
                p2.MaxHP = 60;
                p2.ModifyMaxHP();
                IsSeedInfinite = false;
                IsCureInfinite = false;
                IsAbilChangeable = true;
                Player.MaxAbilChange = 3;
                Player.MaxCureCount = 5;
                Player.MaxFoodCount = 6;
                Player.SeedDmg = 5;
                Player.MaxSeedTurn = 4;
                Player.CritDmg = 1.5;
                Player.InsBufQty = 3;
                return;
            }
            if (mode is SBMode.Classic)
            {
                p1.MaxHP = 50;
                p1.ModifyMaxHP();
                p2.MaxHP = 50;
                p2.ModifyMaxHP();
                IsSeedInfinite = true;
                IsCureInfinite = true;
                IsAbilChangeable = false;
                Player.MaxAbilChange = 3;
                Player.MaxCureCount = 5;
                Player.MaxFoodCount = 6;
                Player.SeedDmg = 5;
                Player.MaxSeedTurn = 4;
                Player.CritDmg = 1.5;
                Player.InsBufQty = 3;
                return;
            }
            if (mode is SBMode.AgeOfSeed)
            {
                p1.MaxHP = 60;
                p1.ModifyMaxHP();
                p2.MaxHP = 60;
                p2.ModifyMaxHP();
                IsSeedInfinite = true;
                IsCureInfinite = false;
                IsAbilChangeable = true;
                Player.MaxAbilChange = 3;
                Player.MaxCureCount = 5;
                Player.MaxFoodCount = 6;
                Player.SeedDmg = 5;
                Player.MaxSeedTurn = 4;
                Player.CritDmg = 1.5;
                Player.InsBufQty = 3;
            }
        }

        static string GetDicPath()
        {
            // TODO: 「見つかるまで探索」処理の実装

            var path = AppDomain.CurrentDomain.BaseDirectory;
            int bsCount = 0;
            int baseIndex = 0;

            // 最大５階層まで遡ってディレクトリ探索
            for (var i = path.Length - 1; i > 0; i--)
            {
                if (path[i] is '\\' or '/') bsCount++;
                if (bsCount == 5)
                {
                    baseIndex = i;
                    break;
                }
            }

            // ディレクトリの絶対パス取得
            if (baseIndex != 0)
            {
                var newPath = path[..baseIndex];
                var filesRaw = Directory.GetFiles(newPath, "*", SearchOption.AllDirectories);
                var r = new Regex(@"^.*\\no type$");
                foreach (var i in filesRaw)
                {
                    var dir = Path.GetDirectoryName(i) ?? string.Empty;
                    if (r.IsMatch(dir))
                        return Path.GetDirectoryName(dir) ?? throw new DirectoryNotFoundException("dic ディレクトリが見つかりませんでした。");
                }
            }
            throw new DirectoryNotFoundException("dic ディレクトリが見つかりませんでした。");
        }
        static async Task ImportDictionary()
        {
            using var noTypeWordsReader = new StreamReader(NoTypeWordsPath);
            using var exNoTypeWordReader = new StreamReader(NoTypeWordExPath);
            var files = Directory.GetFiles(TypedWordsPath, "*", SearchOption.AllDirectories);
            NoTypeWords = new();
            NoTypeWordEx = new();
            TypedWords = new();
            while (!noTypeWordsReader.EndOfStream)
            {
                var line = await noTypeWordsReader.ReadLineAsync() ?? string.Empty;
                NoTypeWords.Add(line);
            }
            while (!exNoTypeWordReader.EndOfStream)
            {
                var line = await exNoTypeWordReader.ReadLineAsync() ?? string.Empty;
                NoTypeWordEx.Add(line);
            }
            foreach (var file in files)
            {
                using var typedWordsReader = new StreamReader(file);
                while (!typedWordsReader.EndOfStream)
                {
                    var line = await typedWordsReader.ReadLineAsync() ?? string.Empty;
                    var statedLine = line.Trim().Split();
                    if (statedLine.Length == 2) TypedWords.Add(statedLine[0], new() { statedLine[1].StringToType() });
                    else if (statedLine.Length == 3) TypedWords.Add(statedLine[0], new() { statedLine[1].StringToType(), statedLine[2].StringToType() });
                }
            }
        }
        static void SetUp()
        {
            new ColoredString("しりとりバトルシミュレーターへようこそ。", ConsoleColor.Yellow).WriteLine();
            while (true)
            {
                var NGNamesList = new[] { "p1", "p2", Player1, Player2 };
                string? p1Name, p2Name;
                const string DEFAULT_P1_NAME = "じぶん";
                const string DEFAULT_P2_NAME = "あいて";
                new ColoredString($"プレイヤーの名前を入力してください。(デフォルトでは「{DEFAULT_P1_NAME}」です)", ConsoleColor.Yellow).WriteLine();
                p1Name = Console.ReadLine();
                if (string.IsNullOrEmpty(p1Name)) p1Name = DEFAULT_P1_NAME;
                p1Name = p1Name.Trim();
                if (NGNamesList.Contains(p1Name))
                {
                    new ColoredString($"名前{p1Name} は使用できません。", ConsoleColor.Red).WriteLine();
                    p1Name = DEFAULT_P1_NAME;
                }
                else
                    foreach (char c in p1Name)
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            new ColoredString($"名前{p1Name} は空白を含むため使用できません。", ConsoleColor.Red).WriteLine();
                            p1Name = DEFAULT_P1_NAME;
                            break;
                        }
                    }
                new ColoredString($"プレイヤーの名前を {p1Name} に設定しました。", ConsoleColor.Green).WriteLine();
                new ColoredString($"相手の名前を入力してください。(デフォルトでは「{DEFAULT_P2_NAME}」です)", ConsoleColor.Yellow).WriteLine();
                p2Name = Console.ReadLine();
                if (string.IsNullOrEmpty(p2Name)) p2Name = DEFAULT_P2_NAME;
                p2Name = p2Name.Trim();
                if (NGNamesList.Contains(p2Name) || p2Name.Contains(' '))
                {
                    new ColoredString($"名前{p2Name} は使用できません。", ConsoleColor.Red).WriteLine();
                    p2Name = DEFAULT_P2_NAME;
                }
                else
                    foreach (char c in p2Name)
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            new ColoredString($"名前{p2Name} は空白を含むため使用できません。", ConsoleColor.Red).WriteLine();
                            p2Name = DEFAULT_P2_NAME;
                            break;
                        }
                    }
                new ColoredString($"相手の名前を {p2Name} に設定しました。", ConsoleColor.Green).WriteLine();
                Player.PlayerAbility p1Abil, p2Abil;
                new ColoredString("プレイヤーの初期特性を入力してください。(デフォルトではデバッガーになります)", ConsoleColor.Yellow).WriteLine();
                p1Abil = (Console.ReadLine() ?? "N").StringToAbil();
                if (p1Abil == Empty) p1Abil = Player.PlayerAbility.Debugger;
                new ColoredString($"{p1Name} の初期特性を {p1Abil.AbilToString()} に設定しました。", ConsoleColor.Green).WriteLine();
                new ColoredString("相手の初期特性を入力してください。(デフォルトではデバッガーになります)", ConsoleColor.Yellow).WriteLine();
                p2Abil = (Console.ReadLine() ?? "N").StringToAbil();
                if (p2Abil == Empty) p2Abil = Player.PlayerAbility.Debugger;
                new ColoredString($"{p2Name} の初期特性を {p2Abil.AbilToString()} に設定しました。", ConsoleColor.Green).WriteLine();
                new ColoredString("この設定でよろしいですか？", ConsoleColor.Yellow).WriteLine();
                Console.WriteLine();
                new ColoredString($"プレイヤーの名前: {p1Name}, プレイヤーの初期特性: {p1Abil.AbilToString()}", ConsoleColor.Cyan).WriteLine();
                new ColoredString($"相手の名前: {p2Name}, 相手の初期特性: {p2Abil.AbilToString()}", ConsoleColor.Cyan).WriteLine();
                Console.WriteLine();
                new ColoredString("OK！ → 任意のキーを入力", ConsoleColor.Yellow).WriteLine();
                new ColoredString("ダメ！ → r キーを入力", ConsoleColor.Yellow).WriteLine();
                if (Console.ReadLine() == "r")
                {
                    Console.Clear();
                    continue;
                }
                p1 = new Player(p1Name, p1Abil);
                p2 = new Player(p2Name, p2Abil);
                Console.Clear();
                break;
            }
        }
        static void Reset(CancellationTokenSource cts)
        {
            IsSuspended = true;
            w.Message.Append(new ColoredString("やり直す？", ConsoleColor.Yellow));
            w.Message.Append(new ColoredString("\nやり直す！ → 任意のキーを入力", ConsoleColor.Yellow));
            w.Message.Append(new ColoredString("ログを表示 → l キーを入力", ConsoleColor.Yellow));
            w.Message.Append(new ColoredString("もうやめる！ → r キーを入力", ConsoleColor.Yellow));
            Refresh();
            w.WriteLine();
            var order = Console.ReadLine() ?? string.Empty;
            if (order == "r")
            {
                cts.Cancel();
                Console.Clear();
                return;
            }
            else if (order == "l")
            {
                cts.Cancel();
                ShowLog();
                Console.Clear();
                w.Message.Clear();
                w.Message.Log.Clear();
                UsedWords = new();
                IsP1sTurn = true;
                Main();
            }
            else
            {
                cts.Cancel();
                Console.Clear();
                w.Message.Clear();
                w.Message.Log.Clear();
                UsedWords = new();
                IsP1sTurn = true;
                Main();
            }
        }
        static void OnOrdered(string order, CancellationTokenSource cts)
        {
            var orderline = order.Trim().Split();
            if (OrderFunctions.TryGetValue(orderline[0], out var func))
            {
                func(orderline, cts);
                return;
            }
            OnDefault(orderline, cts);
        }
        #region action orders
        static void OnAttackOrdered(object sender, CancellationTokenSource cts)
        {
            var word = (Word)sender;
            var critFlag = CurrentP.AttackAndTryCrit(OtherP, word);
            w.Message.Log.Append(new ColoredString($"{CurrentP.Name} は単語 {word} で攻撃した！", ConsoleColor.DarkCyan));
            w.Message.Append(new ColoredString(word.CalcAmp(OtherP.CurrentWord) switch
            {
                0 => "こうかがないようだ...",
                >= 2 => "こうかはばつぐんだ！",
                > 0 and < 1 => "こうかはいまひとつのようだ...",
                1 => "ふつうのダメージだ",
                _ => string.Empty
            }, ConsoleColor.Magenta));

            // 急所の処理
            if (critFlag)
                w.Message.Append(new ColoredString("急所に当たった！", ConsoleColor.Magenta));

            // 暴力タイプの処理
            if (word.ContainsType(WordType.Violence))
            {
                if (CurrentP.Ability == MukiMuki && CurrentP.TryChangeATK(-1, word)) w.Message.Append(new ColoredString($"{CurrentP.Name} の攻撃が下がった！(現在{CurrentP.ATK,0:0.0#}倍)", ConsoleColor.Blue));
                else if (CurrentP.TryChangeATK(-2, word)) w.Message.Append(new ColoredString($"{CurrentP.Name} の攻撃ががくっと下がった！(現在{CurrentP.ATK,0:0.0#}倍)", ConsoleColor.Blue));
                else w.Message.Append(new ColoredString($"{CurrentP.Name} の攻撃はもう下がらない！", ConsoleColor.Blue));
            }

            // かくめいの処理
            if (word.IsRev)
            {
                CurrentP.Rev(OtherP);
                w.Message.Append(new ColoredString("すべての能力変化がひっくりかえった！", ConsoleColor.Cyan));
            }
            // たいふういっかの処理
            if (word.IsWZ)
            {
                CurrentP.WZ(OtherP);
                w.Message.Append(new ColoredString("すべての能力変化が元に戻った！", ConsoleColor.Cyan));
            }

            // どくばりの処理
            if (word.IsPoison && OtherP.IsPoisoned == false)
            {
                OtherP.Poison();
                w.Message.Append(new ColoredString($"{OtherP.Name} は毒を受けた！", ConsoleColor.DarkGreen));
            }

            // ほけんの処理
            if (OtherP.Ability == Hoken && word.CalcAmp(OtherP.CurrentWord) >= 2)
            {
                OtherP.TryChangeATK(Player.InsBufQty, OtherP.CurrentWord);
                w.Message.Append(new ColoredString($"{OtherP.Name} は弱点を突かれて攻撃がぐぐーんと上がった！ (現在{OtherP.ATK,0:0.0#}倍)", ConsoleColor.Blue));
            }

            // 死んだかどうかの判定、ターンの交替
            if (!TryToggleTurn()) Reset(cts);
            Refresh();
            w.WriteLine();
        }
        static void OnBufOrdered(object sender, CancellationTokenSource cts)
        {
            var word = (Word)sender;
            w.Message.Log.Append(new ColoredString($"{CurrentP.Name} は単語 {word} で能力を高めた！", ConsoleColor.DarkCyan));
            if (word.IsATKBuf)
            {
                var rarFlag = word.ContainsType(WordType.Art) && CurrentP.Ability == RocknRoll;
                if (rarFlag && CurrentP.TryChangeATK(2, word))
                    w.Message.Append(new ColoredString($"{CurrentP.Name} の攻撃がぐーんと上がった！(現在{CurrentP.ATK,0:0.0#}倍)", ConsoleColor.Blue));
                else if (!rarFlag && CurrentP.TryChangeATK(1, word))
                    w.Message.Append(new ColoredString($"{CurrentP.Name} の攻撃が上がった！(現在{CurrentP.ATK,0:0.0#}倍)", ConsoleColor.Blue));
                else
                    w.Message.Append(new ColoredString($"{CurrentP.Name} の攻撃はもう上がらない！", ConsoleColor.Yellow));
            }
            else if (word.IsDEFBuf)
            {
                if (CurrentP.TryChangeDEF(1, word))
                    w.Message.Append(new ColoredString($"{CurrentP.Name} の防御が上がった！(現在{CurrentP.DEF,0:0.0#}倍)", ConsoleColor.Blue));
                else
                    w.Message.Append(new ColoredString($"{CurrentP.Name} の防御はもう上がらない！", ConsoleColor.Yellow));
            }
            if (!TryToggleTurn()) Reset(cts);
            Refresh();
            w.WriteLine();
        }
        static void OnHealOrdered(object sender, CancellationTokenSource cts)
        {
            var word = (Word)sender;
            w.Message.Log.Append(new ColoredString($"{CurrentP.Name} は単語 {word} を使った！", ConsoleColor.DarkCyan));
            if (CurrentP.TryHeal(word))
            {
                if (word.ContainsType(WordType.Health) && !word.ContainsType(WordType.Food) && CurrentP.IsPoisoned || word.ContainsType(WordType.Food) && CurrentP.Ability == Ishoku && CurrentP.IsPoisoned)
                {
                    w.Message.Append(new ColoredString($"{CurrentP.Name} の毒がなおった！", ConsoleColor.Green));
                    CurrentP.DePoison();
                }
                w.Message.Append(new ColoredString($"{CurrentP.Name} の体力が回復した", ConsoleColor.Green));
            }
            else if (word.ContainsType(WordType.Food))
                w.Message.Append(new ColoredString($"{CurrentP.Name} はもう食べられない！", ConsoleColor.Yellow));
            else if (word.ContainsType(WordType.Health))
                w.Message.Append(new ColoredString($"{CurrentP.Name} はもう回復できない！", ConsoleColor.Yellow));
            if (!TryToggleTurn()) Reset(cts);
            Refresh();
            w.WriteLine();
        }
        static void OnSeedOrdered(object sender, CancellationTokenSource cts)
        {
            var word = (Word)sender;
            if (OtherP.IsSeeded)
            {
                WarnAndRefresh($"{OtherP.Name} はもうやどりぎ状態になってるよ (attack コマンドを試してね)");
                return;
            }
            w.Message.Log.Append(new ColoredString($"{CurrentP.Name} は単語 {word} でやどりぎを植えた！", ConsoleColor.DarkCyan));
            OtherP.Seed(CurrentP, word);
            w.Message.Append(new ColoredString($"{CurrentP.Name} は {OtherP.Name} に種を植え付けた！", ConsoleColor.DarkGreen));
            if (!TryToggleTurn()) Reset(cts);
            Refresh();
            w.WriteLine();
        }
        #endregion
        static void OnChangeOrdered(object sender, CancellationTokenSource cts)
        {
            var orderline = (string[])sender;
            if (!IsAbilChangeable)
            {
                WarnAndRefresh("設定「変更可能な特性」がオフになっています。option コマンドから設定を切り替えてください。");
                return;
            }
            if (orderline.Length == 2)      // change 俺文字
            {
                var nextAbil = orderline[1].StringToAbil();
                if (nextAbil == Empty)
                {
                    WarnAndRefresh($"入力 {orderline[1]} に対応するとくせいが見つかりませんでした。");
                    return;
                }
                if (nextAbil == CurrentP.Ability)
                {
                    WarnAndRefresh($"既にそのとくせいになっている！");
                    return;
                }
                if (CurrentP.TryChangeAbil(nextAbil))
                    w.Message.Append(new ColoredString($"{CurrentP.Name} はとくせいを {nextAbil.AbilToString()} に変更しました", ConsoleColor.Cyan));
                else
                    w.Message.Append(new ColoredString($"{CurrentP.Name} はもう特性を変えられない！", ConsoleColor.Yellow));
            }
            else if (orderline.Length == 3)    // change じぶん 俺文字
            {
                Player abilChangingP = new();
                bool player1Flag = orderline[1] == p1.Name || orderline[1] == Player1 || orderline[1] == "p1";
                bool player2Flag = orderline[1] == p2.Name || orderline[1] == Player2 || orderline[1] == "p2";
                if (!player1Flag && !player2Flag)
                {
                    WarnAndRefresh($"名前 {orderline[1]} を持つプレイヤーが見つかりませんでした。");
                    return;
                }
                if (player1Flag)
                    abilChangingP = p1;
                else if (player2Flag)
                    abilChangingP = p2;
                var nextAbil = orderline[2].StringToAbil();
                if (nextAbil == Empty)
                {
                    WarnAndRefresh($"入力 {orderline[2]} に対応するとくせいが見つかりませんでした。");
                    return;
                }
                if (nextAbil == abilChangingP.Ability)
                {
                    WarnAndRefresh($"既にそのとくせいになっている！");
                    return;
                }
                if (abilChangingP.TryChangeAbil(nextAbil))
                    w.Message.Append(new ColoredString($"{abilChangingP.Name} はとくせいを {nextAbil.AbilToString()} に変更しました", ConsoleColor.Cyan));
                else
                    w.Message.Append(new ColoredString($"{abilChangingP.Name} はもう特性を変えられない！", ConsoleColor.Yellow));
            }
            else
                Warn();
            Refresh();
            w.WriteLine();
        }
        static void OnOptionOrdered(object sender, CancellationTokenSource cts)
        {
            var order = (string[])sender;
            if (order.Length < 2)
            {
                WarnAndRefresh();
                return;
            }
            Action<string[]> option = order[1] switch
            {
                SET_MAX_HP or "SMH" or "smh" => OptionSetMaxHP,
                INFINITE_SEED or "IS" or "is" => OptionInfiniteSeed,
                INFINITE_CURE or "IC" or "ic" => OptionInfiniteCure,
                ABIL_CHANGE or "AC" or "ac" => OptionAbilChange,
                STRICT or "S" or "s" => OptionStrict,
                INFER or "I" or "i" => OptionInfer,
                SET_ABIL_COUNT or "SAC" or "sac" => OptionSetAbilCount,
                SET_MAX_CURE_COUNT or "SMCC" or "smcc" or "SMC" or "smc" => OptionSetMaxCureCount,
                SET_MAX_FOOD_COUNT or "SMFC" or "smfc" or "SMF" or "smf" => OptionSetMaxFoodCount,
                SET_SEED_DMG or "SSD" or "ssd" => OptionSetSeedDmg,
                SET_MAX_SEED_TURN or "SMST" or "smst" or "SMS" or "sms" => OptionSetMaxSeedTurn,
                SET_CRIT_DMG_MULTIPLIER or "SCDM" or "scdm" or "SCD" or "scd" => OptionSetCritDmgMultiplier,
                SET_INS_BUF_QTY or "SIBQ" or "sibq" or "SIB" or "sib" => OptionSetInsBufQty,
                SET_MODE or "SM" or "sm" => OptionSetMode,
                _ => OptionDefault
            };
            option(order);
            Refresh();
            w.WriteLine();
        }
        #region options
        static void OptionSetMaxHP(string[] order)
        {
            if (order.Length != 4)
            {
                Warn();
                return;
            }
            if (!int.TryParse(order[3], out int hp))
            {
                Warn($"HPの書式が不正です。");
                return;
            }
            if (!TryStringToPlayer(order[2], out Player p))
            {
                Warn($"プレイヤー {order[2]} が見つかりませんでした。");
                return;
            }
            p.MaxHP = hp;
            p.ModifyMaxHP();
            w.Message.Append(new ColoredString($"{p.Name} の最大HPを {p.MaxHP} に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionInfiniteSeed(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!order[2].TryStringToEnabler(out bool enabler))
            {
                Warn();
                return;
            }
            if (enabler)
            {
                IsSeedInfinite = true;
                w.Message.Append(new ColoredString("やどりぎの継続ターン数を 無限 に変更しました。", ConsoleColor.DarkGreen));
                return;
            }
            IsSeedInfinite = false;
            w.Message.Append(new ColoredString($"やどりぎの継続ターン数を {Player.MaxSeedTurn} ターン に変更しました。", ConsoleColor.DarkGreen));
        }
        static void OptionInfiniteCure(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!order[2].TryStringToEnabler(out bool enabler))
            {
                Warn();
                return;
            }
            if (enabler)
            {
                IsCureInfinite = true;
                w.Message.Append(new ColoredString("医療タイプの単語で回復可能な回数を 無限 に変更しました。", ConsoleColor.DarkGreen));
                return;
            }
            IsCureInfinite = false;
            w.Message.Append(new ColoredString($"医療タイプの単語で回復可能な回数を {Player.MaxCureCount}回 に変更しました。", ConsoleColor.DarkGreen));
        }
        static void OptionAbilChange(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!order[2].TryStringToEnabler(out bool enabler))
            {
                Warn();
                return;
            }
            if (enabler)
            {
                IsAbilChangeable = true;
                w.Message.Append(new ColoredString($"とくせいの変更を有効にしました。(上限 {Player.MaxAbilChange}回 まで)", ConsoleColor.DarkGreen));
                return;
            }
            IsAbilChangeable = false;
            w.Message.Append(new ColoredString($"とくせいの変更を無効にしました。", ConsoleColor.DarkGreen));

        }
        static void OptionStrict(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!order[2].TryStringToEnabler(out bool enabler))
            {
                Warn();
                return;
            }
            if (enabler)
            {
                IsStrict = true;
                w.Message.Append(new ColoredString($"ストリクト モードを有効にしました。", ConsoleColor.DarkGreen));
                return;
            }
            IsStrict = false;
            w.Message.Append(new ColoredString($"ストリクト モードを無効にしました。", ConsoleColor.DarkGreen));
        }
        static void OptionInfer(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!order[2].TryStringToEnabler(out bool enabler))
            {
                Warn();
                return;
            }
            if (enabler)
            {
                IsInferable = true;
                w.Message.Append(new ColoredString($"タイプの推論を有効にしました。", ConsoleColor.DarkGreen));
                return;
            }
            IsInferable = false;
            w.Message.Append(new ColoredString($"タイプの推論を無効にしました。", ConsoleColor.DarkGreen));
        }
        static void OptionSetAbilCount(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!int.TryParse(order[2], out int abilCount) || abilCount < 0)
            {
                Warn("とくせいの変更回数の入力が不正です。");
                return;
            }
            Player.MaxAbilChange = abilCount;
            w.Message.Append(new ColoredString($"とくせいの変更回数上限を {Player.MaxAbilChange}回 に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionSetMaxCureCount(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!int.TryParse(order[2], out int cureCount) || cureCount <= 0)
            {
                Warn("回復回数の入力が不正です。");
                return;
            }
            Player.MaxCureCount = cureCount;
            w.Message.Append(new ColoredString($"医療タイプの単語による回復の回数上限を {Player.MaxCureCount}回 に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionSetMaxFoodCount(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!int.TryParse(order[2], out int foodCount) || foodCount <= 0)
            {
                Warn("回復回数の入力が不正です。");
                return;
            }

            Player.MaxFoodCount = foodCount;
            w.Message.Append(new ColoredString($"食べ物タイプの単語による回復の回数上限を {Player.MaxFoodCount}回 に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionSetSeedDmg(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!int.TryParse(order[2], out int seedDmg) || seedDmg <= 0)
            {
                Warn("ダメージ値の入力が不正です。");
                return;
            }
            Player.SeedDmg = seedDmg;
            w.Message.Append(new ColoredString($"やどりぎによるダメージを {Player.SeedDmg} に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionSetMaxSeedTurn(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!int.TryParse(order[2], out int seedTurn) || seedTurn <= 0)
            {
                Warn("ターン数の入力が不正です。");
                return;
            }
            Player.MaxSeedTurn = seedTurn;
            w.Message.Append(new ColoredString($"やどりぎの継続ターン数を {Player.MaxSeedTurn}ターン に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionSetCritDmgMultiplier(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!double.TryParse(order[2], out double critDmg) || critDmg < 0)
            {
                Warn("ダメージ値の入力が不正です。");
                return;
            }
            Player.CritDmg = critDmg;
            w.Message.Append(new ColoredString($"急所によるダメージ倍率を {Player.CritDmg}倍 に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionSetInsBufQty(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            if (!int.TryParse(order[2], out int insBufQty))
            {
                Warn("変化値の入力が不正です。");
                return;
            }
            Player.InsBufQty = insBufQty;
            w.Message.Append(new ColoredString($"ほけん発動による攻撃力の変化を {Player.InsBufQty} 段階 に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionSetMode(string[] order)
        {
            if (order.Length != 3)
            {
                Warn();
                return;
            }
            var mode = order[2].StringToMode();
            if (mode is SBMode.Empty)
            {
                Warn($"モード {order[2]} が見つかりません。");
                return;
            }
            SetMode(mode);
            w.Message.Append(new ColoredString($"モードを {mode.ModeToString()} に設定しました。", ConsoleColor.DarkGreen));
        }
        static void OptionDefault(string[] order)
        {
            Warn($"オプション {order[1]} が存在しないか、書式が不正です。");
        }
        #endregion
        static void OnShowOrdered(object sender, CancellationTokenSource cts)
        {
            var orderline = (string[])sender;
            if (orderline.Length != 2)
            {
                WarnAndRefresh();
                return;
            }
            if (orderline[1] == STATUS)
            {
                ShowStatus();
            }
            else if (orderline[1] == OPTIONS)
            {
                ShowOptions();
            }
            else if (orderline[1] == LOG)
            {
                ShowLog();
            }
            else
                Warn($"表示する情報 {orderline[1]} が見つかりません。");
        }
        static void OnResetOrdered(object sender, CancellationTokenSource cts)
        {
            Reset(cts);
        }
        static void OnExitOrdered(object sender, CancellationTokenSource cts)
        {
            cts.Cancel();
        }
        static void OnHelpOrdered(object sender, CancellationTokenSource cts)
        {
            ShowHelp(cts);
        }

        static bool TryInferWordTypes(string name, out Word word)
        {
            if (name.IsWild())
            {
                word = new Word(name, CurrentP, WordType.Empty);
                return true;
            }
            if (TypedWords.TryGetValue(name, out var types))
            {
                var type1 = types[0];
                var type2 = types.Count > 1 ? types[1] : WordType.Empty;
                word = new Word(name, CurrentP, type1, type2);
                return true;
            }
            if (NoTypeWords.Contains(name) || NoTypeWordEx.Contains(name))
            {
                word = new Word(name, CurrentP, WordType.Empty);
                return true;
            }
            word = new Word();
            return false;
        }
        static void OnDefault(object sender, CancellationTokenSource cts)
        {
            var orderline = (string[])sender;
            if (orderline.Length > 0 && !string.IsNullOrWhiteSpace(orderline[0]))
            {
                bool inferFlag = true;
                var word = new Word();
                if (IsInferable && orderline.Length == 1)
                {
                    var name = KanaConverter.ToHiragana(orderline[0]).Replace('ヴ', 'ゔ');
                    inferFlag = TryInferWordTypes(name, out word);
                }
                else
                {
                    var type1 = orderline.Length > 1 ? orderline[1][0].CharToType() : WordType.Empty;
                    var type2 = orderline.Length > 1 ? orderline[1].Length == 2 ? orderline[1][1].CharToType() : WordType.Empty : WordType.Empty;
                    word = new Word(orderline[0], CurrentP, type1, type2);
                }
                if (IsStrict && inferFlag)
                {
                    var strictFlag = word.IsSuitable(OtherP.CurrentWord);
                    if (strictFlag > 0)
                    {
                        WarnAndRefresh("開始文字がマッチしていません。");
                        return;
                    }
                    if (strictFlag < 0)
                    {
                        WarnAndRefresh("「ん」で終わっています");
                        return;
                    }
                    if (UsedWords.Contains(word.Name) && !(CurrentP.Ability == Hanshoku && word.ContainsType(WordType.Animal)))
                    {
                        WarnAndRefresh("すでに使われた単語です");
                        return;
                    }
                }
                if (IsInferable)
                {
                    if (!inferFlag)
                    {
                        WarnAndRefresh("辞書にない単語です。");
                        return;
                    }
                }
                Action<object, CancellationTokenSource> func = word.IsBuf ? OnBufOrdered
                                                             : word.IsHeal ? OnHealOrdered
                                                             : word.IsSeed ? OnSeedOrdered
                                                             : OnAttackOrdered;
                func(word, cts);
                if (!word.Name.IsWild()) UsedWords.Add(word.Name);
                return;
            }
            Refresh();
            w.WriteLine();
        }
        static bool TryToggleTurn()
        {
            if (OtherP.IsPoisoned)
            {
                w.Message.Append(new ColoredString($"{OtherP.Name} は毒のダメージをうけた！", ConsoleColor.DarkGreen));
                OtherP.TakePoisonDmg();
            }
            if (OtherP.IsSeeded)
            {
                w.Message.Append(new ColoredString($"{CurrentP.Name} はやどりぎで体力を奪った！", ConsoleColor.DarkGreen));
                OtherP.TakeSeedDmg(CurrentP);
            }
            if (CurrentP.HP <= 0)
            {
                CurrentP.Kill();
                w.Message.Append(new ColoredString($"{OtherP.Name} は {CurrentP.Name} を倒した！", ConsoleColor.DarkGreen));
                return false;
            }
            if (OtherP.HP <= 0)
            {
                OtherP.Kill();
                w.Message.Append(new ColoredString($"{CurrentP.Name} は {OtherP.Name} を倒した！", ConsoleColor.DarkGreen));
                return false;
            }
            IsP1sTurn = !IsP1sTurn;
            return true;
        }
        static void Refresh()
        {
            w.StatusFieldPlayer1 = $"{p1.Name}: {p1.HP}/{p1.MaxHP}";
            w.StatusFieldPlayer2 = $"{p2.Name}: {p2.HP}/{p2.MaxHP}";
            w.WordFieldPlayer1 = $"{p1.Name}:\n       {p1.CurrentWord}";
            w.WordFieldPlayer2 = $"{p2.Name}:\n       {p2.CurrentWord}";
            if (!IsSuspended)
            {
                w.Message.Append(new ColoredString($"{CurrentP.Name} のターンです", ConsoleColor.Yellow));
                w.Message.Log.Append(new ColoredString($"{p1.Name}: {p1.HP}/{p1.MaxHP},     {p2.Name}: {p2.HP}/{p2.MaxHP}", ConsoleColor.DarkYellow));
            }
        }
        static void Warn(string s = WARNING)
        {
            w.Message.Append(new ColoredString(s, ConsoleColor.Red));
        }
        static void WarnAndRefresh(string s = WARNING)
        {
            Warn(s);
            Refresh();
            w.WriteLine();
        }
        static void ShowHelp(CancellationTokenSource cts)
        {
            IsSuspended = true;
            while (true)
            {
                Console.Clear();
                new ColoredString("ヘルプへようこそ。\n\n"
                    + "表示するヘルプを選択してください。\n\n"
                    + "・コマンドの使い方       → c キーを入力\n"
                    + "・タイプの入力法         → t キーを入力\n"
                    + "・とくせいの入力法       → a キーを入力\n"
                    + "・オプションの一覧       → o キーを入力\n"
                    + "・ヘルプの終了           → q キーを入力\n"
                    + "・アプリケーションの終了 → r キーを入力", ConsoleColor.Yellow).WriteLine();

                var helpOrder = Console.ReadLine() ?? string.Empty;
                Console.Clear();
                switch (helpOrder)
                {
                    case "c":
                        {
                            new ColoredString("・コマンドの入力について\n\n"
                                            + "アプリケーション中では、コマンドを用いて操作を行います。\n"
                                            + "具体的には以下のようなコマンドがあります。\n\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"・{ACTION} コマンド       ・{SHOW}   コマンド       ・{RESET}  コマンド\n"
                                            + $"・{HELP}   コマンド       ・{CHANGE} コマンド       ・{EXIT}   コマンド\n"
                                            + $"・{OPTION} コマンド\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString($"・{ACTION} コマンドの使い方\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"コマンド名を一切指定せずに実行すると、{ACTION} コマンドとして実行されます。\n\n{ACTION} コマンドは、ゲーム内で何か行動をするときに使用します。\n\n"
                                            + "使い方は以下の通りです。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"[単語名] [単語のタイプ指定]\n", ConsoleColor.Green).WriteLine();
                            new ColoredString($"(例): しっぺ JV     →   しっぺ(遊び / 暴力) で相手に攻撃する\n"
                                            + $"      もなりざ C　  →   もなりざ(芸術) で自分にバフをかける(とくせいが「ロックンロール」の場合)\n"
                                            + $"      うぃすぱー H  →   うぃすぱー(医療) で自分の体力を回復する\n"
                                            + $"      いぺ Y        →   いぺ(植物) で相手にやどりぎを植え付ける (とくせいが「やどりぎ」の場合)\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"また、{INFER} オプションが有効な場合には、一部タイプ名の省略が可能です。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): るいざ   →    るいざ CK\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString($"・{CHANGE} コマンドの使い方\n\n"
                                            + $"バトル中にとくせいを変更する場合には、{CHANGE} コマンドを使います。\n"
                                            + "具体的な入力の仕方は以下の通りです。\n\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"{CHANGE} [変更後のとくせいの指定]    ", ConsoleColor.Green).Write();
                            new ColoredString("(変更者を明示しない場合)\n", ConsoleColor.White).WriteLine();
                            new ColoredString($"{CHANGE} [変更するプレイヤー名] [変更後のとくせいの指定]     ", ConsoleColor.Green).Write();
                            new ColoredString("(変更者を明示する場合)\n", ConsoleColor.White).WriteLine();
                            new ColoredString($"(例): {CHANGE} いかすい      →   とくせいを「いかすい」に変更する\n"
                                            + $"      {CHANGE} じぶん ロクロ →   「じぶん」という名前のプレイヤーのとくせいを「ロックンロール」に変更する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"とくせいを変更するプレイヤーの名前は、直接名前を入力するか「{Player1}」「{Player2}」という名前で参照できます。\n"
                                            + "変更するプレイヤーを明示しない場合は、現在ターンが回ってきているプレイヤーを参照します。\n\n"
                                            + "とくせいの表記の仕方については [ヘルプ] > [とくせいの入力法] もご参照ください。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString("・その他のコマンドの使い方\n\n"
                                            + "他に、以下のようなコマンドが使用可能です。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"・{OPTION} コマンド\n\n  オプションを指定します。\n  詳しくは [ヘルプ] > [オプションの一覧] をご参照ください。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {INFINITE_SEED} {ENABLE}\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{RESET} コマンド\n\n  アプリケーションをリセットします。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {RESET}\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{EXIT} コマンド\n\n  アプリケーションを終了します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {EXIT}\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString($"・{HELP} コマンド\n\n  ヘルプを表示します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {HELP}\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{SHOW} コマンド\n\n  様々な情報を表示します。\n\n  パラメーターには\"{STATUS}\", \"{OPTIONS}\", \"{LOG}\" のいずれかを用いることができます。", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {SHOW} {STATUS}    →   プレイヤーの情報を表示する。\n"
                                            + $"      {SHOW} {OPTIONS}   →   オプションの状態を表示する。\n"
                                            + $"      {SHOW} {LOG}       →   ログを表示する。\n\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();

                            break;
                        }
                    case "t":
                        {
                            new ColoredString("・タイプの入力について\n\n"
                                + "コマンド中で単語のタイプを指定するには、アルファベットによる略記を用いる必要があります。\n"
                                + "詳細は以下の通りです。\n\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString("ノーマル → N    動物 → A    植物      → Y    地名 → G\n"
                                            + "感情     → E    芸術 → C    食べ物    → F    暴力 → V\n"
                                            + "医療     → H    人体 → B    機械      → M    理科 → Q\n"
                                            + "時間     → T    人物 → P    工作      → K    服飾 → L\n"
                                            + "社会     → S    遊び → J    虫        → D    数学 → X\n"
                                            + "暴言     → Z    宗教 → R    スポーツ　→ U    天気 → W\n"
                                            + "物語     → O\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("また、複合タイプは以下のように入力します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString("(例): るーじゅばっく AU  → るーじゅばっく(動物 / スポーツ) の意味\n"
                                            + "      いっこういっき SV → いっこういっき (社会 / 暴力) の意味\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"タイプを何も指定せずに入力すると、{INFER} オプションが有効な場合にはその単語から推論されるタイプが、\nそうでない場合には「無属性」が設定されます。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            break;
                        }
                    case "a":
                        {
                            new ColoredString("・とくせいの入力について\n\n"
                                            + "コマンド中でのとくせいの指定には、日本語表記、アルファベットによる略記、独自の略記の３つの方法があります。\n"
                                            + "詳細は以下の通りです。\n\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString("・日本語表記\n\n"
                                            + "日本語を入力することで、直接とくせいを指定できます。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {CHANGE} ロックンロール ({CHANGE} コマンド中で日本語表記を直接使用)\n\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("・アルファベットによる略記\n\n"
                                            + "アルファベットを指定することで、そのタイプに対応するとくせいを指定できます。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {CHANGE} N → {CHANGE} デバッガー と同じ意味\n"
                                            + $"      {CHANGE} E → {CHANGE} じょうねつ と同じ意味\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString("・独自の略記\n\n"
                                            + "とくせい指定用の独自の略記を用いることもできます。\n"
                                            + "詳細は以下の通りです。\n\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString("デバッガー       → deb    はんしょく     → brd    やどりぎ     → sed    グローバル     → gbl\n"
                                            + "じょうねつ       → psn    ロックンロール → rar    いかすい     → glt    むきむき       → msl\n"
                                            + "いしょくどうげん → mdc    からて         → kar    かちこち     → clk    じっけん       → exp\n"
                                            + "さきのばし       → prc    きょじん       → gnt    ぶそう       → arm    かさねぎ       → lyr\n"
                                            + "ほけん           → ins    かくめい       → rev    どくばり     → ndl    けいさん       → clc\n"
                                            + "ずぼし           → htm    しんこうしん   → fth    トレーニング → trn    たいふういっか → tph\n"
                                            + "俺文字           → orm\n\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("また、上記以外にも使用可能な表記が存在する場合があります。(例: 「出歯」「ロクロ」「WZ」など)\n\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            break;
                        }
                    case "o":
                        {
                            new ColoredString("・オプションについて\n\n"
                                            + $"{OPTION} コマンドを用いることで、ゲーム中に反映される設定を変更することができます。\n"
                                            + "現在、全部で１４種類のオプションを設定可能です。そのいずれも、次のように入力します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"{OPTION} [オプション名] {{オプションのパラメーター}}\n", ConsoleColor.Green).WriteLine();
                            new ColoredString("設定可能なオプション、及びその入力の仕方は以下の通りです。\n\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"・{SET_MAX_HP} オプション\n  指定したプレイヤーの最大HPを設定します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_MAX_HP} じぶん 40   → 「じぶん」という名前のプレイヤーの最大HPを 40 に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{INFINITE_SEED} オプション\n  やどりぎの継続ターン数が無限かどうかを設定します。\n"
                                            + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {INFINITE_SEED} {ENABLE}   → やどりぎの継続ターン数を無限に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{INFINITE_CURE} オプション\n  医療タイプの単語で回復可能な回数が無限かどうかを設定します。\n"
                                            + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {INFINITE_CURE} {DISABLE}   → 医療タイプの単語で回復可能な回数を有限(デフォルトでは５回)に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString($"・{ABIL_CHANGE} オプション\n  とくせいが変更可能かどうかを設定します。\n"
                                            + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {ABIL_CHANGE} {DISABLE}   → とくせいの変更を不可能に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{SET_ABIL_COUNT} オプション\n  とくせいの変更可能な回数を設定します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_ABIL_COUNT} 5   → とくせいの変更可能な回数を５回に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{SET_MAX_CURE_COUNT} オプション\n  医療タイプの単語で回復可能な回数を設定します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_MAX_CURE_COUNT} 3   → 医療タイプの単語で回復可能な回数を３回に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{SET_MAX_FOOD_COUNT} オプション\n  食べ物タイプの単語で回復可能な回数を設定します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_MAX_FOOD_COUNT} 6   → 食べ物タイプの単語で回復可能な回数を６回に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString($"・{SET_SEED_DMG} オプション\n  やどりぎによるダメージを設定します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_SEED_DMG} 5   → やどりぎのダメージを５に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{SET_MAX_SEED_TURN} オプション\n  やどりぎの継続ターン数を設定します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_MAX_SEED_TURN} 10   → やどりぎの継続ターン数を１０ターンに設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{SET_CRIT_DMG_MULTIPLIER} オプション\n  急所によるダメージ倍率を設定します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_CRIT_DMG_MULTIPLIER} 2.5   → 急所によるダメージ倍率を２.５倍に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{SET_INS_BUF_QTY} オプション\n  ほけん発動によって何段階攻撃力が変化するかを設定します。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_INS_BUF_QTY} 4   → ほけん発動による攻撃力の変化をを４段階に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString($"・{STRICT} オプション\n  有効にすると、厳密なしりとりのルールが適用されます。\n"
                                            + "  具体的には、以下の機能が有効になります。\n\n"
                                            + "・開始文字がマッチしない単語の禁止\n\n"
                                            + "・「ん」で終わる単語の禁止\n\n"
                                            + "・すでに使われた単語の再使用禁止\n\n"
                                            + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {STRICT} {ENABLE}   → ストリクトモードを有効にする\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString($"・{INFER} オプション\n  タイプ推論を行うかどうかを設定します。\n  有効にすると、一部の単語についてタイプが自動的に決定されます。\n"
                                            + "  また、辞書にない単語を使用できなくなります。\n"
                                            + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {INFER} {ENABLE}   → タイプの推論を有効にする\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            new ColoredString($"・{SET_MODE} オプション\n  複数のオプションをまとめて変更します。\n"
                                            + "パラメーターにはモード名を指定できます。\n\n"
                                            + "指定可能なモード名は以下の通りです。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"・{DEFAULT_MODE} モード\n\n  現環境のモード。\n  体力上限６０、とくせい変更３回、医療５回、やどりぎ４ターン。\n\n"
                                            + $"・{CLASSIC_MODE} モード\n\n  旧環境のモード。\n  体力上限５０、とくせい変更ナシ、医療・やどりぎ無限。\n\n"
                                            + $"・{AOS_MODE} モード\n\n  やどりぎ環境のモード。\n  体力上限６０、とくせい変更３回、医療５回、やどりぎ無限。\n", ConsoleColor.Yellow).WriteLine();
                            new ColoredString($"(例): {OPTION} {SET_MODE} {CLASSIC_MODE}   → モードを {CLASSIC_MODE} に設定する\n", ConsoleColor.Cyan).WriteLine();
                            new ColoredString("...続けるには任意のキーを押してください...", ConsoleColor.White).WriteLine();
                            Console.ReadLine();
                            break;
                        }
                    case "q":
                        goto loopend;
                    case "r":
                        cts.Cancel(); return;
                    default:
                        break;
                }
            }
        loopend:;
            new ColoredString("ヘルプを終了します。任意のキーを押してください。", ConsoleColor.Yellow).WriteLine();
            Console.ReadLine();
            Console.Clear();
            IsSuspended = false;
            Refresh();
            w.WriteLine();
        }
        static void ShowStatus()
        {
            IsSuspended = true;
            Console.Clear();
            new ColoredString("\n" + p1.GetStatusString() + p2.GetStatusString() + $"\n現在のターン: {CurrentP.Name}\n\n\n\n", ConsoleColor.Yellow).WriteLine();
            new ColoredString("終了するには、任意のキーを押してください. . . ", ConsoleColor.White).WriteLine();
            Console.ReadLine();
            Console.Clear();
            IsSuspended = false;
            Refresh();
            w.WriteLine();
        }
        static void ShowOptions()
        {
            IsSuspended = true;
            Console.Clear();
            new ColoredString("\n"
                            + $"{INFINITE_SEED}:      {IsSeedInfinite}\n\n"
                            + $"{INFINITE_CURE}:      {IsCureInfinite}\n\n"
                            + $"Strict Mode:       {IsStrict}\n\n"
                            + $"Type Inference:    {IsInferable}\n\n"
                            + $"MaxCureCount:      {Player.MaxCureCount}回\n\n"
                            + $"MaxFoodCount:      {Player.MaxFoodCount}回\n\n"
                            + $"SeedDamage:        {Player.SeedDmg}\n\n"
                            + $"MaxSeedTurn:       {Player.MaxSeedTurn}\n\n"
                            + $"InsBufQuantity:    {Player.InsBufQty}段階\n\n"
                            + $"CritDamageMultiplier:      {Player.CritDmg}倍\n\n"
                            + $"ChangeableAbility:         {IsAbilChangeable}\n\n"
                            + $"ChangeableAbilityCount:    {Player.MaxAbilChange}回\n\n", ConsoleColor.Yellow).WriteLine();
            new ColoredString("終了するには、任意のキーを押してください. . . ", ConsoleColor.White).WriteLine();
            Console.ReadLine();
            Console.Clear();
            IsSuspended = false;
            Refresh();
            w.WriteLine();
        }
        static void ShowLog()
        {
            IsSuspended = true;
            Console.Clear();
            var height = Console.WindowHeight - 12;
            if (w.Message.Log.Content.Count > height)
            {
                var logList = new List<MessageLog>();
                var numOfPages = w.Message.Log.Content.Count / height + 1;
                for (var i = 0; i < numOfPages; i++)
                {
                    var page = new MessageLog();
                    page.AppendMany(w.Message.Log.Content.Skip(i * height).Take(height));
                    logList.Add(page);
                }
                var index = 0;
                while (true)
                {
                    Console.Clear();
                    new ColoredString("ログ", ConsoleColor.White).WriteLine();
                    logList[index].WriteLine();
                    if (index > 0)
                        new ColoredString("\n前のページを表示するには p キーを押してください", ConsoleColor.White).WriteLine();
                    if (index < logList.Count - 1)
                        new ColoredString("\n次のページを表示するには n キーを押してください", ConsoleColor.White).WriteLine();
                    new ColoredString("\nログを消去するには c キーを押してください", ConsoleColor.White).WriteLine();
                    new ColoredString("\n終了するには r キーを押してください", ConsoleColor.White).WriteLine();
                    var order = Console.ReadLine() ?? string.Empty;
                    if (order == "r")
                        break;
                    else if (order == "c")
                    {
                        w.Message.Clear();
                        w.Message.Log.Clear();
                        Console.Clear();
                        Console.WriteLine("ログを消去しました。任意のキーを押してください. . .");
                        Console.ReadLine();
                        break;
                    }
                    else if (order == "n" && index < logList.Count - 1)
                    {
                        index++;
                        continue;
                    }
                    else if (order == "p" && index > 0)
                    {
                        index--;
                        continue;
                    }
                }
            }
            else
            {
                while (true)
                {
                    Console.Clear();
                    new ColoredString("ログ", ConsoleColor.White).WriteLine();
                    w.Message.Log.WriteLine();
                    new ColoredString("\nログを消去するには c キーを押してください", ConsoleColor.White).WriteLine();
                    new ColoredString("\n終了するには、r キーを押してください", ConsoleColor.White).WriteLine();
                    var order = Console.ReadLine() ?? string.Empty;
                    if (order == "r")
                        break;
                    else if (order == "c")
                    {
                        w.Message.Clear();
                        w.Message.Log.Clear();
                        Console.Clear();
                        Console.WriteLine("ログを消去しました。任意のキーを押してください. . .");
                        Console.ReadLine();
                        break;
                    }
                }
            }
            Console.Clear();
            IsSuspended = false;
            Refresh();
            w.WriteLine();
        }
        static void ExitApp()
        {
            Console.Clear();
            new ColoredString("アプリケーションを終了します。任意のキーを押してください. . .", ConsoleColor.Yellow).WriteLine();
            Console.ReadLine();
        }
        static bool TryStringToPlayer(string s, out Player p)
        {
            if (s == p1.Name || s is Player1 or "p1")
            {
                p = p1;
                return true;
            }
            if (s == p2.Name || s is Player2 or "p2")
            {
                p = p2;
                return true;
            }
            p = new();
            return false;
        }

        // WARNING: デバッグ用　使うな！
        #region DEBUG
        static void OnAddOrdered(object sender, CancellationTokenSource cts)
        {
            var orderline = (string[])sender;
            if (orderline.Length != 2)
            {
                WarnAndRefresh();
                return;
            }
            if (NoTypeWords.Contains(orderline[1]) || NoTypeWordEx.Contains(orderline[1]))
            {
                WarnAndRefresh($"単語「{orderline[1]}」は既に辞書に含まれています。");
                return;
            }
            var kanaCheck = new Regex(@"^[\u3040-\u309Fー]+$");
            if (!kanaCheck.IsMatch(orderline[1]))
            {
                WarnAndRefresh("ひらがな以外を含む入力は無効です。");
                return;
            }
            using var file = new StreamWriter(NoTypeWordExPath, true, Encoding.UTF8);
            NoTypeWordEx.Add(orderline[1]);
            file.WriteLine(orderline[1]);
            w.Message.Append(new ColoredString($"単語「{orderline[1]}」を辞書に追加しました。", ConsoleColor.Yellow));
            Refresh();
            w.WriteLine();
        }
        static void OnRemoveOrdered(object sender, CancellationTokenSource cts)
        {
            var orderline = (string[])sender;
            if (orderline.Length != 2)
            {
                WarnAndRefresh();
                return;
            }
            if (!NoTypeWordEx.Contains(orderline[1]))
            {
                WarnAndRefresh($"単語「{orderline[1]}」は拡張辞書に含まれていません。");
                return;
            }
            using var file = new StreamWriter(NoTypeWordExPath, false, Encoding.UTF8);
            NoTypeWordEx.Remove(orderline[1]);
            foreach (var i in NoTypeWordEx)
                file.WriteLine(i);
            w.Message.Append(new ColoredString($"単語「{orderline[1]}」を辞書から削除しました。", ConsoleColor.Yellow));
            Refresh();
            w.WriteLine();
        }
        static void OnSearchOrdered(object sender, CancellationTokenSource cts)
        {
        }
        static void Error()
        {
            var a = new int[1];
            var b = a[3];
        }
        #endregion

        #endregion
    }
    #region minor console classes
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
            HelpField = new ColoredString("\"help\" と入力するとヘルプを表示します", ConsoleColor.Green);
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
        public ConsoleColor Color { get; set; } = ConsoleColor.White;
        public ColoredString(string text, ConsoleColor color) => (Text, Color) = (text, color);
        public ColoredString() : this(string.Empty, ConsoleColor.White) { }
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
        public static implicit operator ColoredString(string text) => new(text, ConsoleColor.White);
    }
    #endregion
}
