using System;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Samples;

internal class Program
{
	static void Main(string[] args)
	{
		using var superviser = new MySuperviser(new SupervisableOptions(TimeSpan.FromSeconds(10), 5));

		superviser.Supervise("S1", new MySupervisable("S1", new SupervisableOptions(TimeSpan.Zero, -1)));
		superviser.Supervise("S2", new MySupervisable("S2", new SupervisableOptions(TimeSpan.Zero, 3)));
		superviser.Supervise("S3", new MySupervisable("S3", new SupervisableOptions(TimeSpan.FromSeconds(30), -1)));
		superviser.Supervise("S4", new MySupervisable("S4", new SupervisableOptions(TimeSpan.FromSeconds(30), 3)));
		superviser.Supervise("S5", new MySupervisable("S5"));

		Terminal.Console.Executor.Command("help", context =>
		{
			context.Output.WriteLine(CommandOutletColor.DarkYellow, "Input `exit` to quit the program.");
			context.Output.WriteLine(CommandOutletColor.DarkYellow, "Input `info` to display the superviser information.");
			context.Output.WriteLine(CommandOutletColor.DarkYellow, "Input `open` to start reporting of supervisable objects with the specified name.");
			context.Output.WriteLine(CommandOutletColor.DarkYellow, "Input `close` to stop reporting of supervisable objects with the specified name.");
			context.Output.WriteLine(CommandOutletColor.DarkYellow, "Input `reset` to clear all supervisable objects.");
			context.Output.WriteLine(CommandOutletColor.DarkYellow, "Input `error` to enables error reporting for the supervisable objects with the specified name.");
			context.Output.WriteLine(CommandOutletColor.DarkYellow, "Input `pause` to pause reporting of supervisable objects with the specified name.");
			context.Output.WriteLine(CommandOutletColor.DarkYellow, "Input `resume` to resume reporting of supervisable objects with the specified name.");
		});

		Terminal.Console.Executor.Command("info", context =>
		{
			var content = CommandOutletContent.Create()
				.AppendLine(CommandOutletColor.Gray, new string('·', 60))
				.Append(CommandOutletColor.DarkMagenta, $"Lifecycle: ")
				.Append(CommandOutletColor.DarkYellow, superviser.Options.Lifecycle)
				.Append(CommandOutletColor.DarkGray, ",\t")
				.Append(CommandOutletColor.DarkMagenta, $"Error Limit: ")
				.Append(CommandOutletColor.DarkYellow, superviser.Options.ErrorLimit)
				.AppendLine(CommandOutletColor.DarkGray, ".")
				.AppendLine(CommandOutletColor.Gray, new string('·', 60));

			context.Output.Write(content);

			foreach(var observable in superviser)
				context.Output.WriteLine(observable);
		});

		Terminal.Console.Executor.Command("reset", context =>
		{
			superviser.Clear();
		});

		Terminal.Console.Executor.Command("open", context =>
		{
			if(context.Expression.Arguments.IsEmpty)
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The open command is missing required arguments.");
				return;
			}

			foreach(var supervisable in Get(superviser, context.Expression.Arguments))
				supervisable.Open();
		});

		Terminal.Console.Executor.Command("close", context =>
		{
			if(context.Expression.Arguments.IsEmpty)
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The open command is missing required arguments.");
				return;
			}

			foreach(var supervisable in Get(superviser, context.Expression.Arguments))
				supervisable.Close();
		});

		Terminal.Console.Executor.Command("pause", context =>
		{
			if(context.Expression.Arguments.IsEmpty)
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The pause command is missing required arguments.");
				return;
			}

			foreach(var supervisable in Get(superviser, context.Expression.Arguments))
				supervisable.Pause();
		});

		Terminal.Console.Executor.Command("resume", context =>
		{
			if(context.Expression.Arguments.IsEmpty)
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The resume command is missing required arguments.");
				return;
			}

			foreach(var supervisable in Get(superviser, context.Expression.Arguments))
				supervisable.Resume();
		});

		Terminal.Console.Executor.Command("error", context =>
		{
			if(context.Expression.Arguments.IsEmpty)
			{
				Terminal.WriteLine(CommandOutletColor.Red, $"The resume command is missing required arguments.");
				return;
			}

			foreach(var supervisable in Get(superviser, context.Expression.Arguments))
				supervisable.Error();
		});

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Gray, new string('·', 60))
			.AppendLine(CommandOutletColor.Cyan, $"Welcome to the Superviser & Supervisable sample.".Justify(60))
			.AppendLine(CommandOutletColor.Gray, new string('·', 60));

		Terminal.Console.Executor.Run(splash);
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
