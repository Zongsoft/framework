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

public class FixedElementCollection : PluginElementCollection<FixedElement>
{
	#region 构造函数
	internal protected FixedElementCollection() { }
	#endregion

	#region 公共属性
	public override bool IsReadOnly => false;
	#endregion

	#region 公共方法
	public void Clear() => this.BaseClear();
	public void Remove(string name) => this.BaseRemoveKey(name);
	#endregion
}
