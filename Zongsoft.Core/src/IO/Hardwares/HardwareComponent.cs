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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.IO.Hardwares;

/// <summary>
/// 表示硬件组件。
/// </summary>
public class HardwareComponent
{
	#region 构造函数
	/// <summary>初始化 <see cref="HardwareComponent"/> 类的新实例。</summary>
	/// <param name="name">组件名称。</param>
	/// <param name="code">组件代码。</param>
	/// <param name="type">组件类型。</param>
	/// <param name="description">组件描述。</param>
	/// <param name="properties">组件属性集。</param>
	/// <param name="components">子组件集。</param>
	public HardwareComponent(string name, string code = null, string type = null, string description = null, IEnumerable<HardwareProperty> properties = null, IEnumerable<HardwareComponent> components = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Code = code;
		this.Type = type;
		this.Description = description;
		this.Properties = properties == null ? new() : new(properties);
		this.Components = components == null ? new() : new(components);
	}
	#endregion

	#region 公共属性
	/// <summary>获取组件名称。</summary>
	public string Name { get; }

	/// <summary>获取组件代码。</summary>
	public string Code { get; }

	/// <summary>获取组件类型。</summary>
	public string Type { get; }

	/// <summary>获取组件描述。</summary>
	public string Description { get; }

	/// <summary>获取子组件集。</summary>
	public HardwareComponentCollection Components { get; }

	/// <summary>获取组件属性集。</summary>
	public HardwarePropertyCollection Properties { get; }
	#endregion

	#region 重写方法
	/// <summary>返回当前组件的文本表示。</summary>
	/// <returns>返回当前组件的文本表示。</returns>
	public override string ToString() => string.IsNullOrEmpty(this.Code) ? this.Name : $"{this.Name}({this.Code})";
	#endregion
}
