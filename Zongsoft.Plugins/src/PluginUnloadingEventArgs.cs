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
using System.ComponentModel;

namespace Zongsoft.Plugins;

public class PluginUnloadingEventArgs : CancelEventArgs
{
	#region 构造函数
	public PluginUnloadingEventArgs(Plugin plugin) : this(plugin, false)
	{
	}

	public PluginUnloadingEventArgs(Plugin plugin, bool cancel) : base(cancel)
	{
		if(plugin == null)
			throw new ArgumentNullException("plugin");

		this.Plugin = plugin;
	}
	#endregion

	#region 公共属性
	public Plugin Plugin
	{
		get;
		private set;
	}
	#endregion
}
