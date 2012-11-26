namespace Chutzpah.Coverage
{
    /// <summary>
    /// This class only exists so that the caller doesn't have to have a reference to StructureMap.
    /// </summary>
    public class CoverageEngineFactory
    {
        public static ICoverageEngine GetCoverageEngine()
        {
            return ChutzpahContainer.Current.GetInstance<ICoverageEngine>();
        }
    }
}
