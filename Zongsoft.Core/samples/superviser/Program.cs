using System;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Components;

using Terminal = Zongsoft.Terminals.ConsoleTerminal;

namespace Zongsoft.Samples;

internal class Program
{
	static void Main(string[] args)
	{
		using var superviser = new MySuperviser(new SupervisableOptions(TimeSpan.FromSeconds(10), 5));

		Terminal.Instance.WriteLine(CommandOutletColor.Gray, new string('·', 60));
		Terminal.Instance.Write(CommandOutletColor.DarkMagenta, $"Lifecycle: ");
		Terminal.Instance.Write(CommandOutletColor.DarkYellow, superviser.Options.Lifecycle);
		Terminal.Instance.Write(CommandOutletColor.DarkGray, ",\t");

		Terminal.Instance.Write(CommandOutletColor.DarkMagenta, $"Error Limit: ");
		Terminal.Instance.Write(CommandOutletColor.DarkYellow, superviser.Options.ErrorLimit);
		Terminal.Instance.WriteLine(CommandOutletColor.DarkGray, ".");
		Terminal.Instance.WriteLine(CommandOutletColor.Gray, new string('·', 60));

		Terminal.Instance.WriteLine();

		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `exit` to quit the program.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `info` to display the superviser information.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `open` to start reporting of supervisable objects with the specified name.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `close` to stop reporting of supervisable objects with the specified name.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `clear` to clear all supervisable objects.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `error` to enables error reporting for the supervisable objects with the specified name.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `pause` to pause reporting of supervisable objects with the specified name.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `resume` to resume reporting of supervisable objects with the specified name.");
		Terminal.Instance.WriteLine();

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
						Terminal.Instance.WriteLine(CommandOutletColor.Red, $"The open command is missing required arguments.");
						continue;
					}

					foreach(var supervisable in Get(superviser, parts[1..]))
						supervisable.Open();

					break;
				case "pause":
					if(parts.Length <= 1)
					{
						Terminal.Instance.WriteLine(CommandOutletColor.Red, $"The pause command is missing required arguments.");
						continue;
					}

					foreach(var supervisable in Get(superviser, parts[1..]))
						supervisable.Pause();

					break;
				case "resume":
					if(parts.Length <= 1)
					{
						Terminal.Instance.WriteLine(CommandOutletColor.Red, $"The resume command is missing required arguments.");
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
						Terminal.Instance.WriteLine(CommandOutletColor.Red, $"The close command is missing required arguments.");
						continue;
					}

					foreach(var supervisable in Get(superviser, parts[1..]))
						supervisable.Close();

					break;
				case "err":
				case "error":
					if(parts.Length <= 1)
					{
						Terminal.Instance.WriteLine(CommandOutletColor.Red, $"The error command is missing required arguments.");
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
				Terminal.Instance.WriteLine($"The specified “{names[i]}” supervisable object does not exist.");
				continue;
			}

			if(observable is MySupervisable supervisable)
				yield return supervisable;
		}
	}
}
