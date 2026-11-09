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

namespace Zongsoft.Plugins;

public class ValueChangingEventArgs : EventArgs
{
	#region 构造函数
	public ValueChangingEventArgs(object oldValue, object newValue)
	{
		this.OldValue = oldValue;
		this.NewValue = newValue;
	}
	#endregion

	#region 公共属性
	public object OldValue
	{
		get;
		private set;
	}

	public object NewValue
	{
		get;
		private set;
	}
	#endregion
}
