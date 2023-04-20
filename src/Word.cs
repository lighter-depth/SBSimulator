using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SBSimulator.src.Player;
using static SBSimulator.src.Word.WordType;

namespace SBSimulator.src
{
    internal class Word
    {
        #region properties
        public string Name { get; init; } = "\0";
        public WordType Type1 { get; init; } = Empty;
        public WordType Type2 { get; init; } = Empty;
        public Player? User { get; init; }
        public int Length => Name.Length;
        public bool IsHeal => ContainsType(Food) || ContainsType(Health);
        public bool IsPoison => !IsHeal && ContainsType(Bug) && User?.Ability == PlayerAbility.Dokubari;
        public bool IsSeed => !IsHeal && ContainsType(Plant) && User?.Ability == PlayerAbility.Yadorigi;
        public bool IsRev => !IsHeal && ContainsType(Play) && User?.Ability == PlayerAbility.Kakumei;
        public bool IsWZ => !IsHeal && ContainsType(Weather) && User?.Ability == PlayerAbility.WZ;
        public bool IsBuf => !IsHeal && User?.Ability switch
        {
            PlayerAbility.Jounetsu => ContainsType(Emote),
            PlayerAbility.RocknRoll => ContainsType(Art),
            PlayerAbility.Kachikochi => ContainsType(Mech),
            PlayerAbility.Sakinobashi => ContainsType(Time),
            PlayerAbility.Busou => ContainsType(Work),
            PlayerAbility.Kasanegi => ContainsType(Cloth),
            PlayerAbility.Keisan => ContainsType(WordType.Math),
            PlayerAbility.Training => ContainsType(Sports),
            _ => false
        };
        public bool IsATKBuf => !IsHeal && User?.Ability switch
        {
            PlayerAbility.Jounetsu => ContainsType(Emote),
            PlayerAbility.RocknRoll => ContainsType(Art),
            PlayerAbility.Busou => ContainsType(Work),
            PlayerAbility.Keisan => ContainsType(WordType.Math),
            PlayerAbility.Training => ContainsType(Sports),
            _ => false
        };
        public bool IsDEFBuf => !IsHeal && User?.Ability switch
        {
            PlayerAbility.Kachikochi => ContainsType(Mech),
            PlayerAbility.Sakinobashi => ContainsType(Time),
            PlayerAbility.Kasanegi => ContainsType(Cloth),
            _ => false
        };
        #endregion

        #region private fields
        private static readonly int[,] effList;
        private static readonly WordType[] typeIndex;
        #endregion

        #region constructors
        public Word(string name, WordType type1, WordType type2 = Empty)
        {
            Name = name;
            Type1 = type1;
            Type2 = type2;
        }
        public Word(string name, Player user, WordType type1, WordType type2 = Empty)
        {
            Name = name;
            User = user;
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
        public void Deconstruct(out string name, out WordType type1, out WordType type2)
        {
            name = Name;
            type1 = Type1;
            type2 = Type2;
        }
        public override string ToString()
        {
            return Name + " " + Type1.TypeToString() + " " + Type2.TypeToString();
        }
        public double CalcAmp(Word other)
        {
            var result = CalcAmp(Type1, other.Type1) * CalcAmp(Type1, other.Type2) * CalcAmp(Type2, other.Type1) * CalcAmp(Type2, other.Type2);
            return result;
        }
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
        public bool ContainsType(WordType type)
        {
            if (type == Empty && Type1 != Empty) return false;
            if (type == Type1 || type == Type2) return true;
            return false;
        }
        public int IsSuitable(Word prev)
        {
            if (string.IsNullOrEmpty(prev.Name))
                return 0;
            if (Name[0].IsWild() || prev.Name[^1].IsWild())
                return 0;
            if (!prev.Name[^1].WordlyEquals(Name[0]))
            {
                if (prev.Name.Length > 1 && prev.Name[^1] == 'ー' && prev.Name[^2].WordlyEquals(Name[0]))
                {
                    return 0;
                }
                if (prev.Name.Length > 1 && prev.Name[^1] == 'ー' && prev.Name[^2].IsWild())
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
        public enum WordType
        {
            Empty, Normal, Animal, Plant, Place, Emote, Art, Food, Violence, Health, Body, Mech, Science, Time, Person, Work, Cloth, Society, Play, Bug, Math, Insult, Religion, Sports, Weather, Tale
        }
        #endregion
    }
}
