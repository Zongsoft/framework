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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
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
