using System.Text.RegularExpressions;
using System.Linq;
using Umayadia.Kana;
using static SBSimulator.Source.Word;
using static SBSimulator.Source.SBOptions;

namespace SBSimulator.Source;
// Program クラスで行っている処理を抽出したクラス。
// 文字列で入力し、アノテーション付き文字列で出力する。
/// <summary>
/// バトル単位の処理を管理するクラス。
/// </summary>
internal class Battle
{
    /// <summary>
    /// 一人目のプレイヤー情報
    /// </summary>
    public Player Player1 { get; init; } = new();
    /// <summary>
    /// 二人目のプレイヤー情報
    /// </summary>
    public Player Player2 { get; init; } = new();
    /// <summary>
    /// １ターン前の<see cref="CurrentPlayer"/>の情報
    /// </summary>
    public Player PreActor { get; set; } = new();
    /// <summary>
    /// １ターン前の<see cref="OtherPlayer"/>の情報
    /// </summary>
    public Player PreReceiver { get; set; } = new();
    /// <summary>
    /// <see cref="Player1"/>がターンを持っているかどうかを表すフラグ
    /// </summary>
    public bool IsPlayer1sTurn { get; private set; } = true;
    /// <summary>
    /// 経過したターン数
    /// </summary>
    public int TurnNum { get; private set; } = 1;
    /// <summary>
    /// 使用済みの単語名を保持するリスト
    /// </summary>
    public List<string> UsedWords { get; set; } = new();
    /// <summary>
    /// 入力処理を行うハンドラー
    /// </summary>
    public Func<string[]> In = Array.Empty<string>;
    /// <summary>
    /// 出力処理を行うハンドラー
    /// </summary>
    public Action<List<AnnotatedString>> Out { get; set; } = (_) => { };
    /// <summary>
    /// 出力する情報を保存するバッファー
    /// </summary>
    /// <returns>アノテーション付き文字列のリスト</returns>
    public List<AnnotatedString> Buffer { get; private set; } = new();
    /// <summary>
    /// リセット時の処理を行うハンドラー
    /// </summary>
    public event Action<CancellationTokenSource> OnReset = delegate { };
    /// <summary>
    /// コマンド列とハンドラーを紐づける辞書
    /// </summary>
    Dictionary<string, Action<string[], CancellationTokenSource>> OrderFunctions = new();
    /// <summary>
    /// アクションを行う側のプレイヤー
    /// </summary>
    public Player CurrentPlayer => IsPlayer1sTurn ? Player1 : Player2;
    /// <summary>
    /// アクションを受ける側のプレイヤー
    /// </summary>
    public Player OtherPlayer => IsPlayer1sTurn ? Player2 : Player1;

    public Battle(Player p1, Player p2) => (Player1, Player2) = (p1, p2);
    public Battle() : this(new(), new()) { }

    /// <summary>
    /// インスタンスを実行します。
    /// </summary>
    /// <param name="custom"> <see cref="Initialize(Dictionary{string, Action{string[], CancellationTokenSource}})"/>に渡すハンドラーの情報 </param>
    public void Run(Dictionary<string, Action<string[], CancellationTokenSource>> custom)
    {
        Initialize(custom);
        Out(Buffer);
        Buffer.Clear();
        var cts = new CancellationTokenSource();
        while (!cts.IsCancellationRequested)
        {
            // 入力処理、CPU かどうかを判定
            var order = CurrentPlayer is not CPUPlayer cpu ? In() : cpu.Execute();
            if (order.Length == 0 || string.IsNullOrWhiteSpace(order[0])) continue;

            // 辞書 OrderFunctions からコマンド名に合致するハンドラーを取り出す
            if (OrderFunctions.TryGetValue(order[0], out var func))
                func(order, cts);
            else
                OnDefault(order, cts);

            // 出力処理
            Out(Buffer);
            Buffer.Clear();

        }
    }

    /// <summary>
    /// インスタンスの初期設定を実行します。
    /// </summary>
    /// <param name="custom"> カスタムで追加するハンドラーの情報 </param>
    private void Initialize(Dictionary<string, Action<string[], CancellationTokenSource>> custom)
    {
        SetMode(SBMode.Default);
        Buffer.Add($"{CurrentPlayer.Name} のターンです", Notice.General);
        Buffer.Add($"{Player1.Name}: {Player1.HP}/{Player1.MaxHP},     {Player2.Name}: {Player2.HP}/{Player2.MaxHP}", Notice.LogInfo);
        OrderFunctions = new()
        {
            ["change"] = OnChangeOrdered,
            ["ch"] = OnChangeOrdered,
            ["option"] = OnOptionOrdered,
            ["op"] = OnOptionOrdered,
        };
        OrderFunctions = OrderFunctions.Concat(custom.Where(pair => !OrderFunctions.ContainsKey(pair.Key))).ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    /// <summary>
    /// デフォルトで実行されるハンドラーです。単語の種別に応じて<see cref="Contract"/>を生成し処理します。
    /// </summary>
    public void OnDefault(string[] order, CancellationTokenSource cts)
    {
        Word? word;

        // 単語をタイプ推論し、生成する。

        // タイプ推論が成功したかどうかを表すフラグ。
        bool isInferSuccessed;

        // タイプ推論が有効な場合。推論できない場合は無属性。
        if (IsInferable && order.Length == 1)
        {
            var name = KanaConverter.ToHiragana(order[0]).Replace('ヴ', 'ゔ');
            isInferSuccessed = TryInferWordTypes(name, out Word wordtemp);
            word = wordtemp;
        }
        // タイプ推論が無効な場合。手動で入力されたタイプを参照し、単語に変換する。
        // 単語の書式が不正な場合には、isInferSuccessed を false に設定する。
        else
        {
            var type1 = order.Length > 1 ? order[1][0].CharToType() : WordType.Empty;
            var type2 = order.Length > 1 ? order[1].Length == 2 ? order[1][1].CharToType() : WordType.Empty : WordType.Empty;
            word = new Word(order[0], CurrentPlayer, OtherPlayer, type1, type2);
            isInferSuccessed = word.Name.IsWild() || new Regex("^[ぁ-ゔゟァ-ヴー]*$").IsMatch(word.Name);
        }

        // ContractType の決定。
        var ct = word.IsBuf ? ContractType.Buf
               : word.IsHeal ? ContractType.Heal
               : word.IsSeed ? ContractType.Seed
               : ContractType.Attack;
        var c = Contract.Create(ct, CurrentPlayer, OtherPlayer, word, this, new ContractArgs(PreActor, PreReceiver) { IsInferSuccessed = isInferSuccessed });
        c.Execute();

        // Contract の情報をバッファーに追加する。
        Buffer.Add(ct switch
        {
            ContractType.Attack => $"{CurrentPlayer.Name} は単語 {word} で攻撃した！",
            ContractType.Buf => $"{CurrentPlayer.Name} は単語 {word} で能力を高めた！",
            ContractType.Heal => $"{CurrentPlayer.Name} は単語 {word} を使った！",
            ContractType.Seed => $"{CurrentPlayer.Name} は単語 {word} でやどりぎを植えた！",
            _ => throw new ArgumentException($"ContractType \"{ct}\" has not been implemented.")
        }, Notice.LogActionInfo);

        Buffer.AddMany(c.Message);

        // プレイヤーが死んだかどうかの判定、ターンの交代。
        if (c.DeadFlag) OnReset(cts);
        if (c.IsBodyExecuted) ToggleTurn();
    }

    /// <summary>
    /// とくせいを変更する際に実行されるハンドラーです。
    /// </summary>
    public void OnChangeOrdered(string[] order, CancellationTokenSource cts)
    {
        // 「変更可能な特性」設定の確認
        if (!IsAbilChangeable)
        {
            Buffer.Add("設定「変更可能な特性」がオフになっています。option コマンドから設定を切り替えてください。", Notice.Warn);
            return;
        }

        // パラメータにプレイヤーを指定していない場合の処理
        if (order.Length == 2)
        {
            var nextAbil = AbilityFactory.Create(order[1]);
            if (nextAbil is null)
            {
                Buffer.Add($"入力 {order[1]} に対応するとくせいが見つかりませんでした。", Notice.Warn);
                return;
            }
            if (nextAbil == CurrentPlayer.Ability)
            {
                Buffer.Add("既にそのとくせいになっている！", Notice.Warn);
                return;
            }
            if (CurrentPlayer.TryChangeAbil(nextAbil))
                Buffer.Add($"{CurrentPlayer.Name} はとくせいを {nextAbil.ToString()} に変更しました", Notice.SystemInfo);
            else
                Buffer.Add($"{CurrentPlayer.Name} はもう特性を変えられない！", Notice.Caution);
        }

        // パラメータにプレイヤーを指定している場合の処理
        else if (order.Length == 3)
        {
            Player abilChangingP = new();
            bool player1Flag = order[1] == Player1.Name || order[1] == "Player1" || order[1] == "p1";
            bool player2Flag = order[1] == Player2.Name || order[1] == "Player2" || order[1] == "p2";
            if (!player1Flag && !player2Flag)
            {
                Buffer.Add($"名前 {order[1]} を持つプレイヤーが見つかりませんでした。", Notice.Warn);
                return;
            }
            if (player1Flag)
                abilChangingP = Player1;
            else if (player2Flag)
                abilChangingP = Player2;
            var nextAbil = AbilityFactory.Create(order[2]);
            if (nextAbil is null)
            {
                Buffer.Add($"入力 {order[2]} に対応するとくせいが見つかりませんでした。", Notice.Warn);
                return;
            }
            if (nextAbil == abilChangingP.Ability)
            {
                Buffer.Add("既にそのとくせいになっている！", Notice.Warn);
                return;
            }
            if (abilChangingP.TryChangeAbil(nextAbil))
                Buffer.Add($"{abilChangingP.Name} はとくせいを {nextAbil.ToString()} に変更しました", Notice.SystemInfo);
            else
                Buffer.Add($"{abilChangingP.Name} はもう特性を変えられない！", Notice.Caution);
        }
        else
            Buffer.Add("入力が不正です。", Notice.Warn);
    }

    /// <summary>
    /// オプションを設定する際に実行されるハンドラーです。
    /// </summary>
    public void OnOptionOrdered(string[] order, CancellationTokenSource cts)
    {
        if (order.Length < 2)
        {
            Buffer.Add("入力が不正です", Notice.Warn);
            return;
        }

        // 変更するオプションの決定
        Action<string[]> option = order[1].ToLower() switch
        {
            "setmaxhp" or "smh" => OptionSetMaxHP,
            "infiniteseed" or "is" => OptionInfiniteSeed,
            "infinitecure" or "ic" => OptionInfiniteCure,
            "abilchange" or "ac" => OptionAbilChange,
            "strict" or "s" => OptionStrict,
            "infer" or "i" => OptionInfer,
            "setabilcount" or "sac" => OptionSetAbilCount,
            "setmaxcurecount" or "smcc" or "smc" => OptionSetMaxCureCount,
            "setmaxfoodcount" or "smfc" or "smf" => OptionSetMaxFoodCount,
            "setseeddmg" or "ssd" => OptionSetSeedDmg,
            "setmaxseedturn" or "smst" or "sms" => OptionSetMaxSeedTurn,
            "setcritdmgmultiplier" or "scdm" or "scd" => OptionSetCritDmgMultiplier,
            "setinsbufqty" or "sibq" or "sib" => OptionSetInsBufQty,
            "setmode" or "sm" => OptionSetMode,
            _ => OptionDefault
        };

        // オプションの実行
        option(order);
    }

    // オプションを処理するメソッド群
    #region options
    private void OptionSetMaxHP(string[] order)
    {
        if (order.Length != 4)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!int.TryParse(order[3], out int hp))
        {
            Buffer.Add("HPの書式が不正です。", Notice.Warn);
            return;
        }
        if (!TryStringToPlayer(order[2], out Player p))
        {
            Buffer.Add($"プレイヤー {order[2]} が見つかりませんでした。", Notice.Warn);
            return;
        }
        p.MaxHP = hp;
        p.ModifyMaxHP();
        Buffer.Add($"{p.Name} の最大HPを {p.MaxHP} に設定しました。", Notice.SettingInfo);
    }
    private void OptionInfiniteSeed(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!order[2].TryStringToEnabler(out bool enabler))
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (enabler)
        {
            IsSeedInfinite = true;
            Buffer.Add("やどりぎの継続ターン数を 無限 に変更しました。", Notice.SettingInfo);
            return;
        }
        IsSeedInfinite = false;
        Buffer.Add($"やどりぎの継続ターン数を {Player.MaxSeedTurn} ターン に変更しました。", Notice.SettingInfo);
    }
    private void OptionInfiniteCure(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!order[2].TryStringToEnabler(out bool enabler))
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (enabler)
        {
            IsCureInfinite = true;
            Buffer.Add("医療タイプの単語で回復可能な回数を 無限 に変更しました。", Notice.SettingInfo);
            return;
        }
        IsCureInfinite = false;
        Buffer.Add($"医療タイプの単語で回復可能な回数を {Player.MaxCureCount}回 に変更しました。", Notice.SettingInfo);
    }
    private void OptionAbilChange(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!order[2].TryStringToEnabler(out bool enabler))
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (enabler)
        {
            IsAbilChangeable = true;
            Buffer.Add($"とくせいの変更を有効にしました。(上限 {Player.MaxAbilChange}回 まで)", Notice.SettingInfo);
            return;
        }
        IsAbilChangeable = false;
        Buffer.Add($"とくせいの変更を無効にしました。", Notice.SettingInfo);

    }
    private void OptionStrict(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!order[2].TryStringToEnabler(out bool enabler))
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (enabler)
        {
            IsStrict = true;
            Buffer.Add($"ストリクト モードを有効にしました。", Notice.SettingInfo);
            return;
        }
        IsStrict = false;
        Buffer.Add($"ストリクト モードを無効にしました。", Notice.SettingInfo);
    }
    private void OptionInfer(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!order[2].TryStringToEnabler(out bool enabler))
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (enabler)
        {
            IsInferable = true;
            Buffer.Add($"タイプの推論を有効にしました。", Notice.SettingInfo);
            return;
        }
        IsInferable = false;
        Buffer.Add($"タイプの推論を無効にしました。", Notice.SettingInfo);
    }
    private void OptionSetAbilCount(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!int.TryParse(order[2], out int abilCount) || abilCount < 0)
        {
            Buffer.Add("とくせいの変更回数の入力が不正です。", Notice.Warn);
            return;
        }
        Player.MaxAbilChange = abilCount;
        Buffer.Add($"とくせいの変更回数上限を {Player.MaxAbilChange}回 に設定しました。", Notice.SettingInfo);
    }
    private void OptionSetMaxCureCount(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!int.TryParse(order[2], out int cureCount) || cureCount <= 0)
        {
            Buffer.Add("回復回数の入力が不正です。", Notice.Warn);
            return;
        }
        Player.MaxCureCount = cureCount;
        Buffer.Add($"医療タイプの単語による回復の回数上限を {Player.MaxCureCount}回 に設定しました。", Notice.SettingInfo);
    }
    private void OptionSetMaxFoodCount(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!int.TryParse(order[2], out int foodCount) || foodCount <= 0)
        {
            Buffer.Add("回復回数の入力が不正です。", Notice.Warn);
            return;
        }

        Player.MaxFoodCount = foodCount;
        Buffer.Add($"食べ物タイプの単語による回復の回数上限を {Player.MaxFoodCount}回 に設定しました。", Notice.SettingInfo);
    }
    private void OptionSetSeedDmg(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!int.TryParse(order[2], out int seedDmg) || seedDmg <= 0)
        {
            Buffer.Add("ダメージ値の入力が不正です。", Notice.Warn);
            return;
        }
        Player.SeedDmg = seedDmg;
        Buffer.Add($"やどりぎによるダメージを {Player.SeedDmg} に設定しました。", Notice.SettingInfo);
    }
    private void OptionSetMaxSeedTurn(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!int.TryParse(order[2], out int seedTurn) || seedTurn <= 0)
        {
            Buffer.Add("ターン数の入力が不正です。", Notice.Warn);
            return;
        }
        Player.MaxSeedTurn = seedTurn;
        Buffer.Add($"やどりぎの継続ターン数を {Player.MaxSeedTurn}ターン に設定しました。", Notice.SettingInfo);
    }
    private void OptionSetCritDmgMultiplier(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!double.TryParse(order[2], out double critDmg) || critDmg < 0)
        {
            Buffer.Add("ダメージ値の入力が不正です。", Notice.Warn);
            return;
        }
        Player.CritDmg = critDmg;
        Buffer.Add($"急所によるダメージ倍率を {Player.CritDmg}倍 に設定しました。", Notice.SettingInfo);
    }
    private void OptionSetInsBufQty(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        if (!int.TryParse(order[2], out int insBufQty))
        {
            Buffer.Add("変化値の入力が不正です。", Notice.Warn);
            return;
        }
        Player.InsBufQty = insBufQty;
        Buffer.Add($"ほけん発動による攻撃力の変化を {Player.InsBufQty} 段階 に設定しました。", Notice.SettingInfo);
    }
    private void OptionSetMode(string[] order)
    {
        if (order.Length != 3)
        {
            Buffer.Add("入力が不正です。", Notice.Warn);
            return;
        }
        var mode = order[2].StringToMode();
        if (mode is SBMode.Empty)
        {
            Buffer.Add($"モード {order[2]} が見つかりません。", Notice.Warn);
            return;
        }
        SetMode(mode);
        Buffer.Add($"モードを {mode.ModeToString()} に設定しました。", Notice.SettingInfo);
    }
    private void OptionDefault(string[] order)
    {
        Buffer.Add($"オプション {order[1]} が存在しないか、書式が不正です。", Notice.Warn);
    }
    #endregion

    /// <summary>
    /// 複数のオプションを一括で変更します。
    /// </summary>
    /// <param name="mode">変更先のモード名の指定</param>
    private void SetMode(SBMode mode)
    {
        if (mode is SBMode.Default)
        {
            Player1.MaxHP = 60;
            Player1.ModifyMaxHP();
            Player2.MaxHP = 60;
            Player2.ModifyMaxHP();
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
            Player1.MaxHP = 50;
            Player1.ModifyMaxHP();
            Player2.MaxHP = 50;
            Player2.ModifyMaxHP();
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
            Player1.MaxHP = 60;
            Player1.ModifyMaxHP();
            Player2.MaxHP = 60;
            Player2.ModifyMaxHP();
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

    /// <summary>
    /// 文字列のタイプを推論し、単語を出力します。
    /// </summary>
    /// <param name="name">推論元の文字列</param>
    /// <param name="word">出力される文字</param>
    /// <returns>タイプ推論が成功したかを表すフラグ</returns>
    public bool TryInferWordTypes(string name, out Word word)
    {
        if (name.IsWild())
        {
            word = new Word(name, CurrentPlayer, OtherPlayer, WordType.Empty);
            return true;
        }
        if (Program.TypedWords.TryGetValue(name, out var types))
        {
            var type1 = types[0];
            var type2 = types.Count > 1 ? types[1] : WordType.Empty;
            word = new Word(name, CurrentPlayer, OtherPlayer, type1, type2);
            return true;
        }
        if (Program.NoTypeWords.Contains(name) || Program.NoTypeWordEx.Contains(name))
        {
            word = new Word(name, CurrentPlayer, OtherPlayer, WordType.Empty);
            return true;
        }
        word = new Word();
        return false;
    }

    /// <summary>
    /// ひとつ前のプレイヤー情報を更新し、ターンを交替します。
    /// </summary>
    public void ToggleTurn()
    {
        PreActor = CurrentPlayer.Clone();
        PreReceiver = OtherPlayer.Clone();
        IsPlayer1sTurn = !IsPlayer1sTurn;
        TurnNum++;
        if (TurnNum > 1)
        {
            Buffer.Add($"{CurrentPlayer.Name} のターンです", Notice.General);
            Buffer.Add($"{Player1.Name}: {Player1.HP}/{Player1.MaxHP},     {Player2.Name}: {Player2.HP}/{Player2.MaxHP}", Notice.LogInfo);
        }
    }
    /// <summary>
    /// 文字列からプレイヤーを決定します。
    /// </summary>
    /// <param name="s">推論元の文字列</param>
    /// <param name="p">指定されたプレイヤー</param>
    /// <returns>推論が成功したかどうかを表すフラグ</returns>
    private bool TryStringToPlayer(string s, out Player p)
    {
        if (s == Player1.Name || s.ToLower() is "player1" or "p1")
        {
            p = Player1;
            return true;
        }
        if (s == Player2.Name || s.ToLower() is "player2" or "p2")
        {
            p = Player2;
            return true;
        }
        p = new();
        return false;
    }
}
