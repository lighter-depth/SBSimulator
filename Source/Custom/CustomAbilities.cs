using static System.ConsoleColor;
using static SBSimulator.Source.SBOptions;
namespace SBSimulator.Source;

// カスタム特性のテスト。
// CustomAbility クラスを継承して実装する。 

/// <summary>
/// カスタム特性の作成時に使用するクラスです。
/// </summary>
internal abstract class CustomAbility : Ability
{
    public override List<string> Name => IsCustomAbilUsable ? CustomName : new();
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
        if(c is not AttackContract ac) return;
        if(ac.Receiver.HP <= 0 && ac.Args.PreActor.HP == ac.Args.PreActor.MaxHP)
        {
            ac.Receiver.Endure();
            ac.Message.Add($"{ac.Receiver.Name} はこうげきをこらえた！", Notice.InvokeInfo);
        }
    }
    public override string ToString() => "がんじょう";
}
