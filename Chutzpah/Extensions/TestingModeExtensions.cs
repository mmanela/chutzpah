using System;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah.Extensions
{
    public static class TestingModeExtensions
    {
        private static readonly Dictionary<TestingMode, List<string>> extensionMap;
        static TestingModeExtensions()
        {
            extensionMap = new Dictionary<TestingMode,List<string>>();
            extensionMap[TestingMode.JavaScript] = new  List<string>{Constants.JavaScriptExtension};
            extensionMap[TestingMode.CoffeeScript] = new  List<string>{Constants.CoffeeScriptExtension};
            extensionMap[TestingMode.TypeScript] = new  List<string>{Constants.TypeScriptExtension};
            extensionMap[TestingMode.HTML] = new  List<string>{Constants.HtmlScriptExtension, Constants.HtmScriptExtension};

            extensionMap[TestingMode.All] = new List<string>();
            extensionMap.Values.ForEach(ext => extensionMap[TestingMode.All].AddRange(ext));

            extensionMap[TestingMode.AllExceptHTML] = extensionMap[TestingMode.All].Except(extensionMap[TestingMode.HTML]).ToList();
        }

        public static bool FileBelongsToTestingMode(this TestingMode testingMode, string file)
        {
            if (string.IsNullOrEmpty(file)) return false;
            var extensions = extensionMap[testingMode];
            return extensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        }
    }
}