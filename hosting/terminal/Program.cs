using System;

using Microsoft.Extensions.Hosting;

using Zongsoft.Plugins;
using Zongsoft.Terminals;

namespace Zongsoft.Hosting.Terminal
{
	class Program
	{
		static void Main(string[] args)
		{
			Zongsoft.Diagnostics.Logger.Loggers.Add(new Zongsoft.Diagnostics.TextFileLogger());
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

			Host.CreateDefaultBuilder(args)
				.ConfigurePlugins<TerminalApplicationContext>()
				.Build()
				.Run();
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Zongsoft.Diagnostics.Logger.Error(e.ExceptionObject.ToString());
		}
	}
}
