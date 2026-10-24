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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Common;

/// <summary>按契约类型保存特性实例，并支持从父集合继承特性。</summary>
/// <remarks>集合应在初始化时配置，不负责释放特性实例；计数和枚举仅包含本地注册。</remarks>
public class FeatureCollection : IReadOnlyCollection<KeyValuePair<Type, object>>
{
	#region 成员字段
	private readonly FeatureCollection _parent;
	private readonly Dictionary<Type, object> _features = new();
	#endregion

	#region 构造函数
	/// <summary>初始化空的特性集合。</summary>
	public FeatureCollection() { }

	/// <summary>初始化具有指定父集合的特性集合。</summary>
	/// <param name="parent">本地未注册时用于查找特性的父集合。</param>
	public FeatureCollection(FeatureCollection parent) => _parent = parent;
	#endregion

	#region 公共属性
	/// <summary>获取本地注册的特性数量。</summary>
	public int Count => _features.Count;
	#endregion

	#region 公共方法
	/// <summary>获取按指定契约类型注册的特性，未在本地注册时查找父集合。</summary>
	/// <typeparam name="TFeature">特性契约的引用类型，通常为接口。</typeparam>
	/// <returns>返回注册的共享实例；本地和父集合均未注册时返回空。</returns>
	public TFeature Get<TFeature>() where TFeature : class => _features.TryGetValue(typeof(TFeature), out var feature) ? (TFeature)feature : _parent?.Get<TFeature>();

	/// <summary>设置指定契约的本地特性，空值移除本地覆盖并恢复父集合的实现。</summary>
	/// <typeparam name="TFeature">注册所用的特性契约类型。</typeparam>
	/// <param name="feature">特性实例，或用于移除本地注册的空值。</param>
	/// <remarks>注册按精确类型匹配，操作期间不得修改集合。</remarks>
	public void Set<TFeature>(TFeature feature) where TFeature : class
	{
		if(feature == null)
			_features.Remove(typeof(TFeature));
		else
			_features[typeof(TFeature)] = feature;
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<KeyValuePair<Type, object>> GetEnumerator() => _features.GetEnumerator();
	#endregion
}
