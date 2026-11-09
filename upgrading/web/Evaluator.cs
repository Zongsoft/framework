/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Upgrading;

public partial class Evaluator
{
	public static readonly EvaluatorBase Default = new DefaultEvaluator();

	private sealed class DefaultEvaluator() : EvaluatorBase("Default")
	{
		public override ValueTask<bool> EvaluateAsync(string name, string text, IDictionary<string, string> parameters, CancellationToken cancellation = default)
		{
			if(string.IsNullOrWhiteSpace(text))
				return ValueTask.FromResult(true);

			if(!Configuration.Settings.TryParse(text, out var settings))
				return ValueTask.FromResult(false);

			if(parameters == null)
				return ValueTask.FromResult(false);

			foreach(var setting in settings)
			{
				if(!parameters.TryGetValue(setting.Key, out var value))
					return ValueTask.FromResult(false);
				if(!string.Equals(setting.Value, value, StringComparison.OrdinalIgnoreCase))
					return ValueTask.FromResult(false);
			}

			return ValueTask.FromResult(true);
		}
	}
}
