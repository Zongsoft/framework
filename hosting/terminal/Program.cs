using System;

using Microsoft.Extensions.Hosting;

namespace Zongsoft.Hosting.Terminal
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Zongsoft.Plugins.Hosting.Application
				.Terminal(args)
				.Run();
		}
	}
}
