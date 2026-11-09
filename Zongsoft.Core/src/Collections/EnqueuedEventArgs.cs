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

namespace Zongsoft.Collections;

[Serializable]
public class EnqueuedEventArgs : EventArgs
{
	#region 构造函数
	public EnqueuedEventArgs(object value, object settings)
	{
		this.Value = value;
		this.Settings = settings;
	}
	#endregion

	#region 公共属性
	/// <summary>获取入队的内容值。</summary>
	public object Value { get; }

	/// <summary>获取入队操作的设置选项。</summary>
	public object Settings { get; }
	#endregion
}
