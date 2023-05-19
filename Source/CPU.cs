using static SBSimulator.Source.Word;
using static SBSimulator.Source.SBExtention;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace SBSimulator.Source;

/// <summary>
/// プレイヤーがターンを先攻するかどうかを決定します。
/// </summary>
internal enum TurnProceedingArbiter
{
    /// <summary>
    /// プレイヤーが先攻かどうかはランダムに決定されます。
    /// </summary>
    Random,
    /// <summary>
    /// プレイヤーは必ず先攻になります。
    /// </summary>
    True,
    /// <summary>
    /// プレイヤーは必ず後攻になります。
    /// </summary>
    False
}
/// <summary>
/// <see cref="CPUPlayer"/>クラスのインスタンスを作成するファクトリ クラスです。
/// </summary>
internal class CPUFactory
{
    /// <summary>
    /// 文字列からCPUを生成します。
    /// </summary>
    /// <param name="name">生成に使用する文字列</param>
    /// <returns>文字列から推論された<see cref="CPUPlayer"/>クラスのインスタンス</returns>
    public static CPUPlayer? Create(string name)
    {
        var subClasses = Assembly.GetAssembly(typeof(CPUPlayer))?.GetTypes().Where(x => x.IsSubclassOf(typeof(CPUPlayer)) && !x.IsAbstract).ToArray() ?? Array.Empty<Type>();
        foreach (var i in subClasses)
        {
            var sub = Activator.CreateInstance(i) as CPUPlayer; ;
            if (sub?.ReferedName.Contains(name) == true)
            {
                sub.Name = sub.CPUName;
                sub.Ability = sub.FirstAbility;
                return sub;
            }
        }
        return null;
    }
}

/// <summary>
/// コンピューターにより行動ルーチンを決定するプレイヤーを表すスーパークラスです。
/// </summary>
internal abstract class CPUPlayer : Player
{
    /// <summary>
    /// CPUのプレイヤーに設定する名前
    /// </summary>
    public abstract string CPUName { get; set; }
    /// <summary>
    /// CPUのプレイヤーを生成するときに参照する名前
    /// </summary>
    public abstract List<string> ReferedName { get; }
    /// <summary>
    /// CPUの初期とくせい
    /// </summary>
    public virtual Ability FirstAbility => new Debugger();
    static readonly int _millisecondsDelay = 1800;
    public CPUPlayer(string name, Ability ability) : base(name, ability) { }
    public CPUPlayer() : base() { }
    /// <summary>
    /// CPUの思考ルーチンを実行します。
    /// </summary>
    /// <returns><see cref="Battle"/>クラスへの命令を表す文字列</returns>
    public string[] Execute()
    {
        return Task.Run(executeAsync).GetAwaiter().GetResult();
        async Task<string[]> executeAsync()
        {
            var timer = Parent?.IsCPUDelayEnabled == true ? Task.Delay(_millisecondsDelay) : Task.Run(() => { });
            var result = await Task.Run(() => Execute(0));
            await timer;
            return result;
        }
    }
    /// <summary>
    /// CPUの思考ルーチンを実行します。
    /// </summary>
    /// <returns><see cref="Battle"/>クラスへの命令を表す文字列</returns>
    public abstract string[] Execute(params int[] args);
    /// <summary>
    /// 指定した条件に合う単語を検索します。
    /// </summary>
    /// <param name="startChar">最初の文字の指定</param>
    /// <param name="type">タイプの指定</param>
    /// <param name="word">出力された<see cref="Word"/>クラスのインスタンス</param>
    /// <returns>単語が見つかったかどうかを表すフラグ</returns>
    public bool TryWordSearchByType(char startChar, WordType type, [NotNullWhen(true)]out Word? word)
    {
        word = null;
        if (Parent is null) return false;
        word = new();
        var resultWords = SBDictionary.TypedWords.Where(x => x.Key[0] == startChar && x.Value.Contains(type) && !Parent.UsedWords.Contains(x.Key)).ToDictionary(p => p.Key, p => p.Value);
        var random = new Random().Next(resultWords.Count);
        if (resultWords.Count != 0 && Parent.TryInferWordTypes(resultWords.ElementAt(random).Key, out var wordTemp))
        {
            word = wordTemp;
            return true;
        }
        return false;
    }
    /// <summary>
    /// 指定した条件に合う単語を検索します。
    /// </summary>
    /// <param name="startChar">最初の文字の指定</param>
    /// <param name="type1">タイプ1の指定</param>
    /// <param name="type2">タイプ1の指定</param>
    /// <param name="word">出力された<see cref="Word"/>クラスのインスタンス</param>
    /// <returns>単語が見つかったかどうかを表すフラグ</returns>
    public bool TryWordSearchByType(char startChar, WordType type1, WordType type2, [NotNullWhen(true)] out Word? word)
    {
        word = null;
        if (Parent is null) return false;
        word = new();
        var resultWords = SBDictionary.TypedWords.Where(x => x.Key[0] == startChar && x.Value.Contains(type1) && x.Value.Contains(type2) && !Parent.UsedWords.Contains(x.Key)).ToDictionary(p => p.Key, p => p.Value);
        var random = new Random().Next(resultWords.Count);
        if (resultWords.Count != 0 && Parent.TryInferWordTypes(resultWords.ElementAt(random).Key, out var wordTemp))
        {
            word = wordTemp;
            return true;
        }
        return false;
    }
    /// <summary>
    /// 指定した条件に合う単語を探します。
    /// </summary>
    /// <param name="pred">条件の指定</param>
    /// <param name="word">出力された<see cref="Word"/>クラスのインスタンス</param>
    /// <returns>単語が見つかったかどうかを表すフラグ</returns>
    public bool TryWordSearchByName(Predicate<string> pred, [NotNullWhen(true)]out Word? word)
    {
        word = null;
        if (Parent is null) return false;
        var resultWords = SBDictionary.PerfectNameDic.Where(x => pred(x) && !Parent.UsedWords.Contains(x)).ToList();
        var random = new Random().Next(resultWords.Count);
        if (resultWords.Count != 0 && Parent.TryInferWordTypes(resultWords[random], out var wordTemp))
        {
            word = wordTemp;
            return true;
        }
        return false;
    }
    public bool TrySearchKillWord(char startChar, Word prev, [NotNullWhen(true)] out Word? word)
    {
        word = null;
        var resultTypeList = new List<Word>();
        var resultList = new List<Word>();
        if(Parent is null) return false;
        for(var i = 0; i < NUMBER_OF_TYPES; i++)
        {
            for(var j = 0; j < NUMBER_OF_TYPES; j++)
            {
                var w = new Word(string.Empty, (WordType)i, (WordType)j);
                if (w.CalcAmp(prev) >= 8) resultTypeList.Add(w);
            }
        }
        if (resultTypeList.Count == 0) return false;
        foreach(var i in resultTypeList)
        {
            if (TryWordSearchByType(startChar, i.Type1, i.Type2, out var wordTemp))
                resultList.Add(wordTemp);
        }
        if(resultList.Count > 0)
        {
            word = resultList[new Random().Next(resultList.Count)];
            return true;
        }
        return false;
    }
    /// <summary>
    /// 無条件に単語を検索します。
    /// </summary>
    /// <param name="startchar">最初の文字の指定</param>
    /// <returns>出力された<see cref="Word"/>クラスのインスタンス</returns>
    public Word FindSomeWord(char startchar)
    {
        if (Parent is null) return new();
        var result = new Word();
        foreach (var i in SBDictionary.TypedWords.Keys)
        {
            if (i?[0] == startchar && !Parent.UsedWords.Contains(i))
            {
                Parent?.TryInferWordTypes(i, out result);
                return result;
            }
        }
        foreach (var i in SBDictionary.NoTypeWords)
        {
            if (i?[0] == startchar && !Parent.UsedWords.Contains(i))
            {
                result = new(i, this, Parent.OtherPlayer, WordType.Empty);
                return result;
            }
        }
        return result;
    }
    /// <summary>
    /// 単語探索時に参照する最初の文字を取得します。
    /// </summary>
    /// <returns>最初の文字</returns>
    public char GetStartChar()
    {
        return Parent is null ? GetRandomChar()
             : string.IsNullOrWhiteSpace(Parent.OtherPlayer.CurrentWord.Name) ? GetRandomChar()
             : Parent.OtherPlayer.CurrentWord.LastChar;
    }
    /// <summary>
    /// 現在相手が場に出している単語を取得します。
    /// </summary>
    /// <returns>相手が現在使用している単語の情報</returns>
    public Word GetLastWord()
    {
        return Parent is null ? new()
            : Parent.OtherPlayer.CurrentWord;
    }
}
/// <summary>
/// いたまえのAIです。
/// </summary>
internal class Itamae : CPUPlayer
{
    public override Ability FirstAbility => new Ikasui();
    public override string CPUName { get; set; } = "いたまえ";
    public override List<string> ReferedName => new() { "いたまえ", "s3", "S3" };
    public override TurnProceedingArbiter Proceeding => TurnProceedingArbiter.True;
    public override string[] Execute(params int[] args)
    {
        var startchar = GetStartChar();
        if (TryWordSearchByType(startchar, WordType.Food, out var wordFood))
        {
            return new[] { wordFood.Name };
        }
        return new[] { FindSomeWord(startchar).Name };
    }
}
/// <summary>
/// つよしのAIです。
/// </summary>
internal class Tsuyoshi : CPUPlayer
{
    public override Ability FirstAbility => new Kakumei();
    public override string CPUName { get; set; } = "つよし";
    public override List<string> ReferedName => new() { "つよし", "s11", "S11", "sa", "SA" };
    public int ViolenceUsed { get; private set; } = 0;
    public bool PlayUsed { get; private set; } = false;
    public override TurnProceedingArbiter Proceeding => TurnProceedingArbiter.True;
    public Tsuyoshi(string name, Ability ability) : base(name, ability) { }
    public Tsuyoshi() : base() { }
    public override string[] Execute(params int[] args)
    {
        var startchar = GetStartChar();
        if (ViolenceUsed < 2 && TryWordSearchByType(startchar, WordType.Violence, out var wordViolence))
        {
            ViolenceUsed++;
            return new[] { wordViolence.Name };
        }
        if (ViolenceUsed >= 2 && !PlayUsed && TryWordSearchByType(startchar, WordType.Play, out var wordPlay))
        {
            PlayUsed = true;
            return new[] { wordPlay.Name };
        }
        if (ViolenceUsed >= 2 && PlayUsed && TryWordSearchByType(startchar, WordType.Violence, out var wordViolenceAttacker))
        {
            return new[] { wordViolenceAttacker.Name };
        }
        return new[] { FindSomeWord(startchar).Name };
    }
}
/// <summary>
/// ぬ攻めを行うAiです。
/// </summary>
internal class NuzemeAI : CPUPlayer
{
    public override Ability FirstAbility => new Oremoji();
    public override string CPUName { get; set; } = "ぬぜめマン";
    public override List<string> ReferedName => new() { "ぬぜめ", "nz" };
    public NuzemeAI(string name, Ability ability) : base(name, ability) { }
    public NuzemeAI() : base() { }
    // HACK: 探索のアルゴリズムを改善したい。
    public override string[] Execute(params int[] args)
    {
        if (Parent is null) return Array.Empty<string>();
        var startchar = GetStartChar();
        var word = GetLastWord();
        if(TrySearchKillWord(startchar, Parent.OtherPlayer.CurrentWord, out var killWord))
        {
            return new[] { killWord.Name };
        }
        if (TryWordSearchByName(x => x.Length > 6 && x[0] == startchar && x[^1] == 'ぬ', out var wordNuzeme7))
        {
            return new[] { wordNuzeme7.Name };
        }
        if(TryWordSearchByName(x => x.Length == 6 && x[0] == startchar && x[^1] == 'ぬ', out var wordNuzeme6))
        {
            return new[] { wordNuzeme6.Name };
        }
        if (TryWordSearchByName(x => x.Length > 0 && x[0] == startchar && x[^1] == 'ぬ', out var wordNuzeme))
        {
            return new[] { wordNuzeme.Name };
        }
        if (TryWordSearchByName(x => x.Length > 6 && x[0] == startchar && x[^1] == 'ぐ', out var wordGuzeme7))
        {
            return new[] { wordGuzeme7.Name };
        }
        if (TryWordSearchByName(x => x.Length == 6 && x[0] == startchar && x[^1] == 'ぐ', out var wordGuzeme6))
        {
            return new[] { wordGuzeme6.Name };
        }
        if (TryWordSearchByName(x => x.Length > 0 && x[0] == startchar && x[^1] == 'ぐ', out var wordGuzeme))
        {
            return new[] { wordGuzeme.Name };
        }
        return new[] { FindSomeWord(startchar).Name };
    }
}
