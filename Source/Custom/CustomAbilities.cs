using static System.ConsoleColor;
namespace SBSimulator.Source;

// カスタム特性のテスト。
// Ability クラスを継承して実装する。

// マジックミラー。状態異常を跳ね返す。
internal class MagicMirror : Ability
{
    public override AbilityType Type => AbilityType.Received;
    public override List<string> Name => new() { "mm", "MM", "マジックミラー", "まじっくみらー", "magicmirror", "MagicMirror", "MAGICMIRROR" };
    public override void Execute(Contract c)
    {
        if (c is AttackContract ac && ac.PoisonFlag && ac.Receiver.State.HasFlag(Player.PlayerState.Poison))
        {
            ac.Receiver.DePoison();
            ac.Actor.Poison();
            ac.Message.Add($"{ac.Receiver.Name} は{Player.PlayerState.Poison.StateToString()}を跳ね返した！", Green);
        }
        if (c is SeedContract sc && sc.SeedFlag && sc.Receiver.State.HasFlag(Player.PlayerState.Seed))
        {
            sc.Receiver.DeSeed();
            sc.Actor.Seed();
            sc.Message.Add($"{sc.Receiver.Name} は{Player.PlayerState.Seed.StateToString()}を跳ね返した！", Green);
        }
    }
    public override string ToString() => "マジックミラー";
}

// てんねん。バフによる能力上昇補正を無視する。
internal class Tennen : Ability 
{
    public override AbilityType Type => AbilityType.MtpCalced;
    public override List<string> Name => new() { "ua", "UA", "てんねん", "天然", "unaware", "Unaware", "UNAWARE" };
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

// ふしぎなまもり。こうかばつぐん以外のダメージを無効化する。
internal class WonderGuard : Ability
{
    public override AbilityType Type => AbilityType.PropCalced;
    public override List<string> Name => new() { "wg", "WG", "ふしぎなまもり", "不思議な守り", "wonderguard", "WonderGuard", "WONDERGUARD" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Receiver.Ability is not WonderGuard) return;
        var prop = ac.Word.CalcAmp(ac.Receiver.CurrentWord);
        if(prop is < 2 && ac.Receiver.CurrentWord.Type1 != Word.WordType.Empty)
        {
            ac.PropDmg = 0;
            return;
        }
        ac.PropDmg = prop;
    }
    public override string ToString() => "ふしぎなまもり";
}

// がんじょう。即死するダメージを受けたときに、体力１を残して耐える。
internal class Ganjou : Ability
{
    public override AbilityType Type => AbilityType.Received;
    public override List<string> Name => new() { "st", "ST", "がんじょう", "頑丈", "sturdy", "Sturdy", "STURDY" };
    public override void Execute(Contract c) 
    { 
        if(c is not AttackContract ac) return;
        if(ac.Receiver.HP <= 0 && ac.Args.PreActor.HP == ac.Args.PreActor.MaxHP)
        {
            ac.Receiver.Endure();
            ac.Message.Add($"{ac.Receiver.Name} はこうげきをこらえた！", Green);
        }
    }
    public override string ToString() => "がんじょう";
}
