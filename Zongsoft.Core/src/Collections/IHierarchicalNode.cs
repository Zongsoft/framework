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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.ComponentModel;

namespace Zongsoft.Collections;

/// <summary>
/// 表示层次结构节点的接口。
/// </summary>
public interface IHierarchicalNode : IEquatable<IHierarchicalNode>, INotifyPropertyChanged
{
	/// <summary>获取层次结构节点的名称，名称不可为空或空字符串，根节点的名称为斜杠(即“/”)。</summary>
	string Name { get; }

	/// <summary>获取层次结构节点的路径。</summary>
	/// <remarks>如果为根节点则返回空字符串(<c>String.Empty</c>)，否则即为父节点的全路径。</remarks>
	string Path { get; }

	/// <summary>获取层次结构节点的完整路径，即节点路径与名称的组合。</summary>
	string FullPath { get; }
}

/// <summary>
/// 表示层次结构节点的泛型接口。
/// </summary>
/// <typeparam name="TNode">泛型参数，表示层次结构节点的类型。</typeparam>
public interface IHierarchicalNode<TNode> : IHierarchicalNode where TNode : IHierarchicalNode<TNode>
{
	/// <summary>获取层次结构节点的子节点集合。</summary>
	IHierarchicalNodeCollection<TNode> Nodes { get; }
}