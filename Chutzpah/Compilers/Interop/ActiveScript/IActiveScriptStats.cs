using System;
using System.Runtime.InteropServices;

namespace Chutzpah.Compilers.ActiveScript
{
    /// <summary>
    /// Allows a host to query the statistics of a running script. The host can use this information to determine
    /// if script has taken too long to complete.
    /// </summary>
    [Guid("B8DA6310-E19B-11d0-933C-00A0C90DCAA9")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IActiveScriptStats
    {
        /// <summary>
        /// Returns one of the standard script statistics.
        /// </summary>
        /// <param name="statId">Specifies which statistic to return.</param>
        /// <param name="valueHi">The high 32 bits of a 64-bit unsigned integer representing the statistic.</param>
        /// <param name="valueLow">The low 32 bits of a 64-bit unsigned integer representing the statistic.</param>
        void GetStat(ScriptStatistic statId, out uint valueHi, out uint valueLow);

        /// <summary>
        /// Returns a custom script statistic.
        /// </summary>
        /// <param name="statId">Specifies which statistic to return.</param>
        /// <param name="valueHi">The high 32 bits of a 64-bit unsigned integer representing the statistic.</param>
        /// <param name="valueLow">The low 32 bits of a 64-bit unsigned integer representing the statistic.</param>
        void GetStatEx(Guid statId, out uint valueHi, out uint valueLow);

        /// <summary>
        /// Resets the statistics for this script.
        /// </summary>
        void ResetStats();
    }
}