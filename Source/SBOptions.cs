namespace SBSimulator.Source;

/// <summary>
/// ゲーム オプションを管理するクラスです。
/// </summary>
internal static class SBOptions
{
    #region properties
    /// <summary>
    /// やどりぎが永続するかどうかを表すフラグです。
    /// </summary>
    public static bool IsSeedInfinite { get; set; } = false;
    /// <summary>
    /// 医療タイプの単語による回復が無限に使用可能かどうかを表すフラグです。
    /// </summary>
    public static bool IsCureInfinite { get; set; } = false;
    /// <summary>
    /// とくせいの変更が可能かどうかを表すフラグです。
    /// </summary>
    public static bool IsAbilChangeable { get; set; } = true;
    /// <summary>
    /// ストリクト モードが有効かどうかを表すフラグです。
    /// </summary>
    public static bool IsStrict { get; set; } = true;
    /// <summary>
    /// タイプ推論が有効かどうかを表すフラグです。
    /// </summary>
    public static bool IsInferable { get; set; } = true;
    /// <summary>
    /// カスタムとくせいが使用可能かどうかを表すフラグです。
    /// </summary>
    public static bool IsCustomAbilUsable { get; set; } = true;
    /// <summary>
    /// CPUの行動に待ち時間を設けるかを表すフラグです。
    /// </summary>
    public static bool IsCPUDelayEnabled { get; set; } = true;
    #endregion

    /// <summary>
    /// モードの種類を表す列挙型です。
    /// </summary>
    public enum SBMode
    {
        Empty, 
        Default,
        Classic, 
        AgeOfSeed
    }
}
