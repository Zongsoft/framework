using System;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Hardwares.Samples;

internal class Program
{
	static void Main(string[] args)
	{
		var hardwares = HardwareCollector.Instance.Collect();
		var profile = new IO.Hardwares.HardwareProfile(hardwares);

		Terminal.WriteLine(CommandOutletColor.Magenta, profile.Identifier);

		foreach(var hardware in profile)
		{
			//Terminal.WriteLine(hardware);
			var content = CommandOutletDumper.Dump(hardware, -1);
			Terminal.Write(content);
		}

		Terminal.WriteLine();
		Terminal.WriteLine(CommandOutletColor.Gray, "______________________");
		Terminal.WriteLine(CommandOutletColor.DarkCyan, "Press any key to exit.");
		Console.ReadKey();
	}
}
