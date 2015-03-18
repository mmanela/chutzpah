using System;

namespace Chutzpah.VS2012.TestAdapter
{
	public static class AdapterConstants
	{
		public const string ExecutorUriString = "executor://chutzpah-js";
		public static Uri ExecutorUri = new Uri(ExecutorUriString);


        public const string SettingsName = "ChutzpahAdapterSettings";
	}
}
