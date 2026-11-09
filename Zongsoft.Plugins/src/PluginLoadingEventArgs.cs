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

public class PluginLoadingEventArgs : PluginLoadEventArgs
{
	public PluginLoadingEventArgs(string pluginFile, PluginOptions options) : base(options)
	{
		if(string.IsNullOrEmpty(pluginFile))
			throw new ArgumentNullException(nameof(pluginFile));

		this.PluginFile = pluginFile;
	}

	public string PluginFile
	{
		get;
		private set;
	}
}
