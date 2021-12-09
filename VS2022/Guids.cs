// Guids.cs
// MUST match guids.h

using System;

namespace Chutzpah.VS2022
{
    static class GuidList
    {
        public const string guidVS2022ChutzpahPkgString = "a523d775-1341-4f21-a950-8c716e5628c9";
        public const string guidVS2022ChutzpahCmdSetString = "c9e8741d-31d2-4883-b8e2-1d70201965f9";
        public static readonly Guid guidVS2022ChutzpahCmdSet = new Guid(guidVS2022ChutzpahCmdSetString);
    };
}