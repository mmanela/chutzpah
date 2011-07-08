using System;

namespace Chutzpah.VisualStudio
{
    internal static class GuidList
    {
        public const string guidChutzpahPkgString = "0a3da9d8-a5b6-4fb9-b423-ef87ff203628";


        public const string guidChutzpahCmdSetString = "F0D5D16A-56CE-40A2-9C34-B890D6961CF2";
        //public const string guidSourceEditorCmdSetString = "F683BB14-1D01-444C-9B19-C40485C67824";
        //public const string guidSolutionItemCmdSetString = "3AC35B4C-E12D-40EE-9BCA-33F5B6B4191E";
        //public const string guidSolutionFolderNodeCmdSetString = "7E174869-EE8C-45FC-BEF4-5D19B6AF146D";

        public static readonly Guid guidChutzpahCmdSet = new Guid(guidChutzpahCmdSetString);
        //public static readonly Guid guidSourceEditorCmdSet = new Guid(guidSourceEditorCmdSetString);
        //public static readonly Guid guidSolutionItemCmdSet = new Guid(guidSolutionItemCmdSetString);
        //public static readonly Guid guidSolutionFolderNodeCmdSet = new Guid(guidSolutionFolderNodeCmdSetString);
    }
}