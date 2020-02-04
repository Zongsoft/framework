using System;

namespace Zongsoft.Hosting.Terminal
{
	class Program
	{
		static void Main(string[] args)
		{
			Zongsoft.Plugins.Application.Start(Zongsoft.Terminals.Plugins.ApplicationContext.Current, args);
		}
	}
}
