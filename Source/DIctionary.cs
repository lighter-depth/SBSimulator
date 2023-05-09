﻿using static SBSimulator.Source.Word;

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
    public static Dictionary<string, List<WordType>> PerfectNoTypeDic
    {
        get
        {
            var temp = new Dictionary<string, List<WordType>>();
            var noTypeTemp = NoTypeWords.Concat(NoTypeWordEx).ToList();
            foreach (var i in noTypeTemp)
                temp.Add(i, new() { WordType.Empty, WordType.Empty });
            return temp;
        }
    }
}

