using System.Reflection;
using static SBSimulator.Source.Player;
using static SBSimulator.Source.SBOptions;
using static SBSimulator.Source.Word;
using static System.ConsoleColor;

namespace SBSimulator.Source;
[Flags]
internal enum AbilityType
{
    None = 0,
    WordUsedChecked = 1 << 0,
    WordInferChecked = 1 << 1,
    ContractBegin =  1 << 2,
    BaseDecided = 1 << 3,
    PropCalced = 1 << 4,
    AmpDecided = 1 << 5,
    BrdDecided = 1 << 6,
    CritDecided = 1 << 7,
    MtpCalced = 1 << 8,
    HealAmtCalc = 1 << 9,
    DetermineCanHeal = 1 << 10,
    ActionBegin = 1 << 11,
    ActionExecuted = 1 << 12,
    ViolenceUsed = 1 << 13,
    ActionEnd = 1 << 14,
    Received = 1 << 15,
    ContractEnd = 1 << 16
}
internal class AbilityFactory
{
    // HACK: リフレクションを使わない実装に変えたい。dynamic を使えば実装できる？
    public static Ability? Create(string name)
    {
        var subClasses = Assembly.GetAssembly(typeof(Ability))?.GetTypes().Where(x => x.IsSubclassOf(typeof(Ability)) && !x.IsAbstract).ToArray() ?? Array.Empty<Type>();
        foreach(var i in subClasses)
        {
            var sub = Activator.CreateInstance(i) as Ability;
            if(sub?.Name.Contains(name) == true)
                return sub;
        }
        return null;
    }
}
internal abstract class Ability
{
    public abstract AbilityType Type { get; }
    public abstract List<string> Name { get; }
    public virtual int Base { get; protected set; }
    public virtual int Buf { get; protected set; }
    public virtual double Amp { get; protected set; }
    public abstract void Execute(Contract c);
    public new abstract string ToString();
}
internal interface ISingleTypedBufAbility
{
    public WordType BufType { get; }
}
internal interface ISeedable
{
    public WordType SeedType { get; }
}
internal class Debugger : Ability
{
    public override AbilityType Type => AbilityType.BaseDecided;
    public override List<string> Name => new() { "N", "n", "deb", "デバッガー", "でばっがー", "debugger", "Debugger", "DEBUGGER", "出歯" };
    public override int Base => 13;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.Type1 == Word.WordType.Empty)
            ac.BaseDmg = Base;
    }
    public override string ToString() => "デバッガー";
}
internal class Hanshoku : Ability
{
    public override AbilityType Type => AbilityType.BrdDecided | AbilityType.WordUsedChecked;
    public override List<string> Name => new() { "A", "a", "brd", "はんしょく", "繁殖", "hanshoku", "Hanshoku", "HANSHOKU" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if(ac.State == AbilityType.WordUsedChecked && !ac.Args.IsWordNotUsed)
        {
            ac.Args.IsWordNotUsed = ac.Word.ContainsType(WordType.Animal);
        }
        if(ac.State == AbilityType.BrdDecided)
        {
            var brdBufNames = ac.Actor.BrdBuf.Select(x => x.Name).ToList();
            if (brdBufNames.Contains(ac.Word.Name))
            {
                ac.BrdDmg = ac.Actor.BrdBuf[brdBufNames.IndexOf(ac.Word.Name)].Rep + 1;
                ac.Actor.BrdBuf[brdBufNames.IndexOf(ac.Word.Name)].Increment();
                return;
            }
            ac.Actor.BrdBuf.Add(new BredString(ac.Word.Name));
        }
    }
    public override string ToString() => "はんしょく";
}
internal class Yadorigi : Ability, ISeedable
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "Y", "y", "sed", "やどりぎ", "宿り木", "ヤドリギ", "宿木", "宿", "yadorigi", "Yadorigi", "YADORIGI" };
    public WordType SeedType => WordType.Plant;
    public override void Execute(Contract c)
    {
        if (c is not SeedContract sc) return;
        sc.Receiver.Seed();
        sc.SeedFlag = true;
        sc.Message.Add($"{sc.Actor.Name} は {sc.Receiver.Name} に種を植え付けた！", DarkGreen);
    }
    public override string ToString() => "やどりぎ";
}
internal class Global : Ability
{
    public override AbilityType Type => AbilityType.AmpDecided;
    public override List<string> Name => new() { "G", "g", "gbl", "グローバル", "ぐろーばる", "global", "Global", "GLOBAL" };
    public override double Amp => 1.5;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.ContainsType(WordType.Place))
            ac.AmpDmg = Amp;
    }
    public override string ToString() => "グローバル";
}
internal class Jounetsu : Ability, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "E", "e", "psn", "じょうねつ", "情熱", "jounetsu", "Jounetsu", "JOUNETSU" };
    public WordType BufType => WordType.Emote;
    public override int Buf => 1;
    public override void Execute(Contract c)
    {
        if (c is not BufContract bc) return;
        if (bc.Word.ContainsType(BufType) && bc.Actor.TryChangeATK(Buf, bc.Word))
        {
            bc.Message.Add($"{bc.Actor.Name} の攻撃が上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Blue);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Yellow);
    }
    public override string ToString() => "じょうねつ";
}
internal class RocknRoll : Ability, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "C", "c", "rar", "ロックンロール", "ろっくんろーる", "rocknroll", "RocknRoll", "ROCKNROLL", "ロクロ", "轆轤", "ろくろ" };
    public WordType BufType => WordType.Art;
    public override int Buf => 2;
    public override void Execute(Contract c)
    {
        if (c is not BufContract bc) return;
        if (bc.Word.ContainsType(BufType) && bc.Actor.TryChangeATK(Buf, bc.Word))
        {
            bc.Message.Add($"{bc.Actor.Name} の攻撃がぐーんと上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Blue);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Yellow);
    }
    public override string ToString() => "ロックンロール";
}
internal class Ikasui : Ability
{
    public override AbilityType Type => AbilityType.DetermineCanHeal;
    public override List<string> Name => new() { "F", "f", "glt", "いかすい", "胃下垂", "ikasui", "Ikasui", "IKASUI" };
    public override void Execute(Contract c)
    {
        if(c is not HealContract hc) return;
        if (!hc.IsCure)
        {
            hc.CanHeal = true;
            return;
        }
        hc.CanHeal = hc.Actor.CureCount < MaxCureCount || IsCureInfinite;
    }
    public override string ToString() => "いかすい";
}
internal class Mukimuki : Ability
{
    public override AbilityType Type => AbilityType.ViolenceUsed;
    public override List<string> Name => new() { "V", "v", "msl", "むきむき", "mukimuki", "Mukimuki", "MUKIMUKI", "最強の特性", "最強特性" };
    public override int Buf => -1;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Actor.TryChangeATK(Buf, ac.Word))
        {
            ac.Message.Add($"{ac.Actor.Name} の攻撃が下がった！(現在{ac.Actor.ATK,0:0.0#}倍)", Blue);
            return;
        }
        ac.Message.Add($"{ac.Actor.Name} の攻撃はもう下がらない！", Blue);
    }
    public override string ToString() => "むきむき";
}
internal class Ishoku : Ability
{
    public override AbilityType Type => AbilityType.HealAmtCalc;
    public override List<string> Name => new() { "H", "h", "mdc", "いしょくどうげん", "医食同源", "ishoku", "Ishoku", "ISHOKU", "いしょく", "医食" };
    public override void Execute(Contract c)
    {
        if (c is not HealContract hc) return;
        hc.IsCure = true;
    }
    public override string ToString() => "いしょくどうげん";
}
internal class Karate : Ability
{
    public override AbilityType Type => AbilityType.CritDecided;
    public override List<string> Name => new() { "B", "b", "kar", "からて", "空手", "karate", "Karate", "KARATE" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.ContainsType(WordType.Body))
            ac.CritFlag = true;
    }
    public override string ToString() => "からて";
}
internal class Kachikochi : Ability, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "M", "m", "clk", "かちこち", "kachikochi", "Kachikochi", "KACHIKOCHI", "sus" };
    public override int Buf => 1;
    public WordType BufType => WordType.Mech;
    public override void Execute(Contract c)
    {
        if (c is not BufContract bc) return;
        if (bc.Word.ContainsType(BufType) && bc.Actor.TryChangeDEF(Buf, bc.Word))
        {
            bc.Message.Add($"{bc.Actor.Name} の防御が上がった！(現在{bc.Actor.DEF,0:0.0#}倍)", Blue);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の防御はもう上がらない！", Yellow);
    }
    public override string ToString() => "かちこち";
}
internal class Jikken : Ability
{
    public override AbilityType Type => AbilityType.AmpDecided;
    public override List<string> Name => new() { "Q", "q", "exp", "じっけん", "実験", "jikken", "Jikken", "JIKKEN" };
    public override double Amp => 1.5;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.ContainsType(WordType.Science))
            ac.AmpDmg = Amp;
    }
    public override string ToString() => "じっけん";
}
internal class Sakinobashi : Ability, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "T", "t", "prc", "さきのばし", "先延ばし", "sakinobashi", "Sakinobashi", "SAKINOBASHI", "めざまし" };
    public override int Buf => 1;
    public WordType BufType => WordType.Time;
    public override void Execute(Contract c)
    {
        if (c is not BufContract bc) return;
        if (bc.Word.ContainsType(BufType) && bc.Actor.TryChangeDEF(Buf, bc.Word))
        {
            bc.Message.Add($"{bc.Actor.Name} の防御が上がった！(現在{bc.Actor.DEF,0:0.0#}倍)", Blue);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の防御はもう上がらない！", Yellow);
    }
    public override string ToString() => "さきのばし";
}
internal class Kyojin : Ability
{
    public override AbilityType Type => AbilityType.AmpDecided;
    public override List<string> Name => new() { "P", "p", "gnt", "きょじん", "巨人", "kyojin", "Kyojin", "KYOJIN", "準最強特性" };
    public override double Amp => 1.5;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.ContainsType(WordType.Person))
            ac.AmpDmg = Amp;
    }
    public override string ToString() => "きょじん";
}
internal class Busou : Ability, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "K", "k", "arm", "ぶそう", "武装", "busou", "Busou", "BUSOU", "富士山" };
    public override int Buf => 1;
    public WordType BufType => WordType.Work;
    public override void Execute(Contract c)
    {
        if (c is not BufContract bc) return;
        if (bc.Word.ContainsType(BufType) && bc.Actor.TryChangeATK(Buf, bc.Word))
        {
            bc.Message.Add($"{bc.Actor.Name} の攻撃が上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Blue);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Yellow);
    }
    public override string ToString() => "ぶそう";
}
internal class Kasanegi : Ability, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "L", "l", "lyr", "かさねぎ", "重ね着", "kasanegi", "Kasanegi", "KASANEGI" };
    public override int Buf => 1;
    public WordType BufType => WordType.Cloth;
    public override void Execute(Contract c)
    {
        if (c is not BufContract bc) return;
        if (bc.Word.ContainsType(BufType) && bc.Actor.TryChangeDEF(Buf, bc.Word))
        {
            bc.Message.Add($"{bc.Actor.Name} の防御が上がった！(現在{bc.Actor.DEF,0:0.0#}倍)", Blue);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の防御はもう上がらない！", Yellow);
    }
    public override string ToString() => "かさねぎ";

}
internal class Hoken : Ability
{
    public override AbilityType Type => AbilityType.Received;
    public override List<string> Name => new() { "S", "s", "ins", "ほけん", "保険", "hoken", "Hoken", "HOKEN", "じゃくてんほけん", "弱点保険", "じゃくほ", "弱保" };
    public override int Buf => InsBufQty;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.CalcAmp(ac.Receiver.CurrentWord) >= 2)
        {
            ac.Receiver.TryChangeATK(InsBufQty, ac.Receiver.CurrentWord);
            ac.Message.Add($"{ac.Receiver.Name} は弱点を突かれて攻撃がぐぐーんと上がった！ (現在{ac.Receiver.ATK,0:0.0#}倍)", Blue);
        }
    }
    public override string ToString() => "ほけん";
}
internal class Kakumei : Ability
{
    public override AbilityType Type => AbilityType.ActionEnd;
    public override List<string> Name => new() { "J", "j", "rev", "かくめい", "革命", "kakumei", "Kakumei", "KAKUMEI" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (!ac.Word.IsHeal && ac.Word.ContainsType(WordType.Play))
        {
            ac.Actor.Rev(ac.Receiver);
            ac.Message.Add("すべての能力変化がひっくりかえった！", Cyan);
        }
    }
    public override string ToString() => "かくめい";
}
internal class Dokubari : Ability
{
    public override AbilityType Type => AbilityType.ActionExecuted;
    public override List<string> Name => new() { "D", "d", "ndl", "どくばり", "毒針", "dokubari", "Dokubari", "DOKUBARI" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (!ac.Word.IsHeal && ac.Word.ContainsType(WordType.Bug) && !ac.Receiver.State.HasFlag(PlayerState.Poison))
        {
            ac.Receiver.Poison();
            ac.PoisonFlag = true;
            ac.Message.Add($"{ac.Receiver.Name} は毒を受けた！", DarkGreen);
        }
    }
    public override string ToString() => "どくばり";
}
internal class Keisan : Ability, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "X", "x", "clc", "けいさん", "計算", "keisan", "Keisan", "KEISAN" };
    public override int Buf => 1;
    public WordType BufType => WordType.Math;
    public override void Execute(Contract c)
    {
        if (c is not BufContract bc) return;
        if (bc.Word.ContainsType(BufType) && bc.Actor.TryChangeATK(Buf, bc.Word))
        {
            bc.Message.Add($"{bc.Actor.Name} の攻撃が上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Blue);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Yellow);
    }
    public override string ToString() => "けいさん";
}
internal class Zuboshi : Ability
{
    public override AbilityType Type => AbilityType.CritDecided;
    public override List<string> Name => new() { "Z", "z", "htm", "ずぼし", "図星", "zuboshi", "Zuboshi", "ZUBOSHI" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.ContainsType(WordType.Insult))
            ac.CritFlag = true;
    }
    public override string ToString() => "ずぼし";
}
internal class Shinkoushin : Ability
{
    public override AbilityType Type => AbilityType.AmpDecided;
    public override List<string> Name => new() { "R", "r", "fth", "しんこうしん", "信仰心", "shinkoushin", "Shinkoushin", "SHINKOUSHIN", "ドグマ" };
    public override double Amp => 1.5;
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.ContainsType(WordType.Religion))
            ac.AmpDmg = Amp;
    }
    public override string ToString() => "しんこうしん";
}
internal class Training : Ability, ISingleTypedBufAbility
{
    public override AbilityType Type => AbilityType.ActionBegin;
    public override List<string> Name => new() { "U", "u", "trn", "トレーニング", "とれーにんぐ", "training", "Training", "TRAINING", "誰も使わない特性" };
    public override int Buf => 1;
    public WordType BufType => WordType.Sports;
    public override void Execute(Contract c)
    {
        if (c is not BufContract bc) return;
        if (bc.Word.ContainsType(BufType) && bc.Actor.TryChangeATK(Buf, bc.Word))
        {
            bc.Message.Add($"{bc.Actor.Name} の攻撃が上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Blue);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Yellow);
    }
    public override string ToString() => "トレーニング";
}
internal class WZ : Ability
{
    public override AbilityType Type => AbilityType.ActionEnd;
    public override List<string> Name => new() { "W", "w", "tph", "たいふういっか", "台風一過", "台風一家", "WZ", "wz", "WeathersZero", "天で話にならねぇよ..." };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (!ac.Word.IsHeal && ac.Word.ContainsType(WordType.Weather))
        {
            ac.Actor.WZ(ac.Receiver);
            ac.Message.Add("すべての能力変化が元に戻った！", Cyan);
        }
    }
    public override string ToString() => "たいふういっか";
}
internal class Oremoji : Ability
{
    public override AbilityType Type => AbilityType.AmpDecided;
    public override List<string> Name => new() { "O", "o", "orm", "おれのことばのもじすうがおおいほどいりょくがおおきくなるけんについて", "俺の言葉の文字数が多いほど威力が大きくなる件について", "おれもじ", "俺文字", "oremoji", "Oremoji", "OREMOJI" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        ac.AmpDmg = ac.Actor.CurrentWord.Length is >= 7 ? 2
                  : ac.Actor.CurrentWord.Length is 6 ? 1.5
                  : 1;
    }
    public override string ToString() => "俺文字";
}
