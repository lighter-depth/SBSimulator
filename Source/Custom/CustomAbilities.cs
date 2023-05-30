namespace SBSimulator.Source;

// カスタム特性のテスト。
// CustomAbility クラスを継承して実装する。 

/// <summary>
/// カスタム特性の作成時に使用するクラスです。
/// </summary>
internal abstract class CustomAbility : Ability
{
    public override List<string> Name => CustomName;
    public abstract List<string> CustomName { get; }
}

/// <summary>
/// マジックミラー。状態異常を跳ね返す。
/// </summary>
internal class MagicMirror : CustomAbility
{
    public override AbilityType Type => AbilityType.Received;
    public override List<string> CustomName => new() { "mm", "MM", "マジックミラー", "まじっくみらー", "magicmirror", "MagicMirror", "MAGICMIRROR" };
    public override void Execute(Contract c)
    {
        if (c is AttackContract ac && ac.PoisonFlag && ac.Receiver.State.HasFlag(Player.PlayerState.Poison))
        {
            ac.Receiver.DePoison();
            ac.Actor.Poison();
            ac.Message.Add($"{ac.Receiver.Name} は{Player.PlayerState.Poison.StateToString()}を跳ね返した！", Notice.InvokeInfo);
            ac.Message.Add($"{ac.Actor.Name} は{Player.PlayerState.Poison.StateToString()}を受けた！", Notice.InvokeInfo);
        }
        if (c is SeedContract sc && sc.SeedFlag && sc.Receiver.State.HasFlag(Player.PlayerState.Seed))
        {
            sc.Receiver.DeSeed();
            sc.Actor.Seed();
            sc.Message.Add($"{sc.Receiver.Name} は{Player.PlayerState.Seed.StateToString()}を跳ね返した！", Notice.InvokeInfo);
            sc.Message.Add($"{sc.Actor.Name} は{Player.PlayerState.Seed.StateToString()}を植え付けられた！", Notice.InvokeInfo);
        }
    }
    public override string ToString() => "マジックミラー";
}

/// <summary>
/// てんねん。バフによる能力上昇補正を無視する。
/// </summary>
internal class Tennen : CustomAbility
{
    public override AbilityType Type => AbilityType.MtpCalced;
    public override List<string> CustomName => new() { "ua", "UA", "てんねん", "天然", "unaware", "Unaware", "UNAWARE" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Actor.Ability.Type.HasFlag(Type))
        {
            ac.MtpDmg = Math.Max(ac.Actor.ATK, 1);
            return;
        }
        ac.MtpDmg = 1 / ac.Receiver.DEF;
    }
    public override string ToString() => "てんねん";
}

/// <summary>
/// ふしぎなまもり。こうかばつぐん以外のダメージを無効化する。
/// </summary>
internal class WonderGuard : CustomAbility
{
    public override AbilityType Type => AbilityType.PropCalced;
    public override List<string> CustomName => new() { "wg", "WG", "ふしぎなまもり", "不思議な守り", "wonderguard", "WonderGuard", "WONDERGUARD" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        var prop = ac.Word.CalcAmp(ac.Receiver.CurrentWord);
        ac.PropDmg = prop;
        if (ac.Receiver.Ability is not WonderGuard) return;
        if (prop is < 2 && ac.Receiver.CurrentWord.Type1 != Word.WordType.Empty)
        {
            ac.PropDmg = 0;
            return;
        }
    }
    public override string ToString() => "ふしぎなまもり";
}

/// <summary>
/// がんじょう。即死するダメージを受けたときに、体力１を残して耐える。
/// </summary>
internal class Ganjou : CustomAbility
{
    public override AbilityType Type => AbilityType.Received;
    public override List<string> CustomName => new() { "st", "ST", "がんじょう", "頑丈", "sturdy", "Sturdy", "STURDY" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Receiver.HP <= 0 && ac.Args.PreActor.HP == ac.Args.PreActor.MaxHP)
        {
            ac.Receiver.HP = 1;
            ac.Message.Add($"{ac.Receiver.Name} はこうげきをこらえた！", Notice.InvokeInfo);
        }
    }
    public override string ToString() => "がんじょう";
}
/// <summary>
/// 「最強の特性」
/// </summary>
internal class God : CustomAbility
{
    public override AbilityType Type => AbilityType.AmpDecided | AbilityType.CritDecided | AbilityType.ViolenceUsed | AbilityType.ActionEnd | AbilityType.Received;
    public override List<string> CustomName => new() { "gd", "GD", "神", "かみ", "カミ", "god", "God", "GOD" };
    public override AnnotatedString? InitMessage { get; protected set; } = ("強すぎてつよしになったわね...", Notice.Warn);
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.State == AbilityType.AmpDecided)
        {
            ac.AmpDmg = 2 * Math.Max(0, ac.Actor.CurrentWord.Length - 5) + 1;
        }
        if (ac.State == AbilityType.CritDecided)
        {
            ac.CritFlag = true;
        }
        if (ac.State == AbilityType.ViolenceUsed)
        {
            if (ac.Actor.TryChangeATK(Buf, ac.Word))
            {
                ac.Message.Add($"{ac.Actor.Name} の攻撃が下がった！(現在{ac.Actor.ATK,0:0.0#}倍)", Notice.BufInfo);
                return;
            }
            ac.Message.Add($"{ac.Actor.Name} の攻撃はもう下がらない！", Notice.Caution);
        }
        if (ac.State == AbilityType.ActionEnd)
        {
            if (ac.Actor.TryChangeATK(4, ac.Word))
            {
                ac.Message.Add($"{ac.Actor.Name} の攻撃がぐーんぐーんと上がった！！！！(現在{ac.Actor.ATK,0:0.0#}倍)", Notice.BufInfo);
                return;
            }
            ac.Message.Add($"{ac.Actor.Name} の攻撃はもう上がらない！", Notice.Caution);
        }
        if (ac.State == AbilityType.Received)
        {
            ac.Receiver.TryChangeATK(100, ac.Receiver.CurrentWord);
            ac.Message.Add($"{ac.Receiver.Name} はダメージを受けて攻撃がぐぐーんぐーんと上がった！！！！！ (現在{ac.Receiver.ATK,0:0.0#}倍)", Notice.BufInfo);
            ac.Receiver.TryChangeDEF(100, ac.Receiver.CurrentWord);
            ac.Message.Add($"{ac.Receiver.Name} はダメージを受けて防御がぐぐーんぐーんと上がった！！！１！ (現在{ac.Receiver.DEF,0:0.0#}倍)", Notice.BufInfo);
        }
    }
    public override string ToString() => "神";
}
