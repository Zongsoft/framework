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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.IO.Hardwares;

/// <summary>表示硬件属性集。</summary>
public class HardwarePropertyCollection : KeyedCollection<string, HardwareProperty>
{
	#region 构造函数
	/// <summary>初始化 <see cref="HardwarePropertyCollection"/> 类的新实例。</summary>
	public HardwarePropertyCollection() : base(StringComparer.OrdinalIgnoreCase) { }

	/// <summary>初始化 <see cref="HardwarePropertyCollection"/> 类的新实例。</summary>
	/// <param name="properties">初始的硬件属性集。</param>
	public HardwarePropertyCollection(IEnumerable<HardwareProperty> properties) : this()
	{
		if(properties == null)
			return;

		foreach(var property in properties)
		{
			if(property != null)
				this.Add(property);
		}
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(HardwareProperty property) => property.Name;
	#endregion
}
