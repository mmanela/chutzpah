// Guids.cs
// MUST match guids.h

using System;

namespace Chutzpah.VS2012
{
    static class GuidList
    {
        public const string guidVS2012ChutzpahPkgString = "a523d775-1341-4f21-a950-8c716e5628c9";
        public const string guidVS2012ChutzpahCmdSetString = "c9e8741d-31d2-4883-b8e2-1d70201965f9";
        public static readonly Guid guidVS2012ChutzpahCmdSet = new Guid(guidVS2012ChutzpahCmdSetString);
    };
}