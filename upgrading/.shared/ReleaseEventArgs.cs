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

namespace Zongsoft.Upgrading;

/// <summary>表示发布相关的事件参数类。</summary>
public class ReleaseEventArgs : EventArgs
{
	#region 构造函数
	public ReleaseEventArgs(params Release[] releases)
	{
		this.Releases = releases ?? [];

		if(releases.Length > 0)
			this.Release = releases[0];
	}
	#endregion

	#region 公共属性
	/// <summary>获取发布对象。</summary>
	public Release Release { get; }
	/// <summary>获取发布对象集。</summary>
	public Release[] Releases { get; }
	#endregion
}
