using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Hardwares.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		var hardwares = new List<IO.Hardwares.IHardware>();
		await foreach(var hardware in HardwareCollector.Instance.CollectAsync())
			hardwares.Add(hardware);

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
