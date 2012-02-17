using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VS11.Plugin
{
	static class Constants
	{
		public const string ExecutorUriString = "executor://chutzpah-js";
		public static Uri ExecutorUri = new Uri(ExecutorUriString);
	}
}
