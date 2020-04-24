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
			Host.CreateDefaultBuilder(args)
				.ConfigurePlugins<TerminalApplicationContext>()
				.Build()
				.Run();
		}
	}
}
