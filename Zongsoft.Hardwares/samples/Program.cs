using System;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Hardwares.Samples;

internal class Program
{
	static void Main(string[] args)
	{
		var hardwares = HardwareCollector.Instance.Collect();

		foreach(var hardware in hardwares)
		{
			var content = CommandOutletDumper.Dump(hardware, -1);
			Terminal.Write(content);
		}

		Terminal.WriteLine();
		Terminal.WriteLine(CommandOutletColor.Gray, "______________________");
		Terminal.WriteLine(CommandOutletColor.DarkCyan, "Press any key to exit.");
		Console.ReadKey();
	}
}
