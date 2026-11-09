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
using System.Collections.Generic;

namespace Zongsoft.Collections;

public interface IStack<T> : IReadOnlyCollection<T>, IEnumerable<T>
{
	#region 公共方法
	T Pop();
	T Peek();
	T Take(int index);
	T[] Take(int index, int count);
	void Push(T value);
	void Clear();

	bool TryPop(out T value);
	bool TryPeek(out T value);
	bool TryTake(int index, out T value);
	bool TryTake(int index, int count, out T[] values);
	#endregion
}
