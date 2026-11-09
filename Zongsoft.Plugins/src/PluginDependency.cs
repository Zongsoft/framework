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

public class PluginDependency
{
	#region 构造函数
	public PluginDependency(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name.Trim();
		this.Plugin = null;
	}
	#endregion

	#region 公共属性
	/// <summary>获取依赖的插件名。注：此名称不是插件的文件名。</summary>
	public string Name
	{
		get;
		internal set;
	}

	/// <summary>获取依赖的插件对象。</summary>
	/// <remarks>如果插件未加载完成，该属性返回空(<c>null</c>)。</remarks>
	public Plugin Plugin
	{
		get;
		internal set;
	}
	#endregion
}
