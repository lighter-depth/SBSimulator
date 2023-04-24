using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static SBSimulator.src.Word;
using static SBSimulator.src.Player.PlayerAbility;
using static SBSimulator.src.SBOptions;
using static System.ConsoleColor;
using System.Text.RegularExpressions;
using Umayadia.Kana;

/*   ◆辞書について
 *   
 * 　辞書は全部で３種類。すべて dic ディレクトリ下に設置する。
 * 　無属性単語を扱った辞書は no type ディレクトリ直下に、
 * 　有属性単語を扱った辞書は typed ディレクトリ下に設置する。
 * 　
 * 　・no-type-words.csv
 * 　無属性の単語を扱った辞書。SBの辞書と比べて抜けが多い。（くれもんてぃーぬ　など）
 * 　保存形式は一行一単語。（単語A \n 単語B \n 単語C \n ...）
 * 　原則改変しない。
 * 　
 * 　・no-type-word-extension.csv
 * 　無属性の単語を扱った辞書。no-type-words.csv でカバーしきれない単語を補うため、
 * 　補助的に用いる。
 * 　原則、「ゲーム内には存在するが、no-type-words.csv には含まれていない単語」が含まれる。
 * 　ただし、タイプ付きの単語については後述の有属性辞書を用い、
 * 　この辞書には含めない。
 * 　
 * 　・有属性辞書
 * 　ファイル名は「typed-words-(単語の頭文字).csv」としている。
 * 　それぞれ対応した行を表すサブディレクトリ下に配置する。
 * 　no-tyoe-words.csv に含まれない単語のうち、タイプ付きの単語については
 * 　この辞書に含めている。
 * 　改変はSBwikiに従って行う。
 */

/* 　◆デバッグ用コマンドについて
 * 　
 * 　以下の４つのコマンドはデバッグ用であり、通常利用時は用いない。
 * 　
 * 　・add コマンド
 * 　拡張無属性辞書(no-tyoe-word-extension.csv)に単語を追加する際に使用する。
 * 　入力はひらがなのみ可能。
 * 　ひらがな以外で入力を行った場合や、任意の辞書内に既に単語が存在している場合には警告となる。
 * 　    
 * 　・remove コマンド
 * 　拡張無属性辞書から単語を削除する際に使用する。
 * 　拡張無属性辞書内に該当する単語が存在しない場合には警告となる。
 * 　
 * 　・search コマンド
 * 　ワードサーチモードを起動する。 
 * 　
 * 　・error コマンド
 * 　例外を発生させ、アプリケーションを停止させる。
 */

// TODO: 辞書ディレクトリ探索の改善
// TODO: ユーザー定義の無属性・有属性辞書の作成
// TODO: オミット辞書の作成
// TODO: ワードサーチ機能の実装
// TODO: 辞書パース機能の実装
// TODO: とくせい適用の柔軟化（埋め込みを避ける）

namespace SBSimulator.src
{
    class Program
    {
        #region static fields
        static readonly string Version = "v0.2.1";
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
            ["ch"] = OnChangeOrdered,
            [OPTION] = OnOptionOrdered,
            ["op"] = OnOptionOrdered,
            [SHOW] = OnShowOrdered,
            ["sh"] = OnShowOrdered,
            [RESET] = OnResetOrdered,
            ["rs"] = OnResetOrdered,
            [EXIT] = OnExitOrdered,
            ["ex"] = OnExitOrdered,
            [HELP] = OnHelpOrdered,
            [ADD] = __OnAddOrdered,
            [REMOVE] = __OnRemoveOrdered,
            [SEARCH] = __OnSearchOrdered,
            [ERROR] = __OnErrorOrdered
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
        const string SEARCH = "__search";
        const string ERROR = "__error";
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
        const string INFO = "info";
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
                ("\n\n\n---予期せぬエラーが発生しました---\n\n\n", Red).WriteLine();
                (exc.Message + "\n" + exc.StackTrace, Yellow).WriteLine();
                ("\n\n---この画面のスクリーンショットを開発者に送信してください---\n\n開発者のtwitterアカウント: ", Cyan).Write();
                ("https://twitter.com/lighter_depth", DarkGreen).WriteLine();
                Console.WriteLine("\n\n\n\n任意のキーを押してアプリケーションを終了します. . . ");
                Console.ReadLine();
            }

        }
        #region handlers
        static void OnOrdered(string order, CancellationTokenSource cts)
        {
            var orderline = order.Trim().Split();
            if (OrderFunctions.TryGetValue(orderline[0].ToLower(), out var func))
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
            w.Message.Log.Append($"{CurrentP.Name} は単語 {word} で攻撃した！", DarkCyan);
            w.Message.Append(word.CalcAmp(OtherP.CurrentWord) switch
            {
                0 => "こうかがないようだ...",
                >= 2 => "こうかはばつぐんだ！",
                > 0 and < 1 => "こうかはいまひとつのようだ...",
                1 => "ふつうのダメージだ",
                _ => string.Empty
            }, Magenta);

            // 急所の処理
            if (critFlag)
                w.Message.Append("急所に当たった！", Magenta);

            // 暴力タイプの処理
            if (word.ContainsType(WordType.Violence))
            {
                if (CurrentP.Ability == MukiMuki && CurrentP.TryChangeATK(-1, word)) w.Message.Append($"{CurrentP.Name} の攻撃が下がった！(現在{CurrentP.ATK,0:0.0#}倍)", Blue);
                else if (CurrentP.TryChangeATK(-2, word)) w.Message.Append($"{CurrentP.Name} の攻撃ががくっと下がった！(現在{CurrentP.ATK,0:0.0#}倍)", Blue);
                else w.Message.Append($"{CurrentP.Name} の攻撃はもう下がらない！", Blue);
            }

            // かくめいの処理
            if (word.IsRev)
            {
                CurrentP.Rev(OtherP);
                w.Message.Append("すべての能力変化がひっくりかえった！", Cyan);
            }
            // たいふういっかの処理
            if (word.IsWZ)
            {
                CurrentP.WZ(OtherP);
                w.Message.Append("すべての能力変化が元に戻った！", Cyan);
            }

            // どくばりの処理
            if (word.IsPoison && OtherP.IsPoisoned == false)
            {
                OtherP.Poison();
                w.Message.Append($"{OtherP.Name} は毒を受けた！", DarkGreen);
            }

            // ほけんの処理
            if (OtherP.Ability == Hoken && word.CalcAmp(OtherP.CurrentWord) >= 2)
            {
                OtherP.TryChangeATK(Player.InsBufQty, OtherP.CurrentWord);
                w.Message.Append($"{OtherP.Name} は弱点を突かれて攻撃がぐぐーんと上がった！ (現在{OtherP.ATK,0:0.0#}倍)", Blue);
            }

            // 死んだかどうかの判定、ターンの交替
            if (!TryToggleTurn()) Reset(cts);
            Refresh();
            w.WriteLine();
        }
        static void OnBufOrdered(object sender, CancellationTokenSource cts)
        {
            var word = (Word)sender;
            w.Message.Log.Append($"{CurrentP.Name} は単語 {word} で能力を高めた！", DarkCyan);
            if (word.IsATKBuf)
            {
                var rarFlag = word.ContainsType(WordType.Art) && CurrentP.Ability == RocknRoll;
                if (rarFlag && CurrentP.TryChangeATK(2, word))
                    w.Message.Append($"{CurrentP.Name} の攻撃がぐーんと上がった！(現在{CurrentP.ATK,0:0.0#}倍)", Blue);
                else if (!rarFlag && CurrentP.TryChangeATK(1, word))
                    w.Message.Append($"{CurrentP.Name} の攻撃が上がった！(現在{CurrentP.ATK,0:0.0#}倍)", Blue);
                else
                    w.Message.Append($"{CurrentP.Name} の攻撃はもう上がらない！", Yellow);
            }
            else if (word.IsDEFBuf)
            {
                if (CurrentP.TryChangeDEF(1, word))
                    w.Message.Append($"{CurrentP.Name} の防御が上がった！(現在{CurrentP.DEF,0:0.0#}倍)", Blue);
                else
                    w.Message.Append($"{CurrentP.Name} の防御はもう上がらない！", Yellow);
            }
            if (!TryToggleTurn()) Reset(cts);
            Refresh();
            w.WriteLine();
        }
        static void OnHealOrdered(object sender, CancellationTokenSource cts)
        {
            var word = (Word)sender;
            w.Message.Log.Append($"{CurrentP.Name} は単語 {word} を使った！", DarkCyan);
            if (CurrentP.TryHeal(word))
            {
                if (word.ContainsType(WordType.Health) && !word.ContainsType(WordType.Food) && CurrentP.IsPoisoned || word.ContainsType(WordType.Food) && CurrentP.Ability == Ishoku && CurrentP.IsPoisoned)
                {
                    w.Message.Append($"{CurrentP.Name} の毒がなおった！", Green);
                    CurrentP.DePoison();
                }
                w.Message.Append($"{CurrentP.Name} の体力が回復した", Green);
            }
            else if (word.ContainsType(WordType.Food))
                w.Message.Append($"{CurrentP.Name} はもう食べられない！", Yellow);
            else if (word.ContainsType(WordType.Health))
                w.Message.Append($"{CurrentP.Name} はもう回復できない！", Yellow);
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
            w.Message.Log.Append($"{CurrentP.Name} は単語 {word} でやどりぎを植えた！", DarkCyan);
            OtherP.Seed(CurrentP, word);
            w.Message.Append($"{CurrentP.Name} は {OtherP.Name} に種を植え付けた！", DarkGreen);
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
                    w.Message.Append($"{CurrentP.Name} はとくせいを {nextAbil.AbilToString()} に変更しました", Cyan);
                else
                    w.Message.Append($"{CurrentP.Name} はもう特性を変えられない！", Yellow);
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
                    w.Message.Append($"{abilChangingP.Name} はとくせいを {nextAbil.AbilToString()} に変更しました", Cyan);
                else
                    w.Message.Append($"{abilChangingP.Name} はもう特性を変えられない！", Yellow);
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
            w.Message.Append($"{p.Name} の最大HPを {p.MaxHP} に設定しました。", DarkGreen);
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
                w.Message.Append("やどりぎの継続ターン数を 無限 に変更しました。", DarkGreen);
                return;
            }
            IsSeedInfinite = false;
            w.Message.Append($"やどりぎの継続ターン数を {Player.MaxSeedTurn} ターン に変更しました。", DarkGreen);
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
                w.Message.Append("医療タイプの単語で回復可能な回数を 無限 に変更しました。", DarkGreen);
                return;
            }
            IsCureInfinite = false;
            w.Message.Append($"医療タイプの単語で回復可能な回数を {Player.MaxCureCount}回 に変更しました。", DarkGreen);
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
                w.Message.Append($"とくせいの変更を有効にしました。(上限 {Player.MaxAbilChange}回 まで)", DarkGreen);
                return;
            }
            IsAbilChangeable = false;
            w.Message.Append($"とくせいの変更を無効にしました。", DarkGreen);

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
                w.Message.Append($"ストリクト モードを有効にしました。", DarkGreen);
                return;
            }
            IsStrict = false;
            w.Message.Append($"ストリクト モードを無効にしました。", DarkGreen);
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
                w.Message.Append($"タイプの推論を有効にしました。", DarkGreen);
                return;
            }
            IsInferable = false;
            w.Message.Append($"タイプの推論を無効にしました。", DarkGreen);
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
            w.Message.Append($"とくせいの変更回数上限を {Player.MaxAbilChange}回 に設定しました。", DarkGreen);
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
            w.Message.Append($"医療タイプの単語による回復の回数上限を {Player.MaxCureCount}回 に設定しました。", DarkGreen);
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
            w.Message.Append($"食べ物タイプの単語による回復の回数上限を {Player.MaxFoodCount}回 に設定しました。", DarkGreen);
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
            w.Message.Append($"やどりぎによるダメージを {Player.SeedDmg} に設定しました。", DarkGreen);
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
            w.Message.Append($"やどりぎの継続ターン数を {Player.MaxSeedTurn}ターン に設定しました。", DarkGreen);
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
            w.Message.Append($"急所によるダメージ倍率を {Player.CritDmg}倍 に設定しました。", DarkGreen);
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
            w.Message.Append($"ほけん発動による攻撃力の変化を {Player.InsBufQty} 段階 に設定しました。", DarkGreen);
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
            w.Message.Append($"モードを {mode.ModeToString()} に設定しました。", DarkGreen);
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
            if (orderline[1] is not (STATUS or OPTIONS or LOG or INFO))
            {
                WarnAndRefresh($"表示する情報 {orderline[1]} が見つかりません。");
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
            else if (orderline[1] == INFO)
            {
                ShowInfo();
            }
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
        #endregion

        #region minor methods
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
            ("しりとりバトルシミュレーターへようこそ。", Yellow).WriteLine();
            while (true)
            {
                var NGNamesList = new[] { "p1", "p2", Player1, Player2 };
                string? p1Name, p2Name;
                const string DEFAULT_P1_NAME = "じぶん";
                const string DEFAULT_P2_NAME = "あいて";
                ($"プレイヤーの名前を入力してください。(デフォルトでは「{DEFAULT_P1_NAME}」です)", Yellow).WriteLine();
                p1Name = Console.ReadLine();
                if (string.IsNullOrEmpty(p1Name)) p1Name = DEFAULT_P1_NAME;
                p1Name = p1Name.Trim();
                if (NGNamesList.Contains(p1Name))
                {
                    ($"名前{p1Name} は使用できません。", Red).WriteLine();
                    p1Name = DEFAULT_P1_NAME;
                }
                else
                    foreach (char c in p1Name)
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            ($"名前{p1Name} は空白を含むため使用できません。", Red).WriteLine();
                            p1Name = DEFAULT_P1_NAME;
                            break;
                        }
                    }
                ($"プレイヤーの名前を {p1Name} に設定しました。", Green).WriteLine();
                ($"相手の名前を入力してください。(デフォルトでは「{DEFAULT_P2_NAME}」です)", Yellow).WriteLine();
                p2Name = Console.ReadLine();
                if (string.IsNullOrEmpty(p2Name)) p2Name = DEFAULT_P2_NAME;
                p2Name = p2Name.Trim();
                if (NGNamesList.Contains(p2Name) || p2Name.Contains(' '))
                {
                    ($"名前{p2Name} は使用できません。", Red).WriteLine();
                    p2Name = DEFAULT_P2_NAME;
                }
                else
                    foreach (char c in p2Name)
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            ($"名前{p2Name} は空白を含むため使用できません。", Red).WriteLine();
                            p2Name = DEFAULT_P2_NAME;
                            break;
                        }
                    }
                ($"相手の名前を {p2Name} に設定しました。", Green).WriteLine();
                Player.PlayerAbility p1Abil, p2Abil;
                ("プレイヤーの初期特性を入力してください。(デフォルトではデバッガーになります)", Yellow).WriteLine();
                p1Abil = (Console.ReadLine() ?? "N").StringToAbil();
                if (p1Abil == Empty) p1Abil = Debugger;
                ($"{p1Name} の初期特性を {p1Abil.AbilToString()} に設定しました。", Green).WriteLine();
                ("相手の初期特性を入力してください。(デフォルトではデバッガーになります)", Yellow).WriteLine();
                p2Abil = (Console.ReadLine() ?? "N").StringToAbil();
                if (p2Abil == Empty) p2Abil = Debugger;
                ($"{p2Name} の初期特性を {p2Abil.AbilToString()} に設定しました。", Green).WriteLine();
                ("この設定でよろしいですか？", Yellow).WriteLine();
                Console.WriteLine();
                ($"プレイヤーの名前: {p1Name}, プレイヤーの初期特性: {p1Abil.AbilToString()}", Cyan).WriteLine();
                ($"相手の名前: {p2Name}, 相手の初期特性: {p2Abil.AbilToString()}", Cyan).WriteLine();
                Console.WriteLine();
                ("OK！ → 任意のキーを入力", Yellow).WriteLine();
                ("ダメ！ → r キーを入力", Yellow).WriteLine();
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
            w.Message.Append("やり直す？", Yellow);
            w.Message.Append("\nやり直す！ → 任意のキーを入力", Yellow);
            w.Message.Append("ログを表示 → l キーを入力", Yellow);
            w.Message.Append("もうやめる！ → r キーを入力", Yellow);
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
        static bool TryToggleTurn()
        {
            if (OtherP.IsPoisoned)
            {
                w.Message.Append($"{OtherP.Name} は毒のダメージをうけた！", DarkGreen);
                OtherP.TakePoisonDmg();
            }
            if (OtherP.IsSeeded)
            {
                w.Message.Append($"{CurrentP.Name} はやどりぎで体力を奪った！", DarkGreen);
                OtherP.TakeSeedDmg(CurrentP);
            }
            if (CurrentP.HP <= 0)
            {
                CurrentP.Kill();
                w.Message.Append($"{OtherP.Name} は {CurrentP.Name} を倒した！", DarkGreen);
                return false;
            }
            if (OtherP.HP <= 0)
            {
                OtherP.Kill();
                w.Message.Append($"{CurrentP.Name} は {OtherP.Name} を倒した！", DarkGreen);
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
                w.Message.Append($"{CurrentP.Name} のターンです", Yellow);
                w.Message.Log.Append($"{p1.Name}: {p1.HP}/{p1.MaxHP},     {p2.Name}: {p2.HP}/{p2.MaxHP}", DarkYellow);
            }
        }
        static void Warn(string s = WARNING)
        {
            w.Message.Append(s, Red);
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
                ("ヘルプへようこそ。\n\n"
                 + "表示するヘルプを選択してください。\n\n"
                 + "・コマンドの使い方       → c キーを入力\n"
                 + "・タイプの入力法         → t キーを入力\n"
                 + "・とくせいの入力法       → a キーを入力\n"
                 + "・オプションの一覧       → o キーを入力\n"
                 + "・キーワードの省略法     → k キーを入力\n"
                 + "・ヘルプの終了           → q キーを入力\n"
                 + "・アプリケーションの終了 → r キーを入力", Yellow).WriteLine();

                var helpOrder = Console.ReadLine() ?? string.Empty;
                Console.Clear();
                switch (helpOrder)
                {
                    case "c":
                        {
                            ("・コマンドの入力について\n\n"
                             + "アプリケーション中では、コマンドを用いて操作を行います。\n"
                             + "具体的には以下のようなコマンドがあります。\n\n", Yellow).WriteLine();
                            ($"・{ACTION} コマンド       ・{SHOW}   コマンド       ・{RESET}  コマンド\n"
                             + $"・{HELP}   コマンド       ・{CHANGE} コマンド       ・{EXIT}   コマンド\n"
                             + $"・{OPTION} コマンド\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ($"・{ACTION} コマンドの使い方\n", Yellow).WriteLine();
                            ($"コマンド名を一切指定せずに実行すると、{ACTION} コマンドとして実行されます。\n\n{ACTION} コマンドは、ゲーム内で何か行動をするときに使用します。\n\n"
                                            + "使い方は以下の通りです。\n", Yellow).WriteLine();
                            ($"[単語名] [単語のタイプ指定]\n", Green).WriteLine();
                            ($"(例): しっぺ JV     →   しっぺ(遊び / 暴力) で相手に攻撃する\n"
                             + $"      もなりざ C　  →   もなりざ(芸術) で自分にバフをかける(とくせいが「ロックンロール」の場合)\n"
                             + $"      いぺ Y        →   いぺ(植物) で相手にやどりぎを植え付ける (とくせいが「やどりぎ」の場合)\n", Cyan).WriteLine();
                            ($"また、{INFER} オプションが有効な場合には、一部タイプ名の省略が可能です。\n", Yellow).WriteLine();
                            ($"(例): るいざ   →    るいざ CK\n", Cyan).WriteLine();
                            ($"・ワイルドカード\n", Yellow).WriteLine();
                            ($"{ACTION} コマンド中の「単語名」パラメーター中に、アスタリスク記号 (\"", Yellow).Write();
                            (" * ", Green).Write();
                            ($"\") を含めることで、\n{INFER} オプションや{STRICT} オプションによる制限を軽減することができます。\n", Yellow).WriteLine();
                            ("(例): あ*****ぞ D  →   「あ」で始まり「ぞ」で終わる、7文字の虫タイプの単語", Cyan).WriteLine();
                            ("　    お***** JG   →   「お」で始まり6文字の、遊び・地名複合タイプの単語", Cyan).WriteLine();
                            ("　    * F          →    任意の文字を受け付け、任意の文字に使用できる、1文字の食べ物タイプの単語\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ($"・{CHANGE} コマンドの使い方\n\n"
                             + $"バトル中にとくせいを変更する場合には、{CHANGE} コマンドを使います。\n"
                             + "具体的な入力の仕方は以下の通りです。\n\n", Yellow).WriteLine();
                            ($"{CHANGE} [変更後のとくせいの指定]    ", Green).Write();
                            ("(変更者を明示しない場合)\n", White).WriteLine();
                            ($"{CHANGE} [変更するプレイヤー名] [変更後のとくせいの指定]     ", Green).Write();
                            ("(変更者を明示する場合)\n", White).WriteLine();
                            ($"(例): {CHANGE} いかすい      →   とくせいを「いかすい」に変更する\n"
                             + $"      {CHANGE} じぶん ロクロ →   「じぶん」という名前のプレイヤーのとくせいを「ロックンロール」に変更する\n", Cyan).WriteLine();
                            ($"とくせいを変更するプレイヤーの名前は、直接名前を入力するか「{Player1}」「{Player2}」という名前で参照できます。\n"
                             + "変更するプレイヤーを明示しない場合は、現在ターンが回ってきているプレイヤーを参照します。\n\n"
                             + "とくせいの表記の仕方については [ヘルプ] > [とくせいの入力法] もご参照ください。\n", Yellow).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ("・その他のコマンドの使い方\n\n" + "他に、以下のようなコマンドが使用可能です。\n", Yellow).WriteLine();
                            ($"・{OPTION} コマンド\n\n  オプションを指定します。\n  詳しくは [ヘルプ] > [オプションの一覧] をご参照ください。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {INFINITE_SEED} {ENABLE}\n", Cyan).WriteLine();
                            ($"・{RESET} コマンド\n\n  アプリケーションをリセットします。\n", Yellow).WriteLine();
                            ($"(例): {RESET}\n", Cyan).WriteLine();
                            ($"・{EXIT} コマンド\n\n  アプリケーションを終了します。\n", Yellow).WriteLine();
                            ($"(例): {EXIT}\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ($"・{HELP} コマンド\n\n  ヘルプを表示します。\n", Yellow).WriteLine();
                            ($"(例): {HELP}\n", Cyan).WriteLine();
                            ($"・{SHOW} コマンド\n\n  様々な情報を表示します。\n\n  パラメーターには\"{STATUS}\", \"{OPTIONS}\", \"{LOG}\", \"{INFO}\" のいずれかを用いることができます。", Yellow).WriteLine();
                            ($"(例): {SHOW} {STATUS}    →   プレイヤーの情報を表示する。\n"
                             + $"      {SHOW} {OPTIONS}   →   オプションの状態を表示する。\n"
                             + $"      {SHOW} {LOG}       →   ログを表示する。\n"
                             + $"      {SHOW} {INFO}      →   アプリの情報を表示する。\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();

                            break;
                        }
                    case "t":
                        {
                            ("・タイプの入力について\n\n" + "コマンド中で単語のタイプを指定するには、アルファベットによる略記を用いる必要があります。\n"
                                + "詳細は以下の通りです。\n\n", Yellow).WriteLine();
                            ("ノーマル → N    動物 → A    植物      → Y    地名 → G\n"
                                + "感情     → E    芸術 → C    食べ物    → F    暴力 → V\n"
                                + "医療     → H    人体 → B    機械      → M    理科 → Q\n"
                                + "時間     → T    人物 → P    工作      → K    服飾 → L\n"
                                + "社会     → S    遊び → J    虫        → D    数学 → X\n"
                                + "暴言     → Z    宗教 → R    スポーツ　→ U    天気 → W\n"
                                + "物語     → O\n", Cyan).WriteLine();
                            ("また、複合タイプは以下のように入力します。\n", Yellow).WriteLine();
                            ("(例): るーじゅばっく AU  → るーじゅばっく(動物 / スポーツ) の意味\n"
                                + "      いっこういっき SV → いっこういっき (社会 / 暴力) の意味\n", Cyan).WriteLine();
                            ($"タイプを何も指定せずに入力すると、{INFER} オプションが有効な場合にはその単語から推論されるタイプが、\nそうでない場合には「無属性」が設定されます。\n", Yellow).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            break;
                        }
                    case "a":
                        {
                            ("・とくせいの入力について\n\n" 
                           + "コマンド中でのとくせいの指定には、日本語表記、アルファベットによる略記、独自の略記の３つの方法があります。\n" 
                           + "詳細は以下の通りです。\n\n", Yellow).WriteLine();
                            ("・日本語表記\n\n" + "日本語を入力することで、直接とくせいを指定できます。\n", Yellow).WriteLine();
                            ($"(例): {CHANGE} ロックンロール ({CHANGE} コマンド中で日本語表記を直接使用)\n\n", Cyan).WriteLine();
                            ("・アルファベットによる略記\n\n" + "アルファベットを指定することで、そのタイプに対応するとくせいを指定できます。\n", Yellow).WriteLine();
                            ($"(例): {CHANGE} N → {CHANGE} デバッガー と同じ意味\n" 
                           + $"      {CHANGE} E → {CHANGE} じょうねつ と同じ意味\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ("・独自の略記\n\n" + "とくせい指定用の独自の略記を用いることもできます。\n" + "詳細は以下の通りです。\n\n", Yellow).WriteLine();
                            ("デバッガー       → deb    はんしょく     → brd    やどりぎ     → sed    グローバル     → gbl\n" 
                           + "じょうねつ       → psn    ロックンロール → rar    いかすい     → glt    むきむき       → msl\n" 
                           + "いしょくどうげん → mdc    からて         → kar    かちこち     → clk    じっけん       → exp\n" 
                           + "さきのばし       → prc    きょじん       → gnt    ぶそう       → arm    かさねぎ       → lyr\n" 
                           + "ほけん           → ins    かくめい       → rev    どくばり     → ndl    けいさん       → clc\n" 
                           + "ずぼし           → htm    しんこうしん   → fth    トレーニング → trn    たいふういっか → tph\n" 
                           + "俺文字           → orm\n\n", Cyan).WriteLine();
                            ("また、上記以外にも使用可能な表記が存在する場合があります。(例: 「出歯」「ロクロ」「WZ」など)\n\n", Yellow).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            break;
                        }
                    case "o":
                        {
                            ("・オプションについて\n\n" + $"{OPTION} コマンドを用いることで、ゲーム中に反映される設定を変更することができます。\n" 
                           + "現在、全部で１４種類のオプションを設定可能です。そのいずれも、次のように入力します。\n", Yellow).WriteLine();
                            ($"{OPTION} [オプション名] {{オプションのパラメーター}}\n", Green).WriteLine();
                            ("設定可能なオプション、及びその入力の仕方は以下の通りです。\n\n", Yellow).WriteLine();
                            ($"・{SET_MAX_HP} オプション\n  指定したプレイヤーの最大HPを設定します。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_MAX_HP} じぶん 40   → 「じぶん」という名前のプレイヤーの最大HPを 40 に設定する\n", Cyan).WriteLine();
                            ($"・{INFINITE_SEED} オプション\n  やどりぎの継続ターン数が無限かどうかを設定します。\n" 
                           + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {INFINITE_SEED} {ENABLE}   → やどりぎの継続ターン数を無限に設定する\n", Cyan).WriteLine();
                            ($"・{INFINITE_CURE} オプション\n  医療タイプの単語で回復可能な回数が無限かどうかを設定します。\n" 
                           + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {INFINITE_CURE} {DISABLE}   → 医療タイプの単語で回復可能な回数を有限(デフォルトでは５回)に設定する\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ($"・{ABIL_CHANGE} オプション\n  とくせいが変更可能かどうかを設定します。\n" 
                           + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {ABIL_CHANGE} {DISABLE}   → とくせいの変更を不可能に設定する\n", Cyan).WriteLine();
                            ($"・{SET_ABIL_COUNT} オプション\n  とくせいの変更可能な回数を設定します。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_ABIL_COUNT} 5   → とくせいの変更可能な回数を５回に設定する\n", Cyan).WriteLine();
                            ($"・{SET_MAX_CURE_COUNT} オプション\n  医療タイプの単語で回復可能な回数を設定します。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_MAX_CURE_COUNT} 3   → 医療タイプの単語で回復可能な回数を３回に設定する\n", Cyan).WriteLine();
                            ($"・{SET_MAX_FOOD_COUNT} オプション\n  食べ物タイプの単語で回復可能な回数を設定します。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_MAX_FOOD_COUNT} 6   → 食べ物タイプの単語で回復可能な回数を６回に設定する\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ($"・{SET_SEED_DMG} オプション\n  やどりぎによるダメージを設定します。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_SEED_DMG} 5   → やどりぎのダメージを５に設定する\n", Cyan).WriteLine();
                            ($"・{SET_MAX_SEED_TURN} オプション\n  やどりぎの継続ターン数を設定します。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_MAX_SEED_TURN} 10   → やどりぎの継続ターン数を１０ターンに設定する\n", Cyan).WriteLine();
                            ($"・{SET_CRIT_DMG_MULTIPLIER} オプション\n  急所によるダメージ倍率を設定します。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_CRIT_DMG_MULTIPLIER} 2.5   → 急所によるダメージ倍率を２.５倍に設定する\n", Cyan).WriteLine();
                            ($"・{SET_INS_BUF_QTY} オプション\n  ほけん発動によって何段階攻撃力が変化するかを設定します。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_INS_BUF_QTY} 4   → ほけん発動による攻撃力の変化をを４段階に設定する\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ($"・{STRICT} オプション\n  有効にすると、厳密なしりとりのルールが適用されます。\n" 
                            + "  具体的には、以下の機能が有効になります。\n\n" + "・開始文字がマッチしない単語の禁止\n\n" 
                            + "・「ん」で終わる単語の禁止\n\n" 
                            + "・すでに使われた単語の再使用禁止\n\n" 
                            + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {STRICT} {ENABLE}   → ストリクトモードを有効にする\n", Cyan).WriteLine();
                            ($"・{INFER} オプション\n  タイプ推論を行うかどうかを設定します。\n  有効にすると、一部の単語についてタイプが自動的に決定されます。\n" 
                            + "  また、辞書にない単語を使用できなくなります。\n" 
                            + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {INFER} {ENABLE}   → タイプの推論を有効にする\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            Console.Clear();
                            ($"・{SET_MODE} オプション\n  複数のオプションをまとめて変更します。\n" 
                                + "  パラメーターにはモード名を指定できます。\n\n" 
                                + "  指定可能なモード名は以下の通りです。\n", Yellow).WriteLine();
                            ($"・{DEFAULT_MODE} モード\n\n  現環境のモード。\n  体力上限６０、とくせい変更３回、医療５回、やどりぎ４ターン。\n\n" 
                                + $"・{CLASSIC_MODE} モード\n\n  旧環境のモード。\n  体力上限５０、とくせい変更ナシ、医療・やどりぎ無限。\n\n" 
                                + $"・{AOS_MODE} モード\n\n  やどりぎ環境のモード。\n  体力上限６０、とくせい変更３回、医療５回、やどりぎ無限。\n", Yellow).WriteLine();
                            ($"(例): {OPTION} {SET_MODE} {CLASSIC_MODE}   → モードを {CLASSIC_MODE} に設定する\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
                            Console.ReadLine();
                            break;
                        }
                    case "k":
                        {
                            ("・キーワードの省略法について\n", Yellow).WriteLine();
                            ("一部のキーワードについては、省略して入力することが可能です。\n"
                                + "具体的な一覧は以下の通りです。\n", Yellow).WriteLine();
                            ($"{CHANGE}: ch     {OPTION}: op     {SHOW}: sh     {RESET}: rs     {EXIT}: ex\n\n"
                           + $"{Player1}: p1    {Player2}: p2    {ENABLE}: e    {DISABLE}: d\n\n"
                           + $"{SET_MAX_HP}: smh   {INFINITE_SEED}: is    {INFINITE_CURE}: ic    {ABIL_CHANGE}: ac\n\n"
                           + $"{SET_ABIL_COUNT}: sac  {SET_MAX_CURE_COUNT}: smc   {SET_MAX_FOOD_COUNT}: smf\n\n"
                           + $"{SET_SEED_DMG}: ssd   {SET_MAX_SEED_TURN}: sms   {SET_CRIT_DMG_MULTIPLIER}: scd\n\n"
                           + $"{SET_INS_BUF_QTY}: sib  {SET_MODE}: sm  {STRICT}: s   {INFER}: i\n\n"
                           + $"{DEFAULT_MODE}: d   {CLASSIC_MODE}; c   {AOS_MODE}: s\n\n\n", Cyan).WriteLine();
                            ("...続けるには任意のキーを押してください...", White).WriteLine();
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
            ("ヘルプを終了します。任意のキーを押してください。", Yellow).WriteLine();
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
            ("\n" + p1.GetStatusString() + p2.GetStatusString() + $"\n現在のターン: {CurrentP.Name}\n\n\n\n", Yellow).WriteLine();
            ("終了するには、任意のキーを押してください. . . ", White).WriteLine();
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
            ("\n"
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
             + $"ChangeableAbilityCount:    {Player.MaxAbilChange}回\n\n", Yellow).WriteLine();
            ("終了するには、任意のキーを押してください. . . ", White).WriteLine();
            Console.ReadLine();
            Console.Clear();
            IsSuspended = false;
            Refresh();
            w.WriteLine();
        }
        static void ShowInfo()
        {
            IsSuspended = true;
            Console.Clear();
            ("情報\n\n"
             + $"・現在のバージョン: {Version}\n\n"
             + "・制作: らいたー　（Twitterアカウント: https://twitter.com/lighter_depth）\n\n"
             + "不具合が発生した場合には、上記のTwitterアカウントにご連絡ください。\n\n\n\n"
             + "SBSimulatorは、ブラウザゲーム「しりとりバトル」を参考に作成した、ファンメイドのアプリケーションです。\n\n"
             + "二次配布・商用利用を固く禁止します。\n\n", Yellow).WriteLine();
            ("・ゲーム「しりとりバトル」のURL: http://siritori-battle.net/\n\n"
             + "・「しりとりバトル」の制作者、ささみJP氏のTwitterアカウント: https://twitter.com/sasamijp\n\n\n", DarkYellow).WriteLine();
            ("終了するには、任意のキーを押してください. . . ", White).WriteLine();
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
                    ("ログ", White).WriteLine();
                    logList[index].WriteLine();
                    if (index > 0)
                        ("\n前のページを表示するには p キーを押してください", White).WriteLine();
                    if (index < logList.Count - 1)
                        ("\n次のページを表示するには n キーを押してください", White).WriteLine();
                    ("\nログを消去するには c キーを押してください", White).WriteLine();
                    ("\n終了するには r キーを押してください", White).WriteLine();
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
                    ("ログ", White).WriteLine();
                    w.Message.Log.WriteLine();
                    ("\nログを消去するには c キーを押してください", White).WriteLine();
                    ("\n終了するには、r キーを押してください", White).WriteLine();
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
            ("アプリケーションを終了します。任意のキーを押してください. . .", Yellow).WriteLine();
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
        #endregion

        // WARNING: デバッグ用　使うな！
        #region DEBUG
        static void __OnAddOrdered(object sender, CancellationTokenSource cts)
        {
            var orderline = (string[])sender;
            if (orderline.Length != 2)
            {
                WarnAndRefresh();
                return;
            }
            if (NoTypeWords.Contains(orderline[1]) || NoTypeWordEx.Contains(orderline[1]) || TypedWords.ContainsKey(orderline[1]))
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
            w.Message.Append($"単語「{orderline[1]}」を辞書に追加しました。", Yellow);
            Refresh();
            w.WriteLine();
        }
        static void __OnRemoveOrdered(object sender, CancellationTokenSource cts)
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
            w.Message.Append($"単語「{orderline[1]}」を辞書から削除しました。", Yellow);
            Refresh();
            w.WriteLine();
        }
        static void __OnSearchOrdered(object sender, CancellationTokenSource cts)
        {
            IsSuspended = true;
            var searchMsg = new MessageBox();
            while (true)
            {
                Console.Clear();
                ("ワードサーチモード", White).WriteLine();
                searchMsg.Append("ワードサーチモードへようこそ。", Yellow);
                searchMsg.Append("\n");
                searchMsg.Append("検索条件を入力してください。\n\n", Yellow);
                searchMsg.Append("\n");
                searchMsg.Append("操作方法を表示するには「-m」と入力してください。", Cyan);
                searchMsg.Append("\n");
                searchMsg.Append("終了するには「-q」と入力してください。", Cyan);
                searchMsg.WriteLine();
                searchMsg.Clear();
                var order = Console.ReadLine()?.Trim().Split() ?? Array.Empty<string>();
                if (order.Length < 1 || string.IsNullOrWhiteSpace(order[0]))
                    continue;
                if (order[0] == "-q")
                {
                    Console.Clear();
                    Console.WriteLine("ワードサーチモードを終了します。\n\n任意のキーを押してください. . . ");
                    Console.ReadLine();
                    break;
                }
                if (order[0] == "-m")
                {
                    __ShowWordSearchManual();
                    continue;
                }
                var cond = order[0];
                int searchOption = 0;
                int dicOption = 0;
                if (order.Length > 1 && !__TryStringToSearchOption(order[1], out searchOption))
                {
                    searchMsg.Append($"サーチオプション{order[1]}が見つかりませんでした。", Red);
                    Console.Clear();
                    continue;
                }
                if (order.Length > 2 && !__TryStringToDicOption(order[2], out dicOption))
                {
                    searchMsg.Append($"辞書オプション{order[2]}が見つかりませんでした。", Red);
                    Console.Clear();
                    continue;
                }
                Console.Clear();
                ("ワードサーチモード\n", White).WriteLine();
                ("\nマッチする単語を探しています. . . \n", Yellow).WriteLine();
                if(!__TrySearch(cond, searchOption, dicOption, out var words))
                {
                    ("マッチする単語が見つかりませんでした。\n\n", Red).WriteLine();
                    Console.WriteLine("やり直すには任意のキーを押してください。");
                    Console.ReadLine();
                    continue;
                }
                ($"結果は{words.Count}件見つかりました。\n", Green).WriteLine();
                Console.WriteLine("表示するには任意のキーを押してください. . . ");
                Console.ReadLine();
                Console.Clear();
                var height = Console.WindowHeight - 10;
                if (words.Count > height)
                {
                    var wordBook = new List<List<string>>();
                    var numOfPages = words.Count / height + 1;
                    for (var i = 0; i < numOfPages; i++)
                    {
                        var page = new List<string>();
                        page.AddRange(words.Skip(i * height).Take(height));
                        wordBook.Add(page);
                    }
                    var index = 0;
                    while (true)
                    {
                        Console.Clear();
                        ("ワードサーチモード\n", White).WriteLine();
                        foreach(var i in wordBook[index])
                            (i, Yellow).WriteLine();
                        if (index > 0)
                            ("\n前のページを表示するには p キーを押してください", White).WriteLine();
                        if (index < wordBook.Count - 1)
                            ("\n次のページを表示するには n キーを押してください", White).WriteLine();
                        ("\n終了するには r キーを押してください", White).WriteLine();
                        var orderResult = Console.ReadLine() ?? string.Empty;
                        if (orderResult == "r")
                            break;
                        else if (orderResult == "n" && index < wordBook.Count - 1)
                        {
                            index++;
                            continue;
                        }
                        else if (orderResult == "p" && index > 0)
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
                        ("ワードサーチモード\n", White).WriteLine();
                        foreach (var i in words)
                            (i, Yellow).WriteLine();
                        ("\n終了するには、r キーを押してください", White).WriteLine();
                        var orderResult = Console.ReadLine() ?? string.Empty;
                        if (orderResult == "r")
                            break;
                    }
                }
                /*    
                 *    search options:
                 *    m => 0: search the word perfectly matches cond
                 *    c => 1: search the word contains cond
                 *    s => 2: search the word starts with a particular letter
                 *    e => 3: search the word ends with a particular letter
                 *    b => 4: search the word starts and ends with a particular letter
                 *    r => 5: search the word matches regex
                 *    t => 6: search the word matches types
                 */
                /*
                 *     dic options:
                 *     e => 0: search through every dictionary
                 *     n => 1: search through no-type-words.csv
                 *     x => 2: search through no-type-word.extension.csv
                 *     t => 3: search through typed
                 */
            }
            Console.Clear();
            IsSuspended = false;
            Refresh();
            w.WriteLine();
        }
        static void __ShowWordSearchManual()
        {
            Console.Clear();
            ("ワードサーチモード\n", White).WriteLine();
            ("・操作説明\n", Yellow).WriteLine();
            ("ワードサーチモードは、原則次のようなコマンドによって操作します。\n", Yellow).WriteLine();
            ("[検索する単語の条件] [検索オプション] [辞書オプション]\n", Green).WriteLine();
            ("辞書オプションを指定する場合、検索オプションを省略することはできません。\n", Yellow).WriteLine();
            ("・検索オプション\n", Yellow).WriteLine();
            ("現在、以下の６種類が使用可能です。いずれも、アルファベット一文字を指定します。\n"
                + "省略した場合は、m オプションとして処理されます。\n", Yellow).WriteLine();
            ("・m オプション  →  指定した文字列と完全一致する単語を検索します。\n"
           + "・c オプション  →  指定した文字列を含んでいる単語を検索します。\n"
           + "・s オプション  →  指定した文字列と頭文字が同じである単語を検索します。\n"
           + "・e オプション  →  指定した文字列と同じ文字で終わる単語を検索します。\n"
           + "・b オプション  →  指定した文字列と最初の文字/最後の文字がともに一致する単語を検索します。\n"
           + "・r オプション  →  正規表現を入力し、それにマッチする単語を検索します。\n", Cyan).WriteLine();
            ("続けるには、任意のキーを押してください. . . ", White).WriteLine();
            Console.ReadLine();
            Console.Clear();
            ("ワードサーチモード\n", White).WriteLine();
            ("・辞書オプション\n", Yellow).WriteLine();
            ("現在、以下の４種類が使用可能です。いずれも、アルファベット一文字を指定します。\n"
                + "省略した場合は、e オプションとして処理されます。\n", Yellow).WriteLine();
            ("・e オプション  →  すべての辞書を検索します。\n"
           + "・n オプション  →  無属性辞書のみ検索します。\n"
           + "・x オプション  →  拡張無属性辞書のみ検索します。\n"
           + "・t オプション  →  有属性辞書のみ検索します。\n", Cyan).WriteLine();
            ("続けるには、任意のキーを押してください. . . ", White).WriteLine();
            Console.ReadLine();
        }
        static bool __TrySearch(string name, int searchOption, int dicOption, out List<string> words)
        {
            bool result = false;
            words = new();
            var dic = dicOption switch
            {
                1 => NoTypeWords,
                2 => NoTypeWordEx,
                3 => TypedWords.Keys.ToList(),
                _ => NoTypeWords.Concat(NoTypeWordEx).Concat(TypedWords.Keys).ToList()
            };
            var r = new Regex(searchOption switch
            {
                0 => $"^{name}$",
                1 => name,
                2 => $"^{name[0]}",
                3 => $"{name[^1]}ー*$",
                4 => $@"^{name[0]}\w*{name[^1]}ー*$",
                5 => name,
                6 => name,
                _ => name
            });
            if (name[0] == name[^1] && searchOption == 4)
                r = new Regex($"^{name[0]}\\w*{name[^1]}ー*$|^{name[0]}ー*$");
            foreach(var i in dic)
            {
                if (r.IsMatch(i)) 
                {
                    result = true;
                    words.Add(i);
                }
            }
            words = words.Distinct().ToList();
            return result;
        }
        static void __OnErrorOrdered(object sender, CancellationTokenSource cts)
        {
            __Error();
        }
        static void __Error()
        {
            Console.WriteLine((new int[6])[9]);
        }
        static bool __TryStringToSearchOption(string order, out int option)
        {
            (var result, option) = order switch
            {
                "m" => (true, 0),
                "c" => (true, 1),
                "s" => (true, 2),
                "e" => (true, 3),
                "b" => (true, 4),
                "r" => (true, 5),
                "t" => (true, 6),
                _ => (false, 0)
            };
            return result;
        }
        static bool __TryStringToDicOption(string order, out int option)
        {
            (var result, option) = order switch
            {
                "e" => (true, 0),
                "n" => (true, 1),
                "x" => (true, 2),
                "t" => (true, 3),
                _ => (false, 0)
            };
            return result;
        }
        #endregion

        #endregion
    }
}
