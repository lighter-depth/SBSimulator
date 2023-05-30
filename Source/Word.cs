using static SBSimulator.Source.Player;
using static SBSimulator.Source.Word.WordType;

namespace SBSimulator.Source;

/// <summary>
/// 単語の情報を管理するクラスです。
/// </summary>
internal class Word
{
    #region properties
    /// <summary>
    /// 単語の名前
    /// </summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>
    /// 単語の第一タイプ
    /// </summary>
    public WordType Type1 { get; init; } = Empty;
    /// <summary>
    /// 単語の第二タイプ
    /// </summary>
    public WordType Type2 { get; init; } = Empty;
    /// <summary>
    /// 単語の使用者
    /// </summary>
    public Player? User { get; init; }
    /// <summary>
    /// 単語の被使用者
    /// </summary>
    public Player? Receiver { get; init; }
    /// <summary>
    /// 単語の長さ
    /// </summary>
    public int Length => Name.Length;
    /// <summary>
    /// 最後の文字
    /// </summary>
    public char LastChar 
    {
        get 
        {
            var siritoriChar = new Dictionary<char, char>
            {
                ['ゃ'] = 'や',
                ['ゅ'] = 'ゆ',
                ['ょ'] = 'よ',
                ['っ'] = 'つ',
                ['ぁ'] = 'あ',
                ['ぃ'] = 'い',
                ['ぅ'] = 'う',
                ['ぇ'] = 'え',
                ['ぉ'] = 'お',
                ['を'] = 'お',
                ['ぢ'] = 'じ',
                ['づ'] = 'ず'
            };
            return (Name.Length == 0
             || Name.Length == 1 && Name[0] == 'ー') ? '\0'
             : siritoriChar.ContainsKey(Name[^1]) ? siritoriChar[Name[^1]]
             : Name[^1] == 'ー' && siritoriChar.ContainsKey(Name[^2]) ? siritoriChar[Name[^2]]
             : Name[^1] == 'ー' ? Name[^2]
             : Name[^1];

        } 
    }
    /// <summary>
    /// 単語が回復作用を持つかどうかを表すフラグ
    /// </summary>
    public bool IsHeal => ContainsType(Food) || ContainsType(Health);
    /// <summary>
    /// 単語が急所作用を持つかどうかを表すフラグ
    /// </summary>
    public bool IsCritable => ContainsType(Body) || ContainsType(Insult);
    /// <summary>
    /// 単語がやどりぎを植える作用を持つかどうかを表すフラグ
    /// </summary>
    public bool IsSeed => !IsHeal && User?.Ability is ISeedable isd && ContainsType(isd.SeedType) && Receiver?.State.HasFlag(PlayerState.Seed) is false;
    /// <summary>
    /// 単語がバフ特性を実行可能かどうかを表すフラグ
    /// </summary>
    public bool IsBuf => !IsHeal && User?.Ability is ISingleTypedBufAbility it && ContainsType(it.BufType);
    /// <summary>
    /// 単語が暴力タイプによる攻撃力低下を引き起こすかどうかを表すフラグ
    /// </summary>
    public bool IsViolence => !IsHeal && ContainsType(Violence);
    #endregion

    #region constants
    public const int NUMBER_OF_TYPES = 25;
    #endregion

    #region private fields
    /// <summary>
    /// タイプ相性を表す二次元配列
    /// </summary>
    private static readonly int[,] effList;
    /// <summary>
    /// <see cref="effList"/>から要素を読みだすための配列
    /// </summary>
    private static readonly WordType[] typeIndex;
    #endregion

    #region constructors
    public Word(string name, WordType type1, WordType type2 = Empty)
    {
        Name = name;
        Type1 = type1;
        Type2 = type2;
    }
    public Word(string name, Player user, Player receiver, WordType type1, WordType type2 = Empty)
    {
        Name = name;
        User = user;
        Receiver = receiver;
        Type1 = type1;
        Type2 = type2;
    }
    public Word() : this(string.Empty, Empty) { }
    static Word()
    {
        // 0: Normal, 1: Effective, 2: Not Effective, 3: No Damage
        effList = new int[,]
        {
            { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 3, 1, 1, 1, 1, 3, 3, 2, 1, 1, 1 }, // Violence
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Food
            { 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Place
            { 1, 0, 0, 2, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0 }, // Society
            { 2, 1, 0, 0, 2, 0, 1, 2, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 }, // Animal
            { 2, 0, 0, 1, 0, 2, 0, 0, 0, 0, 0, 0, 0, 1, 0, 3, 0, 0, 2, 2, 0, 0, 2, 0, 0 }, // Emotion
            { 0, 1, 1, 0, 2, 0, 2, 0, 2, 0, 2, 2, 0, 1, 1, 2, 0, 0, 0, 0, 0, 2, 0, 0, 0 }, // Plant
            { 0, 0, 0, 0, 0, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 2, 0, 0 }, // Science
            { 2, 2, 0, 0, 0, 0, 1, 0, 2, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 }, // Playing
            { 2, 0, 0, 2, 2, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0 }, // Person
            { 2, 0, 0, 0, 0, 0, 1, 0, 2, 0, 2, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Clothing
            { 2, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Work
            { 2, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0 }, // Art
            { 2, 1, 0, 0, 2, 0, 2, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Body
            { 0, 1, 0, 0, 0, 0, 2, 0, 2, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Time
            { 2, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Machine
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // Health
            { 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 1, 0, 0, 0, 0, 0, 0 }, // Tale
            { 2, 2, 0, 1, 2, 0, 2, 0, 1, 1, 1, 0, 1, 1, 0, 2, 0, 0, 1, 1, 3, 2, 2, 1, 0 }, // Insult
            { 0, 0, 0, 0, 0, 2, 0, 1, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0 }, // Math
            { 1, 1, 1, 1, 0, 1, 2, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 2, 2, 0, 1, 0 }, // Weather
            { 1, 1, 0, 0, 1, 0, 1, 2, 0, 0, 2, 0, 0, 1, 0, 2, 1, 0, 1, 0, 0, 2, 0, 0, 0 }, // Bug
            { 2, 0, 1, 1, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 1, 0, 1, 0, 2, 0, 0 }, // Religion
            { 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 2, 0 }, // Sports
            { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }  // Normal  
        };
        typeIndex = new WordType[] { Violence, Food, Place, Society, Animal, Emote, Plant, Science, Play, Person, Cloth, Work, Art, Body, Time, Mech, Health, Tale, Insult, WordType.Math, Weather, Bug, Religion, Sports, Normal, Empty };
    }
    #endregion

    #region methods
    public override string ToString()
    {
        return Name + " " + Type1.TypeToString() + " " + Type2.TypeToString();
    }
    /// <summary>
    /// 指定された単語同士のタイプ相性を計算します。
    /// </summary>
    /// <param name="other">攻撃を受ける単語</param>
    /// <returns>タイプ相性によるダメージ倍率</returns>
    public double CalcAmp(Word other)
    {
        var result = CalcAmp(Type1, other.Type1) * CalcAmp(Type1, other.Type2) * CalcAmp(Type2, other.Type1) * CalcAmp(Type2, other.Type2);
        return result;
    }
    /// <summary>
    /// 指定したタイプ同士の相性を計算します。
    /// </summary>
    /// <param name="t1">攻撃側のタイプ</param>
    /// <param name="t2">防御側のタイプ</param>
    /// <returns>タイプ相性によるダメージ倍率</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static double CalcAmp(WordType t1, WordType t2)
    {
        if (t1 == Empty || t2 == Empty) return 1;
        var t1Index = Array.IndexOf(typeIndex, t1);
        var t2Index = Array.IndexOf(typeIndex, t2);
        return effList[t1Index, t2Index] switch
        {
            0 => 1,
            1 => 2,
            2 => 0.5,
            3 => 0,
            _ => throw new ArgumentOutOfRangeException($"パラメーター{effList[t1Index, t2Index]} は無効です。")
        };
    }
    /// <summary>
    /// 単語が指定したタイプを含むかどうかを判定します。
    /// </summary>
    /// <param name="type">判定する<see cref="WordType"/>型のインスタンス</param>
    /// <returns>指定したタイプを含むかどうかを表すフラグ</returns>
    public bool ContainsType(WordType type)
    {
        if (type == Empty && Type1 != Empty) return false;
        if (type == Type1 || type == Type2) return true;
        return false;
    }
    /// <summary>
    /// 単語を指定した単語に続けて出したとき、しりとりのルールに適するかを判定します。
    /// </summary>
    /// <param name="prev">指定する単語</param>
    /// <returns>続けて出せるかどうかを表すフラグ</returns>
    public int IsSuitable(Word prev)
    {
        if (string.IsNullOrWhiteSpace(prev.Name))
            return 0;
        if (Name[0].IsWild() || prev.Name[^1].IsWild())
            return 0;
        if (!prev.Name[^1].WordlyEquals(Name[0]))
        {
            if (prev.Name.Length > 1 && prev.Name[^1] == 'ー' && prev.Name[^2].WordlyEquals(Name[0])
             || prev.Name.Length > 1 && prev.Name[^1] == 'ー' && prev.Name[^2].IsWild())
                return 0;
            return 1;

        }
        if (Name[^1] == 'ん')
        {
            return -1;
        }
        return 0;
    }
    #endregion

    #region enums
    /// <summary>
    /// 単語のタイプを表します。
    /// </summary>
    public enum WordType
    {
        Empty, Normal, Animal, Plant, Place, Emote, Art, Food, Violence, Health, Body, Mech, Science, Time, Person, Work, Cloth, Society, Play, Bug, Math, Insult, Religion, Sports, Weather, Tale
    }
    #endregion
}
