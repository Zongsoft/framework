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

namespace Zongsoft.Data;

/// <summary>表示支持分页的接口。</summary>
public interface IPageable
{
	/// <summary>表示完成分页的事件。</summary>
	event Func<object, PagingEventArgs, CancellationToken, ValueTask> Paginated;

	/// <summary>获取一个值，指示当前分页集是否被禁用。</summary>
	bool Suppressed { get; }
}
