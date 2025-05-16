﻿using System;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Samples;

public class MySuperviser(SupervisableOptions options = null) : Superviser<string>(options)
{
	protected override bool OnError(IObservable<string> observable, Exception exception, uint count)
	{
		Terminal.WriteLine(
			CommandOutletColor.DarkRed,
			$"Superviser.OnError: {observable}({count})" + Environment.NewLine + $"\t[{exception.GetType().Name}] {exception.Message}"
		);

		return base.OnError(observable, exception, count);
	}

	protected override void OnUnsupervised(object key, IObservable<string> observable, SupervisableReason reason)
	{
		Terminal.WriteLine(
			CommandOutletContent
				.Create(CommandOutletColor.DarkGray, $"[{DateTime.Now:HH:mm:ss}] ")
				.Append(CommandOutletColor.DarkMagenta, $"Superviser.Unsupervised: {observable}"));

		base.OnUnsupervised(key, observable, reason);
	}
}
