using System.Text;
using static System.ConsoleColor;
using System.Text.RegularExpressions;

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
 * 　保存形式は no-type-words.csv と同じ。
 * 　
 * 　・有属性辞書
 * 　ファイル名は「typed-words-(単語の頭文字).csv」としている。
 * 　それぞれ対応した行を表すサブディレクトリ下に配置する。
 * 　no-tyoe-words.csv に含まれない単語のうち、タイプ付きの単語については
 * 　この辞書に含めている。
 * 　改変はSBwikiに従って行う。
 * 　また、aux-typed-words ディレクトリ下に補助用のタイプ付き単語リストを保存している。
 * 　いずれも保存形式は一行一単語。(単語A タイプ１ タイプ２ \n 単語B タイプ１ タイプ２ \n ...)
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

// TODO: ユーザー定義の無属性・有属性辞書の作成
// TODO: オミット辞書の作成
// TODO: 起動直後のパフォーマンス改善
// TODO: 状態異常の埋め込み解消（PlayerState クラス作成?）
// TODO: コマンドラインのオブジェクト化(SBOrder クラス作成)
// TODO: 即死検索メソッドの実装

namespace SBSimulator;

class Program
{
    #region static fields
    static readonly string Version = "v0.5.6";
    static readonly Window Window = new();
    static Mode Mode = new();
    static Battle Battle = new();
    static readonly string DicDir = GetDicPath();
    static readonly string NoTypeWordsPath = DicDir + @"\no type";
    static readonly string TypedWordsPath = DicDir + @"\typed";
    static readonly Task DictionaryImportTask;
    #endregion

    #region methods
    static Program()
    {
        DictionaryImportTask = Task.Run(ImportDictionaryAsync);
    }
    static void Main()
    {
        try
        {
            Initialize();
            Battle.Run();
            OnExitApp();
        }
        catch (Exception exc)
        {
            Console.Clear();
            ("\n\n\n---予期せぬエラーが発生しました---\n\n\n", Red).WriteLine();
            (exc.Message + "\n" + exc.StackTrace, Yellow).WriteLine();
            ("\n\n---この画面のスクリーンショットを開発者に送信してください---\n\n開発者のtwitterアカウント: ", Cyan).Write();
            ("https://twitter.com/lighter_depth", DarkGreen).WriteLine();
            Console.WriteLine("\n\n\n\n任意のキーを押してアプリケーションを終了します. . . ");
            Console.ReadKey();
        }
    }
    static void Initialize()
    {
        Console.Title = "しりバトシミュレーター";
        var (p1, p2) = SetUp();
        Console.WriteLine("辞書を読み込み中...\n\nしばらくお待ちください...");
        DictionaryImportTask.Wait();
        Battle = new Battle(p1, p2);
        Battle.Player1.Register(Battle);
        Battle.Player2.Register(Battle);
        Mode.Set(Battle);
        Battle.OnShowOrdered += OnShowOrdered;
        Battle.OnResetOrdered += OnResetOrdered;
        Battle.OnExitOrdered += OnExitOrdered;
        Battle.OnHelpOrdered += OnHelpOrdered;
        Battle.OnAddOrdered += OnAddOrdered;
        Battle.OnRemoveOrdered += OnRemoveOrdered;
        Battle.OnSearchOrdered += OnSearchOrdered;
        Battle.OnReset += Reset;
        Battle.In = () => Order.Parse(Console.ReadLine()?.Trim().Split() ?? Array.Empty<string>(), Battle);
        Battle.Out = Output;
    }
    #region handlers
    static void OnShowOrdered(Order order, CancellationTokenSource cts)
    {
        if (order.ErrorMessage is not null)
        {
            Warn(order.ErrorMessage);
            return;
        }
        var key = order.Body.ElementAtOrDefault(0);
        if (key is not ('s' or 'o' or 'l' or 'i'))
        {
            Warn($"表示する情報 {order.Body} が見つかりません。");
            return;
        }
        if (key is 's')
        {
            ShowStatus();
        }
        else if (key is 'o')
        {
            ShowOptions();
        }
        else if (key is 'l')
        {
            ShowLog();
        }
        else if (key is 'i')
        {
            ShowInfo();
        }
    }
    static void OnResetOrdered(Order order, CancellationTokenSource cts)
    {
        Reset(cts);
    }
    static void OnExitOrdered(Order order, CancellationTokenSource cts)
    {
        cts.Cancel();
    }
    static void OnHelpOrdered(Order order, CancellationTokenSource cts)
    {
        #region constants
        const string EXIT = "exit";
        const string RESET = "reset";
        const string CHANGE = "change";
        const string OPTION = "option";
        const string ENABLE = "enable";
        const string DISABLE = "disable";
        const string SHOW = "show";
        const string HELP = "help";
        const string PLAYER1 = "Player1";
        const string PLAYER2 = "Player2";
        const string STATUS = "status";
        const string OPTIONS = "options";
        const string LOG = "log";
        const string INFO = "info";
        #endregion

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
             + "・モードの設定方法       → m キーを入力\n"
             + "・ヘルプの終了           → q キーを入力\n"
             + "・アプリケーションの終了 → r キーを入力", Yellow).WriteLine();

            var helpOrder = Console.ReadKey().KeyChar.ToString() ?? string.Empty;
            Console.Clear();
            switch (helpOrder)
            {
                case "c":
                    {
                        ("・コマンドの入力について\n\n"
                         + "アプリケーション中では、コマンドを用いて操作を行います。\n"
                         + "具体的には以下のようなコマンドがあります。\n\n", Yellow).WriteLine();
                        ($"・単語 コマンド       ・{SHOW}   コマンド       ・{RESET}  コマンド\n"
                         + $"・{HELP}   コマンド       ・{CHANGE} コマンド       ・{EXIT}   コマンド\n"
                         + $"・{OPTION} コマンド\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ($"・単語 コマンドの使い方\n", Yellow).WriteLine();
                        ($"コマンド名を一切指定せずに実行すると、単語 コマンドとして実行されます。\n\n単語 コマンドは、ゲーム内で何か行動をするときに使用します。\n\n"
                                        + "使い方は以下の通りです。\n", Yellow).WriteLine();
                        ($"[単語名] [単語のタイプ指定]\n", Green).WriteLine();
                        ($"(例): しっぺ JV     →   しっぺ(遊び / 暴力) で相手に攻撃する\n"
                         + $"      もなりざ C　  →   もなりざ(芸術) で自分にバフをかける(とくせいが「ロックンロール」の場合)\n"
                         + $"      いぺ Y        →   いぺ(植物) で相手にやどりぎを植え付ける (とくせいが「やどりぎ」の場合)\n", Cyan).WriteLine();
                        ($"また、{nameof(Options.Infer)} オプションが有効な場合には、一部タイプ名の省略が可能です。\n", Yellow).WriteLine();
                        ($"(例): るいざ   →    るいざ CK\n", Cyan).WriteLine();
                        ($"・ワイルドカード\n", Yellow).WriteLine();
                        ($"単語 コマンド中の「単語名」パラメーター中に、アスタリスク記号 (\"", Yellow).Write();
                        (" * ", Green).Write();
                        ($"\") を含めることで、\n{nameof(Options.Infer)} オプションや{nameof(Options.Strict)} オプションによる制限を軽減することができます。\n", Yellow).WriteLine();
                        ("(例): あ*****ぞ D  →   「あ」で始まり「ぞ」で終わる、7文字の虫タイプの単語", Cyan).WriteLine();
                        ("　    お***** JG   →   「お」で始まり6文字の、遊び・地名複合タイプの単語", Cyan).WriteLine();
                        ("　    * F          →    任意の文字を受け付け、任意の文字に使用できる、1文字の食べ物タイプの単語\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
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
                        ($"とくせいを変更するプレイヤーの名前は、直接名前を入力するか「{PLAYER1}」「{PLAYER2}」という名前で参照できます。\n"
                         + "変更するプレイヤーを明示しない場合は、現在ターンが回ってきているプレイヤーを参照します。\n\n"
                         + "とくせいの表記の仕方については [ヘルプ] > [とくせいの入力法] もご参照ください。\n", Yellow).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ("・その他のコマンドの使い方\n\n" + "他に、以下のようなコマンドが使用可能です。\n", Yellow).WriteLine();
                        ($"・{OPTION} コマンド\n\n  オプションを指定します。\n  詳しくは [ヘルプ] > [オプションの一覧] をご参照ください。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.InfiniteSeed)} {ENABLE}\n", Cyan).WriteLine();
                        ($"・{RESET} コマンド\n\n  アプリケーションをリセットします。\n", Yellow).WriteLine();
                        ($"(例): {RESET}\n", Cyan).WriteLine();
                        ($"・{EXIT} コマンド\n\n  アプリケーションを終了します。\n", Yellow).WriteLine();
                        ($"(例): {EXIT}\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ($"・{HELP} コマンド\n\n  ヘルプを表示します。\n", Yellow).WriteLine();
                        ($"(例): {HELP}\n", Cyan).WriteLine();
                        ($"・{SHOW} コマンド\n\n  様々な情報を表示します。\n\n  パラメーターには\"{STATUS}\", \"{OPTIONS}\", \"{LOG}\", \"{INFO}\" のいずれかを用いることができます。", Yellow).WriteLine();
                        ($"(例): {SHOW} {STATUS}    →   プレイヤーの情報を表示する。\n"
                         + $"      {SHOW} {OPTIONS}   →   オプションの状態を表示する。\n"
                         + $"      {SHOW} {LOG}       →   ログを表示する。\n"
                         + $"      {SHOW} {INFO}      →   アプリの情報を表示する。\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();

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
                        ($"タイプを何も指定せずに入力すると、{nameof(Options.Infer)} オプションが有効な場合にはその単語から推論されるタイプが、\nそうでない場合には「無属性」が設定されます。\n", Yellow).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
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
                        Console.ReadKey();
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
                        Console.ReadKey();
                        break;
                    }
                case "o":
                    {
                        ("・オプションについて\n\n" + $"{OPTION} コマンドを用いることで、ゲーム中に反映される設定を変更することができます。\n"
                       + "現在、全部で１４種類のオプションを設定可能です。そのいずれも、次のように入力します。\n", Yellow).WriteLine();
                        ($"{OPTION} [オプション名] {{オプションのパラメーター}}\n", Green).WriteLine();
                        ("設定可能なオプション、及びその入力の仕方は以下の通りです。\n\n", Yellow).WriteLine();
                        ($"・{nameof(Options.SetMaxHP)} オプション\n  指定したプレイヤーの最大HPを設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetMaxHP)} じぶん 40   → 「じぶん」という名前のプレイヤーの最大HPを 40 に設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.InfiniteSeed)} オプション\n  やどりぎの継続ターン数が無限かどうかを設定します。\n"
                       + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.InfiniteSeed)} {ENABLE}   → やどりぎの継続ターン数を無限に設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.InfiniteCure)} オプション\n  医療タイプの単語で回復可能な回数が無限かどうかを設定します。\n"
                       + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.InfiniteCure)} {DISABLE}   → 医療タイプの単語で回復可能な回数を有限(デフォルトでは５回)に設定する\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ($"・{nameof(Options.AbilChange)} オプション\n  とくせいが変更可能かどうかを設定します。\n"
                       + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.AbilChange)} {DISABLE}   → とくせいの変更を不可能に設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.SetAbilCount)} オプション\n  とくせいの変更可能な回数を設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetAbilCount)} 5   → とくせいの変更可能な回数を５回に設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.SetMaxCureCount)} オプション\n  医療タイプの単語で回復可能な回数を設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetMaxCureCount)} 3   → 医療タイプの単語で回復可能な回数を３回に設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.SetMaxFoodCount)} オプション\n  食べ物タイプの単語で回復可能な回数を設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetMaxFoodCount)} 6   → 食べ物タイプの単語で回復可能な回数を６回に設定する\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ($"・{nameof(Options.SetSeedDmg)} オプション\n  やどりぎによるダメージを設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetSeedDmg)} 5   → やどりぎのダメージを５に設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.SetMaxSeedTurn)} オプション\n  やどりぎの継続ターン数を設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetMaxSeedTurn)} 10   → やどりぎの継続ターン数を１０ターンに設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.SetCritDmgMultiplier)} オプション\n  急所によるダメージ倍率を設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetCritDmgMultiplier)} 2.5   → 急所によるダメージ倍率を２.５倍に設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.SetInsBufQty)} オプション\n  ほけん発動によって何段階攻撃力が変化するかを設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetInsBufQty)} 4   → ほけん発動による攻撃力の変化をを４段階に設定する\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ($"・{nameof(Options.Strict)} オプション\n  有効にすると、厳密なしりとりのルールが適用されます。\n"
                        + "  具体的には、以下の機能が有効になります。\n\n" + "・開始文字がマッチしない単語の禁止\n\n"
                        + "・「ん」で終わる単語の禁止\n\n"
                        + "・すでに使われた単語の再使用禁止\n\n"
                        + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"disable\" を選択すると無効になります。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.Strict)} {ENABLE}   → ストリクトモードを有効にする\n", Cyan).WriteLine();
                        ($"・{nameof(Options.Infer)} オプション\n  タイプ推論を行うかどうかを設定します。\n  有効にすると、一部の単語についてタイプが自動的に決定されます。\n"
                        + "  また、辞書にない単語を使用できなくなります。\n"
                        + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"disable\" を選択すると無効になります。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.Infer)} {ENABLE}   → タイプの推論を有効にする\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ($"・{nameof(Options.SetMode)} オプション\n  複数のオプションをまとめて変更します。\n"
                        + "  パラメーターにはモード名を指定できます。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetMode)} Classic   → モードを Classic に設定する\n", Cyan).WriteLine();
                        ($"・{nameof(Options.SetHP)} オプション\n  指定したプレイヤーの体力を設定します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetHP)} あいて 30   → 「あいて」という名前のプレイヤーのHPを 30 に設定する\n", Cyan).WriteLine();
                        ("設定可能なモードの一覧については、[ヘルプ] > [モードの設定方法] もご参照ください。\n", Yellow).WriteLine();
                        ($"・{nameof(Options.SetLuck)} オプション\n  指定したプレイヤーの運を設定します。\n"
                            + "  パラメーターには「lucky」「normal」「unlucky」のいずれかを設定できます。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetLuck)} じぶん lucky   → 「じぶん」という名前のプレイヤーの運をを 「Lucky」 に設定する\n", Cyan).WriteLine();
                        ("設定可能なモードの一覧については、[ヘルプ] > [モードの設定方法] もご参照ください。\n", Yellow).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ($"・{nameof(Options.CustomAbil)} オプション\n  有効にすると、カスタム特性が使用可能になります。\n"
                       + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.CustomAbil)} {ENABLE}   → カスタム特性を有効にする\n", Cyan).WriteLine();
                        ($"・{nameof(Options.CPUDelay)} オプション\n  CPUの待ち時間が有効かどうかを設定します。\n  有効にすると、CPUの行動時間が一定になります。\n"
                        + $"  パラメーター \"{ENABLE}\" を選択すると有効に、\"{DISABLE}\" を選択すると無効になります。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.CPUDelay)} {ENABLE}   → CPUの待ち時間を有効にする\n", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        break;
                    }
                case "k":
                    {
                        ("・キーワードの省略法について\n", Yellow).WriteLine();
                        ("一部のキーワードについては、省略して入力することが可能です。\n"
                            + "具体的な一覧は以下の通りです。\n", Yellow).WriteLine();
                        ($"{CHANGE}: ch     {OPTION}: op     {SHOW}: sh     {RESET}: rs     {EXIT}: ex\n\n"
                       + $"{PLAYER1}: p1    {PLAYER2}: p2    {ENABLE}: e    {DISABLE}: d\n\n"
                       + $"{nameof(Options.SetMaxHP)}: smh   {nameof(Options.InfiniteSeed)}: is    {nameof(Options.InfiniteCure)}: ic    {nameof(Options.AbilChange)}: ac\n\n"
                       + $"{nameof(Options.SetAbilCount)}: sac  {nameof(Options.SetMaxCureCount)}: smc   {nameof(Options.SetMaxFoodCount)}: smf\n\n"
                       + $"{nameof(Options.SetSeedDmg)}: ssd   {nameof(Options.SetMaxSeedTurn)}: sms   {nameof(Options.SetCritDmgMultiplier)}: scd\n\n"
                       + $"{nameof(Options.SetInsBufQty)}: sib  {nameof(Options.SetMode)}: sm  {nameof(Options.Strict)}: s   {nameof(Options.Infer)}: i\n\n"
                       + $"{nameof(Options.CustomAbil)}: ca    {nameof(Options.CPUDelay)}: cd   {nameof(Options.SetHP)}: sh  {nameof(Options.SetLuck)}: sl\n\n"
                       + $"Default: d   Classic; c   AgeOfSeed: s\n\n"
                       + $"{STATUS}: s     {OPTIONS}: o    {LOG}: l    {INFO}: i\n\n\n  ", Cyan).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        break;
                    }
                case "m":
                    {
                        ("・モードの設定方法について\n", Yellow).WriteLine();
                        ("モードとは、最大体力や医療制限の設定、先手後手の設定、やどりぎの設定など、\n"
                      + "複数のオプションを一括して管理する仕組みを表します。\n\n"
                      + $"モードの設定・変更は、起動直後のモード設定、及び {OPTION} コマンドの {nameof(Options.SetMode)} パラメーターを\n"
                      + "使用することで行うことができます。\n\n"
                      + "設定可能なモードは以下の通りです。\n", Yellow).WriteLine();
                        ("・ランダムマッチ系のモード\n", Yellow).WriteLine();
                        ("ランダムマッチで使用可能・及び使用可能だったルールを再現したモードです。", Yellow).WriteLine();
                        ("それぞれ、固有のシンボルを用いて参照します。\n", Yellow).WriteLine();
                        ($"・Default モード\n  現環境のモード。体力上限６０、とくせい変更３回、医療５回、やどりぎ４ターン。", Yellow).WriteLine();
                        ("  参照名: \"Default\", \"d\" など\n", Yellow).WriteLine();
                        ($"・Classic モード\n  旧環境のモード。体力上限５０、とくせい変更ナシ、医療・やどりぎ無限。", Yellow).WriteLine();
                        ("  参照名: \"Classic\", \"c\" など\n", Yellow).WriteLine();
                        ($"・AgeOfSeed モード\n  やどりぎ環境のモード。体力上限６０、とくせい変更３回、医療５回、やどりぎ無限。", Yellow).WriteLine();
                        ("  参照名: \"AgeOfSeed\", \"s\" など\n", Yellow).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
                        Console.Clear();
                        ("・ストーリー モード\n", Yellow).WriteLine();
                        ("ストーリーに登場するステージの環境を再現したモードです。", Yellow).WriteLine();
                        ("参照する際は、「StoryX」あるいは「sX」(Xはステージ番号)というようにして参照します。\n", Yellow).WriteLine();
                        ($"(例): {OPTION} {nameof(Options.SetMode)} s11\n", Cyan).WriteLine();
                        ("・カスタム モード\n", Yellow).WriteLine();
                        ("起動直後のモード設定でのみ参照可能なモード名です。", Yellow).WriteLine();
                        ("細かい設定を手動で調整することができます。\n", Yellow).WriteLine();
                        ("参照名: \"Custom\", \"cs\"\n", Yellow).WriteLine();
                        ("・先攻指定付きデフォルトモード\n", Yellow).WriteLine();
                        ($"Default モードと同じステージ条件で、どちらのプレイヤーが先攻するかを\n指定することができます。\n", Yellow).WriteLine();
                        ("参照名: \"p1\"(プレイヤー１が先攻する場合), \"p2\"(プレイヤー２が先攻する場合)\n\n", Yellow).WriteLine();
                        ("...続けるには任意のキーを押してください...", White).WriteLine();
                        Console.ReadKey();
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
        Console.ReadKey();
        Console.Clear();
    }
    #endregion

    #region minor methods
    /// <summary>
    /// 辞書ディレクトリを取得します。
    /// </summary>
    /// <returns>辞書のディレクトリパス</returns>
    /// <exception cref="DirectoryNotFoundException"></exception>
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
    /// <summary>
    /// 辞書の情報をインポートします。
    /// </summary>
    static async Task ImportDictionaryAsync()
    {
        var notypeFiles = Directory.GetFiles(NoTypeWordsPath, "*", SearchOption.AllDirectories);
        var typedFiles = Directory.GetFiles(TypedWordsPath, "*", SearchOption.AllDirectories);
        SBDictionary.NoTypeWords = new();
        SBDictionary.TypedWords = new();
        foreach(var file in notypeFiles)
        {
            using var noTypeWordsReader = new StreamReader(file);
            while (!noTypeWordsReader.EndOfStream)
            {
                var line = await noTypeWordsReader.ReadLineAsync() ?? string.Empty;
                SBDictionary.NoTypeWords.Add(line);
            }
        }
        foreach (var file in typedFiles)
        {
            using var typedWordsReader = new StreamReader(file);
            while (!typedWordsReader.EndOfStream)
            {
                var line = await typedWordsReader.ReadLineAsync() ?? string.Empty;
                var statedLine = line.Trim().Split();
                if (statedLine.Length == 2)
                    SBDictionary.TypedWords.TryAdd(statedLine[0], new() { statedLine[1].StringToType() });
                else if (statedLine.Length == 3)
                    SBDictionary.TypedWords.TryAdd(statedLine[0], new() { statedLine[1].StringToType(), statedLine[2].StringToType() });
            }
        }
    }
    /// <summary>
    /// 初期設定を行います。
    /// </summary>
    /// <returns>プレイヤーの初期情報</returns>
    static (Player, Player) SetUp()
    {
        Console.Clear();
        ("しりとりバトルシミュレーターへようこそ。", Yellow).WriteLine();
        ($"モードの名前を入力してください。(デフォルトでは Default モードになります)", Yellow).WriteLine();
        var modeOrder = Console.ReadLine();
        if (modeOrder?.ToUpper() is "CUSTOM" or "CS") return SetUpCustomMode();
        var (mode, modeName) = ModeFactory.Create(modeOrder);
        if (mode is null) (mode, modeName) = (new(), "Default");
        Mode = mode;
        ($"モードを {modeName} に設定しました。", Green).WriteLine();
        if (mode.IsStoryMode) return SetUpStoryMode();
        return SetUpMatchMode();
    }
    #region setups
    static (Player, Player) SetUpMatchMode()
    {
        Player? p1, p2;
        var NGNamesList = new[] { "p1", "p2", "Player1", "Player2" };
        string? p1Name, p2Name;
        string? p1Type, p2Type;
        bool isP1Human, isP2Human;
        const string DEFAULT_P1_NAME = "じぶん";
        const string DEFAULT_P2_NAME = "あいて";
        ("プレイヤーの種類を入力してください。(人間 → hキー、コンピューター → cキーを入力)", Yellow).WriteLine();
        p1Type = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(p1Type) || p1Type.ToUpper()[0] != 'C')
        {
            ("プレイヤーの種類を Human に設定しました。", Green).WriteLine();
            isP1Human = true;
        }
        else
        {
            ("プレイヤーの種類を CPU に設定しました。", Green).WriteLine();
            isP1Human = false;
        }
        if (isP1Human)
            ($"プレイヤーの名前を入力してください。(デフォルトでは「{DEFAULT_P1_NAME}」です)", Yellow).WriteLine();
        else
            ($"CPUの名前を入力してください。（デフォルトでは「つよし」です）", Yellow).WriteLine();
        p1Name = Console.ReadLine();
        if (string.IsNullOrEmpty(p1Name))
        {
            if (isP1Human) p1Name = DEFAULT_P1_NAME;
            else p1Name = "つよし";
        }
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
        ("相手の種類を入力してください。(人間 → hキー、コンピューター → cキーを入力)", Yellow).WriteLine();
        p2Type = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(p2Type) || p2Type.ToUpper()[0] != 'C')
        {
            ("相手の種類を Human に設定しました。", Green).WriteLine();
            isP2Human = true;
        }
        else
        {
            ("相手の種類を CPU に設定しました。", Green).WriteLine();
            isP2Human = false;
        }
        if (isP2Human)
            ($"相手の名前を入力してください。(デフォルトでは「{DEFAULT_P2_NAME}」です)", Yellow).WriteLine();
        else
            ($"CPUの名前を入力してください。（デフォルトでは「つよし」です）", Yellow).WriteLine();
        p2Name = Console.ReadLine();
        if (string.IsNullOrEmpty(p2Name))
        {
            if (isP2Human) p2Name = DEFAULT_P2_NAME;
            else p2Name = "つよし";
        }
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
        Ability? p1Abil, p2Abil;
        CPUPlayer p1CPU = new Tsuyoshi();
        CPUPlayer p2CPU = new Tsuyoshi();
        if (isP1Human)
        {
            ("プレイヤーの初期特性を入力してください。(デフォルトではデバッガーになります)", Yellow).WriteLine();
            p1Abil = AbilityManager.Create(Console.ReadLine() ?? "N", true) ?? new Debugger();
            ($"{p1Name} の初期特性を {p1Abil.ToString()} に設定しました。", Green).WriteLine();
        }
        else
        {
            p1CPU = CPUFactory.Create(p1Name) ?? new Tsuyoshi(p1Name, new Kakumei());
            p1Abil = p1CPU.Ability;
        }
        if (isP2Human)
        {
            ("相手の初期特性を入力してください。(デフォルトではデバッガーになります)", Yellow).WriteLine();
            p2Abil = AbilityManager.Create(Console.ReadLine() ?? "N", true) ?? new Debugger();
            ($"{p2Name} の初期特性を {p2Abil.ToString()} に設定しました。", Green).WriteLine();
        }
        else
        {
            p2CPU = CPUFactory.Create(p2Name) ?? new Tsuyoshi(p2Name, new Kakumei());
            p2Abil = p2CPU.Ability;
        }
        ("続けるには任意のキーを入力してください. . . ", White).WriteLine();
        Console.ReadKey();
        Console.Clear();
        ("以下の設定で開始します。", Yellow).WriteLine();
        Console.WriteLine();
        var p1NameResult = isP1Human ? p1Name : p1CPU.Name;
        var p2NameResult = isP2Human ? p2Name : p2CPU.Name;
        ($"プレイヤーの名前: {p1NameResult}, プレイヤーの初期特性: {p1Abil.ToString()}", Cyan).WriteLine();
        ($"相手の名前: {p2NameResult}, 相手の初期特性: {p2Abil.ToString()}", Cyan).WriteLine();
        Console.WriteLine();
        ("開始するには任意のキーを入力してください. . . ", Yellow).WriteLine();
        Console.ReadKey();
        p1 = isP1Human ? new Player(p1Name, p1Abil) : p1CPU;
        p2 = isP2Human ? new Player(p2Name, p2Abil) : p2CPU;
        Console.Clear();
        return (p1, p2);
    }
    static (Player, Player) SetUpStoryMode()
    {
        Player? p1, p2;
        var NGNamesList = new[] { "p1", "p2", "Player1", "Player2" };
        string? p1Name, p2Name;
        const string DEFAULT_P1_NAME = "じぶん";
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
        p2Name = Mode.StoryName;
        ($"プレイヤーの名前を {p1Name} に設定しました。", Green).WriteLine();
        Ability? p1Abil, p2Abil;
        CPUPlayer? p2CPU;
        ("プレイヤーの初期特性を入力してください。(デフォルトではデバッガーになります)", Yellow).WriteLine();
        p1Abil = AbilityManager.Create(Console.ReadLine() ?? "N", true) ?? new Debugger();
        ($"{p1Name} の初期特性を {p1Abil.ToString()} に設定しました。", Green).WriteLine();
        p2CPU = CPUFactory.Create(p2Name) ?? new Tsuyoshi(p2Name, new Kakumei());
        p2Abil = p2CPU.FirstAbility;
        ("以下の設定で開始します。", Yellow).WriteLine();
        Console.WriteLine();
        ($"プレイヤーの名前: {p1Name}, プレイヤーの初期特性: {p1Abil.ToString()}", Cyan).WriteLine();
        ($"相手の名前: {p2Name}, 相手の初期特性: {p2Abil.ToString()}", Cyan).WriteLine();
        Console.WriteLine();
        ("開始するには任意のキーを入力してください. . . ", Yellow).WriteLine();
        Console.ReadKey();
        p1 = new Player(p1Name, p1Abil);
        p2 = p2CPU;
        Console.Clear();
        return (p1, p2);
    }
    static (Player, Player) SetUpCustomMode()
    {
        ($"モードを カスタム に設定しました。", Green).WriteLine();
        Player? p1, p2;
        var NGNamesList = new[] { "p1", "p2", "Player1", "Player2" };
        string? p1Name, p2Name;
        string? p1Type, p2Type;
        bool isP1Human, isP2Human;
        const string DEFAULT_P1_NAME = "じぶん";
        const string DEFAULT_P2_NAME = "あいて";
        ("プレイヤーの種類を入力してください。(人間 → hキー、コンピューター → cキーを入力)", Yellow).WriteLine();
        p1Type = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(p1Type) || p1Type.ToUpper()[0] != 'C')
        {
            ("プレイヤーの種類を Human に設定しました。", Green).WriteLine();
            isP1Human = true;
        }
        else
        {
            ("プレイヤーの種類を CPU に設定しました。", Green).WriteLine();
            isP1Human = false;
        }
        if (isP1Human)
            ($"プレイヤーの名前を入力してください。(デフォルトでは「{DEFAULT_P1_NAME}」です)", Yellow).WriteLine();
        else
            ($"CPUの名前を入力してください。（デフォルトでは「つよし」です）", Yellow).WriteLine();
        p1Name = Console.ReadLine();
        if (string.IsNullOrEmpty(p1Name))
        {
            if (isP1Human) p1Name = DEFAULT_P1_NAME;
            else p1Name = "つよし";
        }
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
        ("相手の種類を入力してください。(人間 → hキー、コンピューター → cキーを入力)", Yellow).WriteLine();
        p2Type = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(p2Type) || p2Type.ToUpper()[0] != 'C')
        {
            ("相手の種類を Human に設定しました。", Green).WriteLine();
            isP2Human = true;
        }
        else
        {
            ("相手の種類を CPU に設定しました。", Green).WriteLine();
            isP2Human = false;
        }
        if (isP2Human)
            ($"相手の名前を入力してください。(デフォルトでは「{DEFAULT_P2_NAME}」です)", Yellow).WriteLine();
        else
            ($"CPUの名前を入力してください。（デフォルトでは「つよし」です）", Yellow).WriteLine();
        p2Name = Console.ReadLine();
        if (string.IsNullOrEmpty(p2Name))
        {
            if (isP2Human) p2Name = DEFAULT_P2_NAME;
            else p2Name = "つよし";
        }
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
        Ability? p1Abil, p2Abil;
        CPUPlayer p1CPU = new Tsuyoshi();
        CPUPlayer p2CPU = new Tsuyoshi();
        if (isP1Human)
        {
            ("プレイヤーの初期特性を入力してください。(デフォルトではデバッガーになります)", Yellow).WriteLine();
            p1Abil = AbilityManager.Create(Console.ReadLine() ?? "N", true) ?? new Debugger();
            ($"{p1Name} の初期特性を {p1Abil.ToString()} に設定しました。", Green).WriteLine();
        }
        else
        {
            p1CPU = CPUFactory.Create(p1Name) ?? new Tsuyoshi(p1Name, new Kakumei());
            p1Abil = p1CPU.Ability;
        }
        if (isP2Human)
        {
            ("相手の初期特性を入力してください。(デフォルトではデバッガーになります)", Yellow).WriteLine();
            p2Abil = AbilityManager.Create(Console.ReadLine() ?? "N", true) ?? new Debugger();
            ($"{p2Name} の初期特性を {p2Abil.ToString()} に設定しました。", Green).WriteLine();
        }
        else
        {
            p2CPU = CPUFactory.Create(p2Name) ?? new Tsuyoshi(p2Name, new Kakumei());
            p2Abil = p2CPU.Ability;
        }
        ("続けるには任意のキーを入力してください. . . ", White).WriteLine();
        Console.ReadKey();
        Console.Clear();
        TurnProceedingArbiter p1TPA, p2TPA;
        ("プレイヤーが先攻するかどうかを決定してください。(先攻 → t キー、後攻 → f キー、ランダム → r キーを入力)", Yellow).WriteLine();
        var tpaOrder = Console.ReadLine();
        (p1TPA, p2TPA) = tpaOrder is "t" ? (TurnProceedingArbiter.True, TurnProceedingArbiter.False)
            : tpaOrder is "f" ? (TurnProceedingArbiter.False, TurnProceedingArbiter.True)
            : (TurnProceedingArbiter.Random, TurnProceedingArbiter.Random);
        var tpaString = p1TPA switch
        {
            TurnProceedingArbiter.Random => "ランダム",
            TurnProceedingArbiter.True => "先攻",
            TurnProceedingArbiter.False => "後攻",
            _ => "天で話にならねぇよ..."
        };
        ($"{p1Name}の先攻/後攻設定を「{tpaString}」に設定しました。", Green).WriteLine();
        int p1HP, p2HP;
        ("プレイヤーの最大HPを入力してください。(デフォルトでは60です)", Yellow).WriteLine();
        p1HP = int.TryParse(Console.ReadLine(), out p1HP) ? p1HP : 60;
        ($"{p1Name} の最大HPを {p1HP} に設定しました。", Green).WriteLine();
        ("相手の最大HPを入力してください。(デフォルトでは60です)", Yellow).WriteLine();
        p2HP = int.TryParse(Console.ReadLine(), out p2HP) ? p2HP : 60;
        ($"{p2Name} の最大HPを {p2HP} に設定しました。", Green).WriteLine();
        ("「やどりぎの永続」設定を有効にする場合は e キーを入力してください。", Yellow).WriteLine();
        var isSeedInfinite = Console.ReadLine() is "e";
        ("「やどりぎの永続」設定を" + (isSeedInfinite ? "有効" : "無効") + "に設定しました。", Green).WriteLine();
        ("「医療の使用回数無限」設定を有効にする場合は e キーを入力してください。", Yellow).WriteLine();
        var isCureInfinite = Console.ReadLine() is "e";
        ("「医療の使用回数無限」設定を" + (isCureInfinite ? "有効" : "無効") + "に設定しました。", Green).WriteLine();
        ("「変更可能な特性」設定を有効にする場合は e キーを入力してください。", Yellow).WriteLine();
        var isAbilChangeable = Console.ReadLine() is "e";
        ("「変更可能な特性」設定を" + (isAbilChangeable ? "有効" : "無効") + "に設定しました。", Green).WriteLine();
        ("続けるには任意のキーを入力してください. . . ", White).WriteLine();
        Console.ReadKey();
        Console.Clear();
        ("この設定でよろしいですか？", Yellow).WriteLine();
        Console.WriteLine();
        var p1NameResult = isP1Human ? p1Name : p1CPU.Name;
        var p2NameResult = isP2Human ? p2Name : p2CPU.Name;
        ($"プレイヤーの名前: {p1NameResult}, プレイヤーの初期特性: {p1Abil.ToString()}, プレイヤーの最大HP: {p1HP}", Cyan).WriteLine();
        ($"相手の名前: {p2NameResult}, 相手の初期特性: {p2Abil.ToString()}, 相手の最大HP: {p2HP}", Cyan).WriteLine();
        ("\nやどりぎの永続: " + (isSeedInfinite ? "有効" : "無効"), Cyan).WriteLine();
        ("医療の使用回数無限: " + (isCureInfinite ? "有効" : "無効"), Cyan).WriteLine();
        ("変更可能な特性: " + (isAbilChangeable ? "有効" : "無効\n\n"), Cyan).WriteLine();
        ("続けるには任意のキーを入力してください. . . ", White).WriteLine();
        Console.ReadKey();
        Console.Clear();
        Console.WriteLine();
        ("OK！ → 任意のキーを入力", Yellow).WriteLine();
        p1 = isP1Human ? new Player(p1Name, p1Abil) : p1CPU;
        p2 = isP2Human ? new Player(p2Name, p2Abil) : p2CPU;
        Mode = new Mode(p1TPA, p2TPA, p1HP, p2HP, isSeedInfinite, isCureInfinite, isAbilChangeable, 3, 5, 6, 5, 4, 1.5, 3);
        Console.Clear();
        return (p1, p2);
    }
    #endregion
    /// <summary>
    /// <see cref="Battle.OnReset"/>に渡すハンドラーです。
    /// </summary>
    static void Reset(CancellationTokenSource cts)
    {
        Window.Message.Append("やり直す？", Yellow);
        Window.Message.Append(string.Empty);
        Window.Message.Append("やり直す！ → r キーを入力", Yellow);
        Window.Message.Append("ログを表示 → l キーを入力", Yellow);
        Window.Message.Append("もうやめる！ → q キーを入力", Yellow);
        while (true)
        {
            Refresh();
            Window.WriteLine();
            var order = Console.ReadLine() ?? string.Empty;
            if (order == "q")
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
                Window.Message.Clear();
                Window.Message.Log.Clear();
                Main();
                break;
            }
            else if (order == "r")
            {
                cts.Cancel();
                Console.Clear();
                Window.Message.Clear();
                Window.Message.Log.Clear();
                Main();
                break;
            }
        }
    }
    /// <summary>
    /// <see cref="Battle.Out"/>に渡すハンドラーです。
    /// </summary>
    /// <param name="msgs">出力するアノテーション付き文字列のリスト</param>
    static void Output(List<AnnotatedString> msgs)
    {
        foreach (var msg in msgs)
        {
            if (msg.IsLog)
                Window.Message.Log.Append((ColoredString)msg);
            else
            {
                Window.Message.Append((ColoredString)msg);
                Window.Message.Log.Append((ColoredString)msg);
            }
        }
        Refresh();
        Window.WriteLine();
    }
    /// <summary>
    /// <see cref="SBSimulator.Window"/>を初期化します。
    /// </summary>
    static void Refresh()
    {
        Window.StatusFieldPlayer1 = $"{Battle.Player1.Name}: {Battle.Player1.HP}/{Battle.Player1.MaxHP}";
        Window.StatusFieldPlayer2 = $"{Battle.Player2.Name}: {Battle.Player2.HP}/{Battle.Player2.MaxHP}";
        Window.WordFieldPlayer1 = $"{Battle.Player1.Name}:\n       {Battle.Player1.CurrentWord}";
        Window.WordFieldPlayer2 = $"{Battle.Player2.Name}:\n       {Battle.Player2.CurrentWord}";
    }
    /// <summary>
    /// 警告を表示します。
    /// </summary>
    /// <param name="s">警告のメッセージ</param>
    static void Warn(string s = "入力が不正です")
    {
        Window.Message.Append(s, Red);
    }
    /// <summary>
    /// プレイヤーのステータスを表示します。
    /// </summary>
    static void ShowStatus()
    {
        Console.Clear();
        ("\n" + Battle.Player1.GetStatusString() + Battle.Player2.GetStatusString() + $"\n現在のターン: {Battle.CurrentPlayer.Name}\n経過したターン数: {Battle.TurnNum}\n\n\n\n", Yellow).WriteLine();
        ("終了するには、任意のキーを押してください. . . ", White).WriteLine();
        Console.ReadKey();
        Console.Clear();
    }
    /// <summary>
    /// オプションの状態を表示します。
    /// </summary>
    static void ShowOptions()
    {
        Console.Clear();
        ($"\n{nameof(Options.InfiniteSeed)}:      {Battle.IsSeedInfinite}                     CustomAbilities:   {Battle.IsCustomAbilUsable}\n\n"
         + $"{nameof(Options.InfiniteCure)}:      {Battle.IsCureInfinite}                     CPUDelay:          {Battle.IsCPUDelayEnabled}\n\n"
         + $"Strict Mode:       {Battle.IsStrict}\n\n"
         + $"Type Inference:    {Battle.IsInferable}\n\n"
         + $"MaxCureCount:      {Player.MaxCureCount}回\n\n"
         + $"MaxFoodCount:      {Player.MaxFoodCount}回\n\n"
         + $"SeedDamage:        {Player.SeedDmg}\n\n"
         + $"MaxSeedTurn:       {Player.MaxSeedTurn}\n\n"
         + $"InsBufQuantity:    {Player.InsBufQty}段階\n\n"
         + $"CritDamageMultiplier:      {Player.CritDmg}倍\n\n"
         + $"ChangeableAbility:         {Battle.IsAbilChangeable}\n\n"
         + $"ChangeableAbilityCount:    {Player.MaxAbilChange}回\n\n", Yellow).WriteLine();
        ("終了するには、任意のキーを押してください. . . ", White).WriteLine();
        Console.ReadKey();
        Console.Clear();
    }
    /// <summary>
    /// アプリケーションの情報を表示します。
    /// </summary>
    static void ShowInfo()
    {
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
        Console.ReadKey();
        Console.Clear();
    }
    /// <summary>
    /// ログを表示します。
    /// </summary>
    static void ShowLog()
    {
        Console.Clear();
        var height = Console.WindowHeight - 13;
        if (Window.Message.Log.Content.Count > height)
        {
            var logList = new List<MessageLog>();
            var numOfPages = Window.Message.Log.Content.Count / height + 1;
            for (var i = 0; i < numOfPages; i++)
            {
                var page = new MessageLog();
                page.AppendMany(Window.Message.Log.Content.Skip(i * height).Take(height));
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
                    Window.Message.Clear();
                    Window.Message.Log.Clear();
                    Console.Clear();
                    Console.WriteLine("ログを消去しました。任意のキーを押してください. . .");
                    Console.ReadKey();
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
                Window.Message.Log.WriteLine();
                ("\nログを消去するには c キーを押してください", White).WriteLine();
                ("\n終了するには、r キーを押してください", White).WriteLine();
                var order = Console.ReadLine() ?? string.Empty;
                if (order == "r")
                    break;
                else if (order == "c")
                {
                    Window.Message.Clear();
                    Window.Message.Log.Clear();
                    Console.Clear();
                    Console.WriteLine("ログを消去しました。任意のキーを押してください. . .");
                    Console.ReadKey();
                    break;
                }
            }
        }
        Console.Clear();
    }
    /// <summary>
    /// アプリケーションを終了時の処理を行います。
    /// </summary>
    static void OnExitApp()
    {
        Console.Clear();
        ("アプリケーションを終了します。任意のキーを押してください. . .", Yellow).WriteLine();
        Console.ReadKey();
    }
    #endregion

    // WARNING: デバッグ用　使うな！
    #region DEBUG
    /// <summary>
    /// 拡張無属性辞書に単語を追加します。
    /// </summary>
    static void OnAddOrdered(Order order, CancellationTokenSource cts)
    {
        Warn("__add コマンドは現在無効です。代わりにタイプ指定子をご利用ください。");
        /*
        if (order.ErrorMessage is not null)
        {
            Warn(order.ErrorMessage);
            return;
        }
        if (SBDictionary.NoTypeWords.Contains(order.Body) || SBDictionary.NoTypeWordEx.Contains(order.Body) || SBDictionary.TypedWords.ContainsKey(order.Body))
        {
            Warn($"単語「{order.Body}」は既に辞書に含まれています。");
            return;
        }
        var kanaCheck = new Regex(@"^[\u3040-\u309Fー]+$");
        if (!kanaCheck.IsMatch(order.Body))
        {
            Warn("ひらがな以外を含む入力は無効です。");
            return;
        }
        using var file = new StreamWriter(NoTypeWordExPath, true, Encoding.UTF8);
        SBDictionary.NoTypeWordEx.Add(order.Body);
        file.WriteLine(order.Body);
        Window.Message.Append($"単語「{order.Body}」を辞書に追加しました。", Yellow);
        */
    }
    /// <summary>
    /// 拡張無属性辞書から単語を削除します。
    /// </summary>
    /// <param name="order"></param>
    /// <param name="cts"></param>
    static void OnRemoveOrdered(Order order, CancellationTokenSource cts)
    {
        Warn("__remove コマンドは現在無効です。");
        /*
        if (order.ErrorMessage is not null)
        {
            Warn(order.ErrorMessage);
            return;
        }
        if (!SBDictionary.NoTypeWordEx.Contains(order.Body))
        {
            Warn($"単語「{order.Body}」は拡張辞書に含まれていません。");
            return;
        }
        using var file = new StreamWriter(NoTypeWordExPath, false, Encoding.UTF8);
        SBDictionary.NoTypeWordEx.Remove(order.Body);
        foreach (var i in SBDictionary.NoTypeWordEx)
            file.WriteLine(i);
        Window.Message.Append($"単語「{order.Body}」を辞書から削除しました。", Yellow);
        */
    }
    /// <summary>
    /// ワードサーチモードを起動します。
    /// </summary>
    /// <param name="order"></param>
    /// <param name="cts"></param>
    static void OnSearchOrdered(Order order, CancellationTokenSource cts)
    {
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
            var orderSearchLog = Console.ReadLine()?.Trim().Split() ?? Array.Empty<string>();
            if (orderSearchLog.Length < 1 || string.IsNullOrWhiteSpace(orderSearchLog[0]))
                continue;
            if (orderSearchLog[0] == "-q")
            {
                Console.Clear();
                Console.WriteLine("ワードサーチモードを終了します。\n\n任意のキーを押してください. . . ");
                Console.ReadKey();
                break;
            }
            if (orderSearchLog[0] == "-m")
            {
                ShowWordSearchManual();
                continue;
            }
            if (orderSearchLog[0] == "-l")
            {
                Console.Clear();
                var clothList = SBDictionary.TypedWords.Where(x => x.Value.Contains(Word.WordType.Cloth)).Select(x => x.Key).ToList();
                clothList.Sort();
                foreach (var i in clothList) Console.WriteLine($"{i}, {SBDictionary.TypedWords.GetValueOrDefault(i)?.ElementAtOrDefault(0).TypeToString() ?? string.Empty} {SBDictionary.TypedWords.GetValueOrDefault(i)?.ElementAtOrDefault(1).TypeToString() ?? string.Empty}");
                Console.ReadLine();
                continue;
            }
            var cond = orderSearchLog[0];
            int searchOption = 0;
            int dicOption = 0;
            if (orderSearchLog.Length > 1 && !TryStringToSearchOption(orderSearchLog[1], out searchOption))
            {
                searchMsg.Append($"サーチオプション{orderSearchLog[1]}が見つかりませんでした。", Red);
                Console.Clear();
                continue;
            }
            if (orderSearchLog.Length > 2 && !TryStringToDicOption(orderSearchLog[2], out dicOption))
            {
                searchMsg.Append($"辞書オプション{orderSearchLog[2]}が見つかりませんでした。", Red);
                Console.Clear();
                continue;
            }
            Console.Clear();
            ("ワードサーチモード\n", White).WriteLine();
            ("\n検索条件: ", Yellow).Write();
            (string.Join(" ", orderSearchLog), Cyan).WriteLine();
            ("\nマッチする単語を探しています. . . \n", Yellow).WriteLine();
            var searchFlags = TrySearch(cond, searchOption, dicOption, out var words);
            if (!searchFlags.HasParseSuccessed)
            {
                ("単語を検索できませんでした。\n", Red).WriteLine();
                ("検索文の書式が不正であるか、検索できない文字が含まれています。\n\n", DarkYellow).WriteLine();
                Console.WriteLine("やり直すには任意のキーを押してください。");
                Console.ReadKey();
                continue;
            }
            if (!searchFlags.HasFound)
            {
                ("マッチする単語が見つかりませんでした。\n\n", Red).WriteLine();
                Console.WriteLine("やり直すには任意のキーを押してください。");
                Console.ReadKey();
                continue;
            }
            ($"結果は{words.Count}件見つかりました。\n", Green).WriteLine();
            Console.WriteLine("表示するには任意のキーを押してください. . . ");
            Console.ReadKey();
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
                    foreach (var i in wordBook[index])
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
    }
    /// <summary>
    /// ワードサーチモードの操作方法を表示します。
    /// </summary>
    static void ShowWordSearchManual()
    {
        Console.Clear();
        ("ワードサーチモード\n", White).WriteLine();
        ("・操作説明\n", Yellow).WriteLine();
        ("ワードサーチモードは、原則次のようなコマンドによって操作します。\n", Yellow).WriteLine();
        ("[検索する単語の条件] [検索オプション] [辞書オプション]\n", Green).WriteLine();
        ("辞書オプションを指定する場合、検索オプションを省略することはできません。\n", Yellow).WriteLine();
        ("・検索オプション\n", Yellow).WriteLine();
        ("現在、以下の８種類が使用可能です。いずれも、アルファベット一文字を指定します。\n"
            + "省略した場合は、m オプションとして処理されます。\n", Yellow).WriteLine();
        ("・m オプション  →  指定した文字列と完全一致する単語を検索します。\n"
       + "・c オプション  →  指定した文字列を含んでいる単語を検索します。\n"
       + "・s オプション  →  指定した文字列と頭文字が同じである単語を検索します。\n"
       + "・e オプション  →  指定した文字列と同じ文字で終わる単語を検索します。\n"
       + "・b オプション  →  指定した文字列と最初の文字/最後の文字がともに一致する単語を検索します。\n"
       + "・r オプション  →  正規表現を入力し、それにマッチする単語を検索します。\n"
       + "・7 オプション  →  b オプションと同様の検索方法で、7文字以上のもののみ出力します。\n"
       + "・6 オプション  →  b オプションと同様の検索方法で、6文字以上のもののみ出力します。\n", Cyan).WriteLine();
        ("続けるには、任意のキーを押してください. . . ", White).WriteLine();
        Console.ReadKey();
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
        Console.ReadKey();
    }

    // FIXME: 「ゔ」のサーチがうまくいかない。（「\u3094」でマッチすればうまくいく）
    /// <summary>
    /// 単語の検索を行います。
    /// </summary>
    /// <param name="name">単語の名前を表す<see cref="string"/></param>
    /// <param name="searchOption">検索のオプション</param>
    /// <param name="dicOption">辞書のオプション</param>
    /// <param name="words">検索条件にマッチする文字列のリスト</param>
    /// <returns>検索が成功したかどうかを表すフラグ</returns>
    static (bool HasFound, bool HasParseSuccessed) TrySearch(string name, int searchOption, int dicOption, out List<string> words)
    {
        var result = (false, true);
        words = new();
        Regex r;
        try
        {
            r = new Regex(searchOption switch
            {
                0 => $"^{name}$",
                1 => name,
                2 => $"^{name[0]}",
                3 => $"{name[^1]}ー*$",
                4 => $@"^{name[0]}.*{name[^1]}ー*$",
                5 => name,
                6 => name,
                7 => $@"^{name[0]}.{{5,}}{name[^1]}$|^{name[0]}.{{4,}}{name[^1]}ー$",
                8 => $@"^{name[0]}.{{4,}}{name[^1]}$|^{name[0]}.{{3,}}{name[^1]}ー$",
                _ => name
            });
        }
        catch (RegexParseException)
        {
            return (false, false);
        }
        var dic = dicOption switch
        {
            1 or 2 => SBDictionary.NoTypeWords,
            3 => SBDictionary.TypedWords.Keys.ToList(),
            _ => SBDictionary.NoTypeWords.Concat(SBDictionary.TypedWords.Keys).ToList()
        };
        if (name[0] == name[^1] && searchOption == 4)
            r = new Regex($"^{name[0]}\\w*{name[^1]}ー*$|^{name[0]}ー*$");
        foreach (var i in dic)
        {
            if (r.IsMatch(i))
            {
                result.Item1 = true;
                words.Add(i);
            }
        }
        words = words.Distinct().ToList();
        return result;
    }
    /// <summary>
    /// 文字列を検索オプションに変換します。
    /// </summary>
    /// <param name="str">変換する<see cref="string"/></param>
    /// <param name="option">変換された検索オプションを表す<see cref="int"/></param>
    /// <returns>変換が成功したかどうかを表すフラグ</returns>
    static bool TryStringToSearchOption(string str, out int option)
    {
        (var result, option) = str switch
        {
            "m" => (true, 0),
            "c" => (true, 1),
            "s" => (true, 2),
            "e" => (true, 3),
            "b" => (true, 4),
            "r" => (true, 5),
            "t" => (true, 6),
            "7" => (true, 7),
            "6" => (true, 8),
            _ => (false, 0)
        };
        return result;
    }
    /// <summary>
    /// 文字列を辞書オプションに変換します。
    /// </summary>
    /// <param name="str">変換する<see cref="string"/></param>
    /// <param name="option">変換された辞書オプションを表す<see cref="int"/></param>
    /// <returns>変換が成功したかどうかを表すフラグ</returns>
    static bool TryStringToDicOption(string str, out int option)
    {
        (var result, option) = str switch
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
