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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

/// <summary>提供扩展模式成员解析所需的上下文。</summary>
public readonly struct SchemaMemberResolverContext
{
	#region 构造函数
	/// <summary>初始化模式成员解析上下文。</summary>
	/// <param name="entity">当前映射实体；纯模型作用域中可能为空。</param>
	/// <param name="modelType">当前模型类型。</param>
	/// <param name="parent">当前父成员；根级成员为空。</param>
	/// <param name="name">显式成员名；通配符枚举时为空。</param>
	public SchemaMemberResolverContext(IDataEntity entity, Type modelType, ISchemaMember parent, string name)
	{
		this.Entity = entity;
		this.ModelType = modelType ?? typeof(object);
		this.Parent = parent;
		this.Name = name;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前映射实体。</summary>
	public IDataEntity Entity { get; }
	/// <summary>获取当前模型类型。</summary>
	public Type ModelType { get; }
	/// <summary>获取当前父成员。</summary>
	public ISchemaMember Parent { get; }
	/// <summary>获取显式成员名；通配符枚举时为空。</summary>
	public string Name { get; }
	/// <summary>获取当前父成员的完整路径。</summary>
	public string Path => this.Parent?.FullPath ?? string.Empty;
	#endregion
}
