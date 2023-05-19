using static SBSimulator.Source.Word;

namespace SBSimulator.Source;

internal class SBDictionary
{
    /// <summary>
    /// タイプ無し単語の情報
    /// </summary>
    public static List<string> NoTypeWords { get; set; } = new();
    /// <summary>
    /// 拡張タイプ無し単語の情報
    /// </summary>
    public static List<string> NoTypeWordEx { get; set; } = new();
    /// <summary>
    /// タイプ付き単語の情報
    /// </summary>
    public static Dictionary<string, List<WordType>> TypedWords { get; set; } = new();
    /// <summary>
    /// タイプ無し単語の完全な辞書
    /// </summary>
    public static Dictionary<string, List<WordType>> PerfectNoTypeDic => _perfectNoTypeDic ??= GetPerfectNoTypeDic();
    #region perfect notype dictionary
    static Dictionary<string, List<WordType>>? _perfectNoTypeDic;
    /// <summary>
    /// タイプ無し単語の完全な辞書を作成します。
    /// </summary>
    /// <returns>作成された辞書</returns>
    static Dictionary<string, List<WordType>> GetPerfectNoTypeDic()
    {
        var temp = new Dictionary<string, List<WordType>>();
        var noTypeTemp = NoTypeWords.Concat(NoTypeWordEx).ToList();
        foreach (var i in noTypeTemp)
            temp.Add(i, new() { WordType.Empty, WordType.Empty });
        return temp;
    }
    #endregion
    public static Dictionary<string, List<WordType>> PerfectDic => _perfectDic ??= GetPerfectDic();
    static Dictionary<string, List<WordType>>? _perfectDic;
    static Dictionary<string, List<WordType>> GetPerfectDic()
    {
        return TypedWords.Concat(PerfectNoTypeDic.Where(p => !TypedWords.ContainsKey(p.Key))).ToDictionary(p => p.Key, p => p.Value);
    }
    public static List<string> PerfectNameDic => _perfectNameDic ??= GetPerfectNameDic();
    static List<string>? _perfectNameDic;
    static List<string> GetPerfectNameDic()
    {
        return PerfectDic.Keys.ToList();
    }
}

