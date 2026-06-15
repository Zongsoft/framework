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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.IO.Hardwares;

/// <summary>
/// 表示硬件组件集。
/// </summary>
public class HardwareComponentCollection : ICollection<HardwareComponent>, IReadOnlyCollection<HardwareComponent>
{
	#region 成员字段
	private readonly List<HardwareComponent> _components;
	#endregion

	#region 构造函数
	/// <summary>初始化 <see cref="HardwareComponentCollection"/> 类的新实例。</summary>
	public HardwareComponentCollection() => _components = [];

	/// <summary>初始化 <see cref="HardwareComponentCollection"/> 类的新实例。</summary>
	/// <param name="components">初始的硬件组件集。</param>
	public HardwareComponentCollection(IEnumerable<HardwareComponent> components)
	{
		_components = components == null ? [] : new List<HardwareComponent>(components);
		_components.RemoveAll(component => component == null);
	}
	#endregion

	#region 公共属性
	/// <summary>获取组件数量。</summary>
	public int Count => _components.Count;

	/// <summary>获取指定索引处的组件。</summary>
	/// <param name="index">要获取的组件索引。</param>
	/// <returns>返回指定索引处的组件。</returns>
	public HardwareComponent this[int index] => _components[index];
	#endregion

	#region 公共方法
	/// <summary>添加指定的组件。</summary>
	/// <param name="component">要添加的组件。</param>
	public void Add(HardwareComponent component)
	{
		if(component == null)
			throw new ArgumentNullException(nameof(component));

		_components.Add(component);
	}

	/// <summary>移除指定的组件。</summary>
	/// <param name="component">要移除的组件。</param>
	/// <returns>如果移除成功则返回真(<c>true</c>)，否则返回假(<c>false</c>)。</returns>
	public bool Remove(HardwareComponent component) => component != null && _components.Remove(component);

	/// <summary>清空所有组件。</summary>
	public void Clear() => _components.Clear();

	/// <summary>获取一个值，指示是否包含指定的组件。</summary>
	/// <param name="component">要判断的组件。</param>
	/// <returns>如果包含指定的组件则返回真(<c>true</c>)，否则返回假(<c>false</c>)。</returns>
	public bool Contains(HardwareComponent component) => component != null && _components.Contains(component);

	/// <summary>将组件复制到指定数组。</summary>
	/// <param name="array">目标数组。</param>
	/// <param name="index">目标数组中的起始索引。</param>
	public void CopyTo(HardwareComponent[] array, int index) => _components.CopyTo(array, index);
	#endregion

	#region 显式实现
	bool ICollection<HardwareComponent>.IsReadOnly => false;
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<HardwareComponent> GetEnumerator() => _components.GetEnumerator();
	#endregion
}
