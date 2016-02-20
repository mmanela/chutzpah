using System;
using System.Collections.Generic;
using System.Linq;
using Chutzpah.Models;

namespace Chutzpah.Extensions
{
    public static class TestingModeExtensions
    {
        public static Dictionary<TestingMode, List<string>> ExtensionMap { get; private set; }


        static TestingModeExtensions()
        {
            ExtensionMap = new Dictionary<TestingMode,List<string>>();
            ExtensionMap[TestingMode.JavaScript] = new  List<string>{ Constants.JavaScriptExtension , Constants.JavaScriptReactExtension };
            ExtensionMap[TestingMode.CoffeeScript] = new  List<string>{Constants.CoffeeScriptExtension};
            ExtensionMap[TestingMode.TypeScript] = new  List<string>{ Constants.TypeScriptExtension, Constants.TypeScriptReactExtension };
            ExtensionMap[TestingMode.HTML] = new  List<string>{Constants.HtmlScriptExtension, Constants.HtmScriptExtension};

            ExtensionMap[TestingMode.All] = new List<string>();
            ExtensionMap.Values.ForEach(ext => ExtensionMap[TestingMode.All].AddRange(ext));

            ExtensionMap[TestingMode.AllExceptHTML] = ExtensionMap[TestingMode.All].Except(ExtensionMap[TestingMode.HTML]).ToList();
        }

        public static bool FileBelongsToTestingMode(this TestingMode testingMode, string file)
        {
            if (string.IsNullOrEmpty(file)) return false;
            var extensions = ExtensionMap[testingMode];
            return extensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

        }
    }
}