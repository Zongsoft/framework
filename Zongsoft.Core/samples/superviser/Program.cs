using System;
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Samples;

internal class Program
{
	static void Main(string[] args)
	{
		using var superviser = new MySuperviser(new SupervisableOptions(TimeSpan.FromSeconds(10), 5));

		Terminal.WriteLine(CommandOutletColor.Gray, new string('·', 60));
		Terminal.Write(CommandOutletColor.DarkMagenta, $"Lifecycle: ");
		Terminal.Write(CommandOutletColor.DarkYellow, superviser.Options.Lifecycle);
		Terminal.Write(CommandOutletColor.DarkGray, ",\t");

		Terminal.Write(CommandOutletColor.DarkMagenta, $"Error Limit: ");
		Terminal.Write(CommandOutletColor.DarkYellow, superviser.Options.ErrorLimit);
		Terminal.WriteLine(CommandOutletColor.DarkGray, ".");
		Terminal.WriteLine(CommandOutletColor.Gray, new string('·', 60));

		Terminal.WriteLine();

		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `exit` to quit the program.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `info` to display the superviser information.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `open` to start reporting of supervisable objects with the specified name.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `close` to stop reporting of supervisable objects with the specified name.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `clear` to clear all supervisable objects.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `error` to enables error reporting for the supervisable objects with the specified name.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `pause` to pause reporting of supervisable objects with the specified name.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `resume` to resume reporting of supervisable objects with the specified name.");
		Terminal.WriteLine();

		superviser.Supervise("S1", new MySupervisable("S1", new SupervisableOptions(TimeSpan.Zero, -1)));
		superviser.Supervise("S2", new MySupervisable("S2", new SupervisableOptions(TimeSpan.Zero, 3)));
		superviser.Supervise("S3", new MySupervisable("S3", new SupervisableOptions(TimeSpan.FromSeconds(30), -1)));
		superviser.Supervise("S4", new MySupervisable("S4", new SupervisableOptions(TimeSpan.FromSeconds(30), 3)));
		superviser.Supervise("S5", new MySupervisable("S5"));

		while(true)
		{
			var parts = Console.ReadLine().Trim().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			if(parts == null || parts.Length == 0)
				continue;

			switch(parts[0])
			{
				case "exit":
					return;
				case "info":
					foreach(var observable in superviser)
						Console.WriteLine(observable);
					break;
				case "open":
					if(parts.Length <= 1)
					{
						Terminal.WriteLine(CommandOutletColor.Red, $"The open command is missing required arguments.");
						continue;
					}

					foreach(var supervisable in Get(superviser, parts[1..]))
						supervisable.Open();

					break;
				case "pause":
					if(parts.Length <= 1)
					{
						Terminal.WriteLine(CommandOutletColor.Red, $"The pause command is missing required arguments.");
						continue;
					}

					foreach(var supervisable in Get(superviser, parts[1..]))
						supervisable.Pause();

					break;
				case "resume":
					if(parts.Length <= 1)
					{
						Terminal.WriteLine(CommandOutletColor.Red, $"The resume command is missing required arguments.");
						continue;
					}

					foreach(var supervisable in Get(superviser, parts[1..]))
						supervisable.Resume();

					break;
				case "clear":
					superviser.Clear();
					break;
				case "close":
					if(parts.Length <= 1)
					{
						Terminal.WriteLine(CommandOutletColor.Red, $"The close command is missing required arguments.");
						continue;
					}

					foreach(var supervisable in Get(superviser, parts[1..]))
						supervisable.Close();

					break;
				case "err":
				case "error":
					if(parts.Length <= 1)
					{
						Terminal.WriteLine(CommandOutletColor.Red, $"The error command is missing required arguments.");
						continue;
					}

					foreach(var supervisable in Get(superviser, parts[1..]))
						supervisable.Error();

					break;
				default:
					break;
			}
		}
	}

	private static IEnumerable<MySupervisable> Get(MySuperviser superviser, params string[] names)
	{
		if(names == null || names.Length == 0)
			yield break;

		for(int i = 0; i < names.Length; i++)
		{
			var observable = superviser[names[i]];

			if(observable == null)
			{
				Terminal.WriteLine($"The specified “{names[i]}” supervisable object does not exist.");
				continue;
			}

			if(observable is MySupervisable supervisable)
				yield return supervisable;
		}
	}
}
