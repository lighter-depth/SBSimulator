using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBSimulator.Source;

// CPU 機能の埋め込み回避用テストクラス。
[Obsolete("作成中です", true)]
internal abstract class SiritoriMode
{
    public abstract TurnProceedingArbiter Player1Proceeds { get; }
    public abstract TurnProceedingArbiter Player2Proceeds { get; }
    public abstract int Player1MaxHP { get; }
    public abstract int Player2MaxHP { get; }
}

