﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBSimulator.src
{
    internal static class SBOptions
    {
        #region properties
        public static bool IsSeedInfinite { get; set; } = false;
        public static bool IsCureInfinite { get; set; } = false;
        public static bool IsAbilChangeable { get; set; } = true;
        public static bool IsStrict { get; set; } = true;
        public static bool IsInferable { get; set; } = true;
        #endregion
        public enum SBMode
        {
            Empty, Default, Classic, AgeOfSeed
        }
    }
}