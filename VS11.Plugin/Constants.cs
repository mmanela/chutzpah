using System;

namespace Chutzpah.VS11
{
	static class Constants
	{
		public const string ExecutorUriString = "executor://chutzpah-js";
		public static Uri ExecutorUri = new Uri(ExecutorUriString);
	}
}
