using static SBSimulator.Source.Word;
using static SBSimulator.Source.SBOptions;

namespace SBSimulator.Source;

internal class Player
{
    #region instance properties
    public string Name { get; init; } = "じぶん";
    public int HP { get; private set; }
    public double ATK => playerBufCap[ATKIndex];
    public int ATKIndex { get; private set; } = 6;
    public double DEF => playerBufCap[DEFIndex];
    public int DEFIndex { get; private set; } = 6;

    public Word CurrentWord { get; internal set; } = new Word(string.Empty, WordType.Empty);
    public Ability Ability { get; private set; } = null!;
    public int FoodCount { get; private set; } = 0;
    public int CureCount { get; private set; } = 0;
    public PlayerState State { get; private set; } = PlayerState.Normal;
    public int PoisonDmg { get; private set; } = 0;
    public int MaxHP { get; set; } = 60;
    public List<BredString> BrdBuf { get; set; } = new();
    #endregion

    #region static properties
    public static int MaxAbilChange { get; set; } = 3;
    public static int MaxCureCount { get; set; } = 5;
    public static int SeedDmg { get; set; } = 5;
    public static int MaxSeedTurn { get; set; } = 4;
    public static int MaxFoodCount { get; set; } = 6;
    public static double CritDmg { get; set; } = 1.5;
    public static int InsBufQty { get; set; } = 3;
    #endregion

    #region private fields
    private int _seedCount = 0;
    public int _changeableAbilCount = 0;
    private static readonly double[] playerBufCap = new[] { 0.25, 0.28571429, 0.33333333, 0.4, 0.5, 0.66666666, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0 };
    #endregion

    #region constructors
    public Player(string name, Ability abil) => (Name, Ability, HP) = (name, abil, MaxHP);
    public Player() : this("じぶん", AbilityFactory.Create("n")!) { }
    #endregion

    #region enums
    [Flags]
    public enum PlayerState
    {
        Normal = 0,
        Poison = 1,
        Seed = 2
    }
    #endregion

    #region methods and change status
    public bool TryChangeAbil(Ability abil)
    {
        if (_changeableAbilCount < MaxAbilChange)
        {
            Ability = abil;
            _changeableAbilCount++;
            return true;
        }
        return false;
    }
    public string PlayerStateToString()
    {
        return State switch
        {
            PlayerState.Poison | PlayerState.Seed => "毒, やどりぎ",
            PlayerState.Poison => "毒",
            PlayerState.Seed => "やどりぎ",
            _ => "なし"
        };
    }
    public string GetStatusString()
    {
        var seedTurn = State.HasFlag(PlayerState.Seed) ? MaxSeedTurn - _seedCount : 0;
        var currentWordString = CurrentWord.Name == string.Empty ? "(なし)" : CurrentWord.ToString();
        return $"{Name}:\n"
             + $"         HP:   {HP}/{MaxHP},    残り食べ物回数:    {MaxFoodCount - FoodCount}回,          状態: [{PlayerStateToString()}]\n\n"
             + $"         ATK:  {ATK}倍,      残り医療回数:    {MaxCureCount - CureCount}回,        現在の単語: {currentWordString}\n\n"
             + $"         DEF:  {DEF}倍,      毒のダメージ: {PoisonDmg}ダメージ,        とくせい: {Ability.ToString()}\n\n"
             + $"         残りとくせい変更回数: {MaxAbilChange - _changeableAbilCount}回,    残りやどりぎターン: {seedTurn}ターン\n\n";
    }
    public void Attack(Player other, double damage)
    {
        other.HP -= (int)damage;
    }
    public void Poison()
    {
        State |= PlayerState.Poison;
        PoisonDmg = 0;
    }
    public void DePoison()
    {
        State &= ~PlayerState.Poison;
        PoisonDmg = 0;
    }
    public void Seed()
    {
        State |= PlayerState.Seed;
        _seedCount = 0;
    }
    public void DeSeed()
    {
        State &= ~PlayerState.Seed;
        _seedCount = 0;
    }
    public void TakePoisonDmg()
    {
        // NOTE: ダメージ算出はかりうむ式。
        PoisonDmg += (int)(MaxHP * 0.062);
        HP -= PoisonDmg;
    }
    public void TakeSeedDmg(Player other)
    {
        HP -= SeedDmg;
        var otherHPResult = other.HP + SeedDmg;
        if (otherHPResult > other.MaxHP) other.HP = other.MaxHP;
        else other.HP = otherHPResult;
        if (!IsSeedInfinite) _seedCount++;
        if (_seedCount > MaxSeedTurn) State &= ~PlayerState.Seed;
    }
    public void Heal(bool isCure)
    {
        if (isCure)
        {
            var resultHPCure = HP + 40;
            HP = resultHPCure <= MaxHP ? resultHPCure : MaxHP;
            if (!IsCureInfinite) CureCount++;
            return;
        }
        var resultHPFood = HP + 20;
        HP = resultHPFood <= MaxHP ? resultHPFood : MaxHP;
        FoodCount++;
    }
    public void Kill() => HP = 0;
    public void Endure() => HP = 1;
    public void ModifyMaxHP() => HP = MaxHP;
    public bool TryChangeATK(int arg, Word word)
    {
        var resultIndex = ATKIndex + arg;
        if (resultIndex < 0 || ATKIndex == playerBufCap.Length - 1) return false;
        else if (resultIndex >= playerBufCap.Length - 1) ATKIndex = playerBufCap.Length - 1;
        else ATKIndex = resultIndex;
        CurrentWord = word;
        return true;
    }
    public bool TryChangeDEF(int arg, Word word)
    {
        var resultIndex = DEFIndex + arg;
        if (resultIndex < 0 || resultIndex > playerBufCap.Length - 1) return false;
        DEFIndex = resultIndex;
        CurrentWord = word;
        return true;
    }
    public void Rev(Player other)
    {
        var thisDifATK = ATKIndex - 6;
        var thisDifDEF = DEFIndex - 6;
        var otherDifATK = other.ATKIndex - 6;
        var otherDifDEF = other.DEFIndex - 6;
        ATKIndex = 6 - thisDifATK;
        DEFIndex = 6 - thisDifDEF;
        other.ATKIndex = 6 - otherDifATK;
        other.DEFIndex = 6 - otherDifDEF;
    }
    public void WZ(Player other)
    {
        ATKIndex = 6;
        DEFIndex = 6;
        other.ATKIndex = 6;
        other.DEFIndex = 6;
    }

    public Player Clone()
    {
        return (Player)MemberwiseClone();
    }
    #endregion

    #region minor classes
    internal class BredString
    {
        public string Name { get; init; } = string.Empty;
        public int Rep { get; private set; } = 0;
        public BredString(string name) => (Name, Rep) = (name, 0);
        public void Increment() => Rep++;
    }
    #endregion
}
