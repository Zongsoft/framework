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

public class PluginFileException : PluginException
{
	#region 构造函数
	public PluginFileException(string message) : base(message) { }
	public PluginFileException(string fileName, string message) : base(message, null) => this.FileName = fileName;
	public PluginFileException(string fileName, string message, Exception innerException) : base(message, innerException) => this.FileName = fileName;
	#endregion

	#region 公共属性
	public string FileName { get; }
	#endregion
}
