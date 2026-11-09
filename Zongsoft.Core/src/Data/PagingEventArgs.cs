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

namespace Zongsoft.Data;

/// <summary>表示分页事件的参数类。</summary>
public class PagingEventArgs : EventArgs
{
	#region 构造函数
	public PagingEventArgs(string name, Paging paging)
	{
		this.Name = name;
		this.Paging = paging ?? throw new ArgumentNullException(nameof(paging));
	}
	#endregion

	#region 公共属性
	/// <summary>表示分页源的名称。</summary>
	public string Name { get; }

	/// <summary>表示分页的信息。</summary>
	public Paging Paging { get; }
	#endregion
}
