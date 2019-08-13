namespace Chutzpah.VisualStudioContextMenu
{
    // EnvDte.Constants cannot be embedded.  
    // The next best solution is to manually add these to the project.

    internal class DTEConstants
    {
        internal static string vsWindowKindSolutionExplorer = "{3AE79031-E1BC-11D0-8F78-00A0C9110057}";
        internal static string vsDocumentKindText = "{8E7B96A8-E33D-11D0-A6D5-00C04FB67F6A}";
        internal static object vsProjectItemKindPhysicalFile = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";
        internal static object vsProjectItemKindPhysicalFolder = "{6BB5F8EF-4483-11D3-8BCF-00C04F8EC28C}";
    }
}