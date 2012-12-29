namespace Chutzpah.Compilers.ActiveScript
{
    public enum ScriptStatistic : uint
    {
        /// <summary>
        /// Return the number of statements executed since the script started or the statistics were reset.
        /// </summary>
        StatementCount = 1,
        InstructionCount = 2,
        InstructionTime = 3,
        TotalTime = 4,
    }
}