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

public class PluginMountEventArgs : EventArgs
{
	#region 构造函数
	public PluginMountEventArgs(string path, object value)
	{
		if(string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		this.Path = path;
		this.Value = value;
	}
	#endregion

	#region 公共属性
	public string Path { get; }
	public object Value { get; }
	#endregion
}
