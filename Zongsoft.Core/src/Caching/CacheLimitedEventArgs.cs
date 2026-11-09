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

namespace Zongsoft.Caching;

public class CacheLimitedEventArgs
{
	public CacheLimitedEventArgs(int limit, int count)
	{
		this.Limit = limit;
		this.Count = count;
	}

	public int Limit { get; }
	public int Count { get; }

	public override string ToString() => $"{this.Limit}/{this.Count}";
}
