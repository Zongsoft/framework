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
using System.ComponentModel;

namespace Zongsoft.Collections;

/// <summary>表示层次结构节点的接口。</summary>
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

/// <summary>表示层次结构节点的泛型接口。</summary>
/// <typeparam name="TNode">泛型参数，表示层次结构节点的类型。</typeparam>
public interface IHierarchicalNode<TNode> : IHierarchicalNode where TNode : IHierarchicalNode<TNode>
{
	/// <summary>获取层次结构节点的子节点集合。</summary>
	IHierarchicalNodeCollection<TNode> Nodes { get; }
}