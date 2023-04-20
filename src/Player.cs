using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SBSimulator.src.Word;
using static SBSimulator.src.Player.PlayerAbility;
using static SBSimulator.src.SBOptions;

namespace SBSimulator.src
{
    internal class Player
    {
        #region instance properties
        public string Name { get; init; } = "じぶん";
        public int HP { get; private set; }
        public double ATK => playerBufCap[ATKIndex];
        public int ATKIndex { get; private set; } = 6;
        public double DEF => playerBufCap[DEFIndex];
        public int DEFIndex { get; private set; } = 6;

        public Word CurrentWord { get; private set; } = new Word(string.Empty, WordType.Empty);
        public PlayerAbility Ability { get; private set; } = Empty;
        public int FoodCount { get; private set; } = 0;
        public int CureCount { get; private set; } = 0;
        public bool IsPoisoned { get; private set; } = false;
        public int PoisonDmg { get; private set; } = 0;
        public bool IsSeeded { get; private set; } = false;
        public int MaxHP { get; set; } = 60;
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
        private List<BredString> _brdBuf = new();
        #endregion

        #region constructors
        public Player(string name, PlayerAbility abil) => (Name, Ability, HP) = (name, abil, MaxHP);
        public Player() : this("じぶん", Empty) { }
        #endregion

        #region enums
        public enum PlayerAbility
        {
            Empty, Debugger, Hanshoku, Yadorigi, Global, Jounetsu, RocknRoll, Ikasui, MukiMuki, Ishoku, Karate, Kachikochi, Jikken, Sakinobashi, Kyojin, Busou, Kasanegi, Hoken, Kakumei, Dokubari, Keisan, Zuboshi, Shinkoushin, Training, WZ, Oremoji
        }
        #endregion

        #region methods and change status
        public bool TryChangeAbil(PlayerAbility abil)
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
            return (IsPoisoned, IsSeeded) switch
            {
                (true, true) => "毒, やどりぎ",
                (true, false) => "毒",
                (false, true) => "やどりぎ",
                (false, false) => "なし"
            };
        }
        public string GetStatusString()
        {
            var seedTurn = IsSeeded ? MaxSeedTurn - _seedCount : 0;
            var currentWordString = CurrentWord.Name == string.Empty ? "(なし)" : CurrentWord.ToString();
            return $"{Name}:\n"
                 + $"         HP:   {HP}/{MaxHP},    残り食べ物回数:    {MaxFoodCount - FoodCount}回,          状態: [{PlayerStateToString()}]\n\n"
                 + $"         ATK:  {ATK}倍,      残り医療回数:    {MaxCureCount - CureCount}回,        現在の単語: {currentWordString}\n\n"
                 + $"         DEF:  {DEF}倍,      毒のダメージ: {PoisonDmg}ダメージ,        とくせい: {Ability.AbilToString()}\n\n"
                 + $"         残りとくせい変更回数: {MaxAbilChange - _changeableAbilCount}回,    残りやどりぎターン: {seedTurn}ターン\n\n";
        }
        public bool AttackAndTryCrit(Player other, Word word)
        {
            CurrentWord = word;
            var critDmg = CritDamage(word);
            var baseDamage = BaseDamage(word) * word.CalcAmp(other.CurrentWord);
            double damage;
            if (critDmg == 1)
                damage = baseDamage * ATK / other.DEF * BufAbilDamage(word) * BreedDamage(word) * critDmg;
            else
            {
                if (ATK < 1)
                    damage = baseDamage * BufAbilDamage(word) * BreedDamage(word) * critDmg;
                else
                    damage = baseDamage * ATK * BufAbilDamage(word) * BreedDamage(word) * critDmg;
            }
            if (!(CurrentWord.Type1 == WordType.Empty || other.CurrentWord.Type1 == WordType.Empty))
                damage *= 0.85 + new Random().Next(15) * 0.01;
            other.HP -= (int)damage;
            return critDmg != 1;
        }
        public void Poison()
        {
            IsPoisoned = true;
            PoisonDmg = 0;
        }
        public void DePoison() => IsPoisoned = false;
        public void Seed(Player other, Word word)
        {
            other.CurrentWord = word;
            IsSeeded = true;
            _seedCount = 0;
        }
        public void TakePoisonDmg()
        {
            PoisonDmg += new Random().Next(2) == 0 ? 3 : 4;
            HP -= PoisonDmg;
        }
        public void TakeSeedDmg(Player other)
        {
            HP -= SeedDmg;
            var otherHPResult = other.HP + SeedDmg;
            if (otherHPResult > other.MaxHP) other.HP = other.MaxHP;
            else other.HP = otherHPResult;
            if (!IsSeedInfinite) _seedCount++;
            if (_seedCount > MaxSeedTurn) IsSeeded = false;
        }
        public bool TryHeal(Word word)
        {
            CurrentWord = word;
            if (word.ContainsType(WordType.Food))
            {
                if (Ability == Ishoku && (CureCount < MaxCureCount || IsCureInfinite))
                {
                    var resultHP = HP + 40;
                    HP = resultHP <= MaxHP ? resultHP : MaxHP;
                    if (!IsCureInfinite) CureCount++;
                    return true;
                }
                if (FoodCount < MaxFoodCount || Ability == Ikasui)
                {
                    var resultHP = HP + 20;
                    HP = resultHP <= MaxHP ? resultHP : MaxHP;
                    FoodCount++;
                    return true;
                }
                return false;
            }
            if (word.ContainsType(WordType.Health))
            {
                if (CureCount < MaxCureCount || IsCureInfinite)
                {
                    var resultHP = HP + 40;
                    HP = resultHP <= MaxHP ? resultHP : MaxHP;
                    if (!IsCureInfinite) CureCount++;
                    return true;
                }
                return false;
            }
            return false;
        }
        public void Kill() => HP = 0;
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
        #endregion

        #region damage calculation
        int BaseDamage(Word word)
        {
            if (word.Type1 == WordType.Empty) return Ability == Debugger ? 13 : 7;
            return 10;
        }
        double BufAbilDamage(Word word)
        {
            return Ability == Global && word.ContainsType(WordType.Place) ? 1.5
                : Ability == Jikken && word.ContainsType(WordType.Science) ? 1.5
                : Ability == Kyojin && word.ContainsType(WordType.Person) ? 1.5
                : Ability == Shinkoushin && word.ContainsType(WordType.Religion) ? 1.5
                : Ability == Oremoji && word.Length >= 7 ? 2
                : Ability == Oremoji && word.Length == 6 ? 1.5
                : 1;
        }
        int BreedDamage(Word word)
        {
            int damage = 1;
            var brdBufNames = _brdBuf.Select(x => x.Name).ToList();
            if (brdBufNames.Contains(word.Name))
            {
                if (Ability == Hanshoku) damage = _brdBuf[brdBufNames.IndexOf(word.Name)].Rep + 1;
                _brdBuf[brdBufNames.IndexOf(word.Name)].Increment();
            }
            else
                _brdBuf.Add(new BredString(word.Name));
            return damage;
        }
        double CritDamage(Word word)
        {
            return Ability == Karate && word.ContainsType(WordType.Body) ? CritDmg
                : Ability == Zuboshi && word.ContainsType(WordType.Insult) ? CritDmg
                : word.ContainsType(WordType.Body) || word.ContainsType(WordType.Insult) && new Random().Next(5) == 0 ? CritDmg
                : 1;
        }
        #endregion

        #region minor classes
        class BredString
        {
            public string Name { get; init; } = string.Empty;
            public int Rep { get; private set; } = 0;
            public BredString(string name) => (Name, Rep) = (name, 0);
            public void Increment() => Rep++;
        }
        #endregion
    }
}
