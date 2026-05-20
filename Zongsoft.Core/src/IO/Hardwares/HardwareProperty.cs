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

namespace Zongsoft.IO.Hardwares;

/// <summary>
/// 表示硬件属性。
/// </summary>
public class HardwareProperty
{
	#region 构造函数
	/// <summary>初始化 <see cref="HardwareProperty"/> 类的新实例。</summary>
	/// <param name="name">属性名称。</param>
	/// <param name="value">属性值。</param>
	/// <param name="description">属性描述。</param>
	public HardwareProperty(string name, object value, string description = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Value = value;
		this.Description = description;
	}
	#endregion

	#region 公共属性
	/// <summary>获取属性名称。</summary>
	public string Name { get; }

	/// <summary>获取属性值。</summary>
	public object Value { get; }

	/// <summary>获取属性描述。</summary>
	public string Description { get; }
	#endregion

	#region 重写方法
	public override string ToString() => this.Value == null ? this.Name : $"{this.Name}={this.Value}";
	#endregion
}
