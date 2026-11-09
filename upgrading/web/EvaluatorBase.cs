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

public abstract class EvaluatorBase
{
	protected EvaluatorBase(string name)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
	}

	public string Name { get; }
	public string Title => Resources.ResourceUtility.GetResourceString(this.GetType(),
	[
		$"Evaluator.{this.Name}.{nameof(this.Title)}",
		$"{this.Name}.{nameof(this.Title)}",
		$"Evaluator.{this.Name}",
		this.Name,
	]);

	public string Description => Resources.ResourceUtility.GetResourceString(this.GetType(),
	[
		$"Evaluator.{this.Name}.{nameof(this.Description)}",
		$"{this.Name}.{nameof(this.Description)}",
	]);

	public abstract ValueTask<bool> EvaluateAsync(string name, string settings, IDictionary<string, string> parameters, CancellationToken cancellation = default);
}
