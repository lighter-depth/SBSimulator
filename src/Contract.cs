using System.Diagnostics.CodeAnalysis;
using static SBSimulator.Source.Word;
using static System.ConsoleColor;
using static SBSimulator.Source.SBOptions;
using static System.Net.Mime.MediaTypeNames;

namespace SBSimulator.Source;
internal enum ContractType
{
    None, Attack, Buf, Heal, Seed
}
internal abstract class Contract
{
    #region properties
    public required Player Actor { get; init; }
    public required Player Receiver { get; init; }
    public required Word Word { get; init; }
    public ContractArgs Args { get; set; } = ContractArgs.Empty;
    public abstract ContractType Type { get; }
    public AbilityType State { get; protected set; } = AbilityType.None;
    public bool DeadFlag { get; protected set; } = false;
    public bool IsBodyExecuted { get; protected set; } = true;
    public List<ColoredString> Message { get; protected set; } = new();
    protected List<Action> Contents { get; set; } = new();
    #endregion

    #region constructors
    [SetsRequiredMembers]
    public Contract(Player actor, Player receiver, Word word) : this(actor, receiver, word, ContractArgs.Empty) { }
    [SetsRequiredMembers]
    public Contract(Player actor, Player receiver, Word word, ContractArgs args)
    {
        Actor = actor;
        Receiver = receiver;
        Word = word;
        Args = args;
        Contents = new()
        {
            OnContractBegin,
            OnActionBegin,
            OnActionExecuted,
            OnActionEnd,
            OnReceive,
            OnContractEnd
        };
    }
    [SetsRequiredMembers] public Contract() : this(new(), new(), new()) { }
    #endregion

    #region methods
    public void Execute()
    {
        var usedCheck = OnWordUsedCheck();
        var inferCheck = OnWordInferCheck();
        if (!usedCheck || !inferCheck)
        {
            IsBodyExecuted = false;
            return;
        }
        foreach (var action in Contents)
        {
            action();
        }
    }
    public virtual bool OnWordUsedCheck()
    {
        State = AbilityType.WordUsedChecked;
        if (IsStrict && Args.IsInferSuccessed)
        {
            var strictFlag = Word.IsSuitable(Receiver.CurrentWord);
            Args.IsWordNotUsed = !Program.UsedWords.Contains(Word.Name);
            if (strictFlag > 0)
            {
                Message.Add("開始文字がマッチしていません。", Red);
                return false;
            }
            if (strictFlag < 0)
            {
                Message.Add("「ん」で終わっています", Red);
                return false;
            }
            if (Actor.Ability.Type.HasFlag(AbilityType.WordUsedChecked))
            {
                Actor.Ability.Execute(this);
            }
            if (!Args.IsWordNotUsed)
            {
                Message.Add("すでに使われた単語です", Red);
                return false;
            }
        }
        return true;
    }
    public virtual bool OnWordInferCheck()
    {
        State = AbilityType.WordInferChecked;
        if (Actor.Ability.Type.HasFlag(State))
        {
            Actor.Ability.Execute(this);
            return true;
        }
        if (IsInferable)
        {
            if (!Args.IsInferSuccessed)
            {
                Message.Add("辞書にない単語です。", Red);
                return false;
            }
        }
        return true;
    }
    public virtual void OnContractBegin()
    {
        State = AbilityType.ContractBegin;
        Actor.CurrentWord = Word;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public virtual void OnActionBegin()
    {
        State = AbilityType.ActionBegin;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public virtual void OnActionExecuted()
    {
        State = AbilityType.ActionExecuted;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public virtual void OnActionEnd()
    {
        State = AbilityType.ActionEnd;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public virtual void OnReceive()
    {
        State = AbilityType.Received;
        if (Receiver.Ability.Type.HasFlag(State))
            Receiver.Ability.Execute(this);
    }
    public virtual void OnContractEnd()
    {
        State = AbilityType.ContractEnd;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
        if (Receiver.State.HasFlag(Player.PlayerState.Seed))
        {
            Message.Add($"{Actor.Name} はやどりぎで体力を奪った！", DarkGreen);
            Receiver.TakeSeedDmg(Actor);
        }
        if (Receiver.State.HasFlag(Player.PlayerState.Poison))
        {
            Message.Add($"{Receiver.Name} は毒のダメージをうけた！", DarkGreen);
            Receiver.TakePoisonDmg();
        }
        if (Receiver.HP <= 0)
        {
            Receiver.Kill();
            Message.Add($"{Actor.Name} は {Receiver.Name} を倒した！", DarkGreen);
            DeadFlag = true;
            return;
        }
        DeadFlag = false;
        if (!Word.Name.IsWild()) Program.UsedWords.Add(Word.Name);
    }
    public static Contract Create(ContractType c, Player actor, Player receiver, Word word, ContractArgs args)
    {
        return c switch
        {
            ContractType.Attack => new AttackContract(actor, receiver, word, args),
            ContractType.Buf => new BufContract(actor, receiver, word, args),
            ContractType.Heal => new HealContract(actor, receiver, word, args),
            ContractType.Seed => new SeedContract(actor, receiver, word, args),
            _ => throw new ArgumentException($"ContractType \"{c}\" is not implemented.")
        };
    }
    #endregion
}
internal class ContractArgs
{
    public bool IsInferSuccessed { get; set; }
    public bool IsWordNotUsed { get; set; }
    public Player PreActor { get; set; }
    public Player PreReceiver { get; set; }
    public ContractArgs(Player pa, Player pr)
    {
        IsInferSuccessed = false;
        IsWordNotUsed = false;
        PreActor = pa;
        PreReceiver = pr;
    }
    public static ContractArgs Empty => new(new(), new());
}
internal class AttackContract : Contract
{
    public override ContractType Type => ContractType.Attack;
    public int BaseDmg { get; internal set; }
    public double PropDmg { get; internal set; } = 1;
    public double MtpDmg { get; internal set; } = 1;
    public double AmpDmg { get; internal set; } = 1;
    public int BrdDmg { get; internal set; } = 1;
    public bool CritFlag { get; internal set; } = false;
    public bool PoisonFlag { get; internal set; } = false;
    public double Damage { get; internal set; }

    #region constructors
    [SetsRequiredMembers]
    public AttackContract(Player actor, Player receiver, Word word, ContractArgs args) : base(actor, receiver, word, args)
    {
        Contents = new()
        {
            OnContractBegin,
            OnBaseCalc,
            OnPropCalc,
            OnAmpCalc,
            OnBrdCalc,
            OnCritCalc,
            OnMtpCalc,
            OnActionBegin,
            OnActionExecuted,
            OnViolenceUsed,
            OnActionEnd,
            OnReceive,
            OnContractEnd
        };
    }
    [SetsRequiredMembers]
    public AttackContract() : base() { }
    #endregion
    public void OnBaseCalc()
    {
        State = AbilityType.BaseDecided;
        BaseDmg = Word.Type1 == WordType.Empty ? 7 : 10;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public void OnPropCalc()
    {
        State = AbilityType.PropCalced;
        if (!Actor.Ability.Type.HasFlag(State) && !Receiver.Ability.Type.HasFlag(State))
        {
            PropDmg = Word.CalcAmp(Receiver.CurrentWord);
            return;
        }
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
        if (Receiver.Ability.Type.HasFlag(State))
            Receiver.Ability.Execute(this);
    }
    public void OnAmpCalc()
    {
        State = AbilityType.AmpDecided;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public void OnBrdCalc()
    {
        State = AbilityType.BrdDecided;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public void OnCritCalc()
    {
        State = AbilityType.CritDecided;
        if (Word.IsCritable)
            CritFlag = new Random().Next(5) == 0;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public void OnMtpCalc()
    {
        State = AbilityType.MtpCalced;
        if (!Actor.Ability.Type.HasFlag(State) && !Receiver.Ability.Type.HasFlag(State))
        {
            MtpDmg = CritFlag ? Math.Max(Actor.ATK, 1) : Actor.ATK / Receiver.DEF;
            return;
        }
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
        if (Receiver.Ability.Type.HasFlag(State))
            Receiver.Ability.Execute(this);
    }
    public override void OnActionBegin()
    {
        State = AbilityType.ActionBegin;

        // NOTE: かりうむ式のダメージ計算。
        // 特性倍率と急所倍率の適用順は入れ替わる可能性あり。

        // 乱数の算出。
        var randomFlag = !(Actor.CurrentWord.Type1 == WordType.Empty || Receiver.CurrentWord.Type1 == WordType.Empty);
        var randomDmg = randomFlag ? 0.85 + new Random().Next(15) * 0.01 : 1;

        // 基礎になるダメージ（小数値）の計算。
        var dmgRoot = BaseDmg * PropDmg * MtpDmg * randomDmg;

        // とくせい倍率によるダメージ（小数点以下切り捨て）の計算。
        var dmgAmpCalced = (int)(dmgRoot * AmpDmg * BrdDmg);

        // 急所によるダメージ（小数点以下切り捨て）の計算。
        var dmgCritCalced = (int)(dmgAmpCalced * (CritFlag ? Player.CritDmg : 1));

        var damage = dmgCritCalced;
        Actor.Attack(Receiver, damage);

        /* 当初の実装。使わない。ゲーム内のダメージと誤差がある
        double damage;
        damage = BaseDmg * PropDmg * MtpDmg * AmpDmg * BrdDmg * (CritFlag ? Player.CritDmg : 1);
        if (!(Actor.CurrentWord.Type1 == WordType.Empty || Receiver.CurrentWord.Type1 == WordType.Empty))
            damage *= 0.85 + new Random().Next(15) * 0.01;
        Actor.Attack(Receiver, damage);
        */
    }
    public override void OnActionExecuted()
    {
        State = AbilityType.ActionExecuted;
        Message.Add(PropDmg switch
        {
            0 => "こうかがないようだ...",
            >= 2 => "こうかはばつぐんだ！",
            > 0 and < 1 => "こうかはいまひとつのようだ...",
            1 => "ふつうのダメージだ",
            _ => string.Empty
        }, Magenta);
        if (CritFlag)
        {
            Message.Add("急所に当たった！", Magenta);
        }
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public void OnViolenceUsed()
    {
        State = AbilityType.ViolenceUsed;
        if (!Word.IsViolence) return;
        if (Actor.Ability.Type.HasFlag(State))
        {
            Actor.Ability.Execute(this);
            return;
        }
        if (Actor.TryChangeATK(-2, Word))
        {
            Message.Add($"{Actor.Name} の攻撃ががくっと下がった！(現在{Actor.ATK,0:0.0#}倍)", Blue);
            return;
        }
        Message.Add($"{Actor.Name} の攻撃はもう下がらない！", Blue);
    }
    public override void OnActionEnd()
    {
        State = AbilityType.ActionEnd;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
    public override void OnReceive()
    {
        State = AbilityType.Received;
        if (Receiver.Ability.Type.HasFlag(State))
            Receiver.Ability.Execute(this);
    }
}
internal class BufContract : Contract
{
    public override ContractType Type => ContractType.Buf;
    #region constructors
    [SetsRequiredMembers]
    public BufContract(Player actor, Player receiver, Word word, ContractArgs args) : base(actor, receiver, word, args) { }
    [SetsRequiredMembers]
    public BufContract() : base() { }
    #endregion
    public override void OnActionBegin()
    {
        State = AbilityType.ActionBegin;
        if (Actor.Ability.Type.HasFlag(State))
            Actor.Ability.Execute(this);
    }
}
internal class HealContract : Contract
{
    public override ContractType Type => ContractType.Heal;
    protected bool HealFlag { get; set; } = false;
    public bool IsCure { get; set; } = false;
    public bool CanHeal { get; set; } = false;
    #region constructors
    [SetsRequiredMembers]
    public HealContract(Player actor, Player receiver, Word word, ContractArgs args) : base(actor, receiver, word, args)
    {
        Contents = new()
        {
            OnContractBegin,
            OnHealAmtCalc,
            OnDetermineCanHeal,
            OnActionBegin,
            OnActionExecuted,
            OnActionEnd,
            OnReceive,
            OnContractEnd
        };
    }
    [SetsRequiredMembers]
    public HealContract() : base() { }
    #endregion
    public void OnHealAmtCalc()
    {
        State = AbilityType.HealAmtCalc;
        if (Actor.Ability.Type.HasFlag(State))
        {
            Actor.Ability.Execute(this);
            return;
        }
        IsCure = !Word.ContainsType(WordType.Food);
    }
    public void OnDetermineCanHeal()
    {
        State = AbilityType.DetermineCanHeal;
        if (Actor.Ability.Type.HasFlag(State))
        {
            Actor.Ability.Execute(this);
            return;
        }
        if (IsCure)
        {
            CanHeal = Actor.CureCount < Player.MaxCureCount || IsCureInfinite;
            return;
        }
        CanHeal = Actor.FoodCount < Player.MaxFoodCount;
    }
    public override void OnActionBegin()
    {
        State = AbilityType.ActionBegin;
        if (Actor.Ability.Type.HasFlag(State))
        {
            Actor.Ability.Execute(this);
            return;
        }
        if (!CanHeal)
        {
            if (IsCure)
            {
                Message.Add($"{Actor.Name} はもう回復できない！", Yellow);
                return;
            }
            Message.Add($"{Actor.Name} はもう食べられない！", Yellow);
            return;
        }
        Actor.Heal(IsCure);
    }
    public override void OnActionExecuted()
    {
        State = AbilityType.ActionExecuted;
        if (Actor.Ability.Type.HasFlag(State))
        {
            Actor.Ability.Execute(this);
            return;
        }
        if (IsCure && CanHeal)
        {
            if (Actor.State.HasFlag(Player.PlayerState.Poison))
            {
                Message.Add($"{Actor.Name} の毒がなおった！", Green);
                Actor.DePoison();
            }
            Message.Add($"{Actor.Name} の体力が回復した", Green);
        }
    }
}
internal class SeedContract : Contract
{
    public override ContractType Type => ContractType.Seed;
    public bool SeedFlag { get; internal set; } = false;
    #region constructors
    [SetsRequiredMembers]
    public SeedContract(Player actor, Player receiver, Word word, ContractArgs args) : base(actor, receiver, word, args) { }
    [SetsRequiredMembers]
    public SeedContract() : base() { }
    #endregion
}
