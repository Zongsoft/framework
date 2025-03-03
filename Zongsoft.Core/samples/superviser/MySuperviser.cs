using System;

using Zongsoft.Services;
using Zongsoft.Components;

using Terminal = Zongsoft.Terminals.ConsoleTerminal;

namespace Zongsoft.Samples;

public class MySuperviser(SupervisableOptions options = null) : Superviser<string>(options)
{
	protected override bool OnError(IObservable<string> observable, Exception exception, uint count)
	{
		Terminal.Instance.WriteLine(
			CommandOutletColor.DarkRed,
			$"Superviser.OnError: {observable}({count})" + Environment.NewLine + $"\t[{exception.GetType().Name}] {exception.Message}"
		);

		return base.OnError(observable, exception, count);
	}

	protected override void OnUnsupervised(object key, IObservable<string> observable, SupervisableReason reason)
	{
		Terminal.Instance.WriteLine(
			CommandOutletContent
				.Create(CommandOutletColor.DarkGray, $"[{DateTime.Now:HH:mm:ss}] ")
				.Append(CommandOutletColor.DarkMagenta, $"Superviser.Unsupervised: {observable}"));

		base.OnUnsupervised(key, observable, reason);
	}
}
