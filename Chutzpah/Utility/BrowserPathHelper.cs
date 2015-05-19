using System;
using System.IO;
using System.Net.Configuration;
using Microsoft.Win32;

namespace Chutzpah.Utility
{
    public class BrowserPathHelper
    {
        private const string BrowserIERegPath = "Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\IEXPLORE.EXE";
        private const string BrowserChromeRegPath = "Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\Chrome.EXE";
        private const string BrowserFirefoxRegPath = "Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\Firefox.exe";
        private const string DefaultBrowserProdIdPath = "Software\\Microsoft\\Windows\\Shell\\Associations\\UrlAssociations\\http\\UserChoice";

        public static string GetBrowserPath(string browserName)
        {
            var browserPath = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(browserName))
                {
                    browserPath = GetBrowserPathFromRegistry(browserName);
                }

                if (string.IsNullOrEmpty(browserPath))
                {
                    browserPath = GetSystemDefaultBrowser();
                }
            }
            catch (Exception e)
            {
                ChutzpahTracer.TraceError(e, "Unable to read browser path from registry");
            }

            return browserPath;
        }

        private static string GetBrowserPathFromRegistry(string browserName)
        {
            string browserPath = null;
            bool hasRegEntry = false;
            switch (browserName.ToLower())
            {
                case "ie":
                    hasRegEntry = TryRetrieveRegistryKeyValue(Registry.LocalMachine, BrowserIERegPath, null, out browserPath);
                    break;
                case "chrome":
                    hasRegEntry = TryRetrieveRegistryKeyValue(Registry.LocalMachine, BrowserChromeRegPath, null, out browserPath);
                    break;
                case "firefox":
                    hasRegEntry = TryRetrieveRegistryKeyValue(Registry.LocalMachine, BrowserFirefoxRegPath, null, out browserPath);
                    break;
                default:
                    break;
            }

            if (hasRegEntry && File.Exists(browserPath))
            {
                return browserPath;
            }
            else
            {
                return string.Empty;
            }
        }

        private static string GetSystemDefaultBrowser()
        {
            string defaultBrowserProgId;
            string defaultBrowserCommandPath;
            string trimmedDefaultBrowserCommandPath;

            if (TryRetrieveRegistryKeyValue(Registry.CurrentUser, DefaultBrowserProdIdPath, "ProgId", out defaultBrowserProgId) &&
                TryRetrieveRegistryKeyValue(Registry.LocalMachine, "Software\\Classes\\" + defaultBrowserProgId + "\\shell\\open\\command", null, out defaultBrowserCommandPath) &&
                TrimRegistryCommandPath(defaultBrowserCommandPath, out trimmedDefaultBrowserCommandPath) &&
                File.Exists(trimmedDefaultBrowserCommandPath))
            {
                return trimmedDefaultBrowserCommandPath;
            }

            return string.Empty;
        }

        private static bool TryRetrieveRegistryKeyValue(RegistryKey parentKey, string registryKeyPath, string name, out string registryValue)
        {
            var registryKey = parentKey.OpenSubKey(registryKeyPath, false);
            registryValue = null;

            if (registryKey != null)
            {
                var value = registryKey.GetValue(name);
                if (value != null)
                {
                    registryValue = value.ToString();
                }

                registryKey.Close();
            }

            return !string.IsNullOrEmpty(registryValue);
        }

        private static bool TrimRegistryCommandPath(string commandFilePath, out string trimmedCommandFilePath)
        {
            trimmedCommandFilePath = null;

            commandFilePath = commandFilePath.ToLower().Replace("\"", string.Empty);

            if (!commandFilePath.EndsWith("exe"))
            {
                var indexOfExe = commandFilePath.LastIndexOf(".exe");

                if (indexOfExe > 0)
                {
                    trimmedCommandFilePath = commandFilePath.Substring(0, indexOfExe + 4);
                }
            }
            else
            {
                trimmedCommandFilePath = commandFilePath;
            }

            return !string.IsNullOrEmpty(trimmedCommandFilePath);
        }
    }
}