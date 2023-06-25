using System.Reflection;
using System.Runtime.CompilerServices;
using static SBSimulator.Player;
using static SBSimulator.Word;

namespace SBSimulator;
/// <summary>
/// とくせいの発動条件を管理するフラグです。
/// </summary>
[Flags]
public enum AbilityType
{
    /// <summary>
    /// とくせいは発動しません。
    /// </summary>
    None = 0,
    /// <summary>
    /// とくせいは単語の使用チェック時に発動します。
    /// </summary>
    WordUsedChecked = 1 << 0,
    /// <summary>
    /// とくせいは単語のタイプ推論時に発動します。
    /// </summary>
    WordInferChecked = 1 << 1,
    /// <summary>
    /// とくせいは<see cref="Contract"/>の開始時に発動します。
    /// </summary>
    ContractBegin =  1 << 2,
    /// <summary>
    /// とくせいは基礎ダメージの決定時に発動します。
    /// </summary>
    BaseDecided = 1 << 3,
    /// <summary>
    /// とくせいはタイプ相性の決定時に発動します。
    /// </summary>
    PropCalced = 1 << 4,
    /// <summary>
    /// とくせいは自身の作用する倍率の決定時に発動します。
    /// </summary>
    AmpDecided = 1 << 5,
    /// <summary>
    /// とくせいははんしょくによる倍率の決定時に発動します。
    /// </summary>
    BrdDecided = 1 << 6,
    /// <summary>
    /// とくせいは急所の決定時に発動します。
    /// </summary>
    CritDecided = 1 << 7,
    /// <summary>
    /// とくせいはバフによる倍率の決定時に発動します。
    /// </summary>
    MtpCalced = 1 << 8,
    /// <summary>
    /// とくせいは回復の種類の決定時に発動します。
    /// </summary>
    HealAmtCalc = 1 << 9,
    /// <summary>
    /// とくせいは回復の実行時に発動します。
    /// </summary>
    DetermineCanHeal = 1 << 10,
    /// <summary>
    /// とくせいはアクションの開始時に発動します。
    /// </summary>
    ActionBegin = 1 << 11,
    /// <summary>
    /// とくせいはアクションの実行時に発動します。
    /// </summary>
    ActionExecuted = 1 << 12,
    /// <summary>
    /// とくせいはアクションの使用後、暴力タイプが使用された場合に発動します。
    /// </summary>
    ViolenceUsed = 1 << 13,
    /// <summary>
    /// とくせいはアクションの終了時に発動します。
    /// </summary>
    ActionEnd = 1 << 14,
    /// <summary>
    /// とくせいはアクションを受け取ったときに発動します。
    /// </summary>
    Received = 1 << 15,
    /// <summary>
    /// とくせいは<see cref="Contract"/>の終了時に発動します。
    /// </summary>
    ContractEnd = 1 << 16
}
/// <summary>
/// とくせいを生成するファクトリ クラスです。
/// </summary>
public class AbilityManager
{
    // HACK: リフレクションを使わない実装に変えたい。dynamic を使えば実装できる？
    /// <summary>
    /// 文字列からとくせいを生成します。
    /// </summary>
    /// <param name="name">生成に使用する文字列</param>
    /// <returns>入力から推論されたとくせい</returns>
    public static Ability? Create(string name, bool allowCustomAbility)
    {
        var subClasses = Assembly.GetAssembly(typeof(Ability))?.GetTypes()
            .Where(x => x.IsSubclassOf(typeof(Ability)) && !x.IsAbstract).ToArray() ?? Array.Empty<Type>();
        foreach(var i in subClasses)
        {
            var sub = Activator.CreateInstance(i) as Ability;
            if(sub?.Name.Contains(name) == true && !(!allowCustomAbility && sub is CustomAbility))
            {
                return sub;
            }
        }
        return null;
    }
}

/// <summary>
/// とくせいの情報を管理するスーパークラスです。
/// </summary>
public abstract class Ability
{
    /// <summary>
    /// とくせいの発動する条件
    /// </summary>
    public abstract AbilityType Type { get; }

    /// <summary>
    /// 生成時に参照する名前
    /// </summary>
    public abstract List<string> Name { get; }

    /// <summary>
    /// バフのインデックス値
    /// </summary>
    public virtual int Buf { get; protected set; }

    /// <summary>
    /// 攻撃力に作用させる倍率
    /// </summary>
    public virtual double Amp { get; protected set; }
    /// <summary>
    /// とくせい初期化時に表示するメッセージ
    /// </summary>
    public virtual AnnotatedString? InitMessage { get; protected set; }

    /// <summary>
    /// とくせいを実行します。
    /// </summary>
    /// <param name="c">発動元の<see cref="Contract"/></param>
    public abstract void Execute(Contract c);
    public new abstract string ToString();
}
/// <summary>
/// バフ系のとくせいに実装するインターフェースです。
/// </summary>
internal interface ISingleTypedBufAbility
{
    /// <summary>
    /// バフ時に使用する単語のタイプ
    /// </summary>
    public WordType BufType { get; }
}

/// <summary>
/// やどりぎ系のとくせいに実装するインターフェースです。
/// </summary>
internal interface ISeedable
{
    /// <summary>
    /// やどりぎ時に使用する単語のタイプ
    /// </summary>
    public WordType SeedType { get; }
}
/// <summary>
/// デバッガー。まだタイプのついていない言葉の威力が上がる
/// </summary>
internal class Debugger : Ability
{
    public override AbilityType Type => AbilityType.BaseDecided;
    public override List<string> Name => new() { "N", "n", "deb", "デバッガー", "でばっがー", "debugger", "Debugger", "DEBUGGER", "出歯" };
    public override void Execute(Contract c)
    {
        if (c is not AttackContract ac) return;
        if (ac.Word.Type1 == WordType.Empty)
            ac.BaseDmg = 13;
    }
    public override string ToString() => "デバッガー";
}
/// <summary>
/// はんしょく。動物タイプの言葉なら何度でも繰り返し使え、繰り返すごとに威力が上がる
/// </summary>
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
/// <summary>
/// やどりぎ。植物タイプの言葉を使うとダメージを与える代わりに相手に宿木を植え付ける
/// </summary>
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
        sc.Message.Add($"{sc.Actor.Name} は {sc.Receiver.Name} に種を植え付けた！", Notice.AuxInfo);
    }
    public override string ToString() => "やどりぎ";
}
/// <summary>
/// グローバル。地名タイプの言葉の威力が上がる
/// </summary>
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
/// <summary>
/// じょうねつ。感情タイプの言葉を使うとダメージを与える代わりに攻撃力が上がる
/// </summary>
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
            bc.Message.Add($"{bc.Actor.Name} の攻撃が上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Notice.Caution);
    }
    public override string ToString() => "じょうねつ";
}
/// <summary>
/// ロックンロール。芸術タイプの言葉を使うとダメージを与える代わりに攻撃力がぐーんと上がる
/// </summary>
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
            bc.Message.Add($"{bc.Actor.Name} の攻撃がぐーんと上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Notice.Caution);
    }
    public override string ToString() => "ロックンロール";
}
/// <summary>
/// いかすい。いくらでも食べることができる
/// </summary>
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
        hc.CanHeal = hc.Actor.CureCount < MaxCureCount || hc.Parent.IsCureInfinite;
    }
    public override string ToString() => "いかすい";
}
/// <summary>
/// むきむき。暴力タイプの言葉を使っても攻撃力がすこししか下がらなくなる
/// </summary>
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
            ac.Message.Add($"{ac.Actor.Name} の攻撃が下がった！(現在{ac.Actor.ATK,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        ac.Message.Add($"{ac.Actor.Name} の攻撃はもう下がらない！", Notice.Caution);
    }
    public override string ToString() => "むきむき";
}
/// <summary>
/// いしょくどうげん。食べ物タイプの言葉で医療タイプと同じ効果が得られる
/// </summary>
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
/// <summary>
/// からて。人体タイプの言葉を使った時に必ず相手の急所に当たる
/// </summary>
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
/// <summary>
/// かちこち。機械タイプの言葉を使うとダメージを与える代わりに防御力が上がる
/// </summary>
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
            bc.Message.Add($"{bc.Actor.Name} の防御が上がった！(現在{bc.Actor.DEF,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の防御はもう上がらない！", Notice.Caution);
    }
    public override string ToString() => "かちこち";
}
/// <summary>
/// じっけん。理科タイプの言葉の威力が上がる
/// </summary>
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
/// <summary>
/// さきのばし。時間タイプの言葉を使うとダメージを与える代わりに防御力が上がる
/// </summary>
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
            bc.Message.Add($"{bc.Actor.Name} の防御が上がった！(現在{bc.Actor.DEF,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の防御はもう上がらない！", Notice.Caution);
    }
    public override string ToString() => "さきのばし";
}
/// <summary>
/// きょじん。人物タイプの言葉の威力が上がる
/// </summary>
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
/// <summary>
/// ぶそう。工作タイプの言葉を使うとダメージを与える代わりに攻撃力が上がる
/// </summary>
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
            bc.Message.Add($"{bc.Actor.Name} の攻撃が上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Notice.Caution);
    }
    public override string ToString() => "ぶそう";
}
/// <summary>
/// かさねぎ。服飾タイプの言葉を使うとダメージを与える代わりに防御力が上がる
/// </summary>
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
            bc.Message.Add($"{bc.Actor.Name} の防御が上がった！(現在{bc.Actor.DEF,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の防御はもう上がらない！", Notice.Caution);
    }
    public override string ToString() => "かさねぎ";

}
/// <summary>
/// ほけん。効果抜群のダメージを受けると攻撃力がぐぐーんと上がる
/// </summary>
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
            ac.Message.Add($"{ac.Receiver.Name} は弱点を突かれて攻撃がぐぐーんと上がった！ (現在{ac.Receiver.ATK,0:0.0#}倍)", Notice.BufInfo);
        }
    }
    public override string ToString() => "ほけん";
}
/// <summary>
/// かくめい。遊びタイプの言葉を使うたびに自分と相手の能力変化をひっくり返す
/// </summary>
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
            ac.Message.Add("すべての能力変化がひっくりかえった！", Notice.SystemInfo);
        }
    }
    public override string ToString() => "かくめい";
}
/// <summary>
/// どくばり。虫タイプの言葉を使うと相手を毒状態にできる
/// </summary>
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
            ac.Message.Add($"{ac.Receiver.Name} は毒を受けた！", Notice.AuxInfo);
        }
    }
    public override string ToString() => "どくばり";
}
/// <summary>
/// けいさん。数学タイプの言葉を使うとダメージを与える代わりに攻撃力が上がる
/// </summary>
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
            bc.Message.Add($"{bc.Actor.Name} の攻撃が上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Notice.Caution);
    }
    public override string ToString() => "けいさん";
}
/// <summary>
/// ずぼし。暴言タイプの言葉を使った時に必ず相手の急所に当たる
/// </summary>
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
/// <summary>
/// しんこうしん。宗教タイプの言葉の威力が上がる
/// </summary>
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
/// <summary>
/// トレーニング。スポーツタイプの言葉を使うとダメージを与える代わりに攻撃力が上がる
/// </summary>
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
            bc.Message.Add($"{bc.Actor.Name} の攻撃が上がった！(現在{bc.Actor.ATK,0:0.0#}倍)", Notice.BufInfo);
            return;
        }
        bc.Message.Add($"{bc.Actor.Name} の攻撃はもう上がらない！", Notice.Caution);
    }
    public override string ToString() => "トレーニング";
}
/// <summary>
/// たいふういっか。天気タイプの言葉を使うと自分と相手の能力変化をもとに戻す
/// </summary>
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
            ac.Message.Add("すべての能力変化が元に戻った！", Notice.SystemInfo);
        }
    }
    public override string ToString() => "たいふういっか";
}
/// <summary>
/// 俺文字。言葉の文字数が多いほど威力が大きくなる
/// </summary>
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
