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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Serialization;

namespace Zongsoft.Collections;

public abstract class HierarchicalNode<TNode> : HierarchicalNode, IHierarchicalNode<TNode> where TNode : HierarchicalNode<TNode>
{
	#region 构造函数
	protected HierarchicalNode() { }
	protected HierarchicalNode(string name) : base(name) { }
	#endregion

	#region 公共属性
	[SerializationMember(Ignored = true)]
	[System.Text.Json.Serialization.JsonIgnore]
	protected abstract TNode Parent { get; }

	[SerializationMember(Ignored = true)]
	[System.Text.Json.Serialization.JsonIgnore]
	protected abstract IHierarchicalNodeCollection<TNode> Nodes { get; }
	IHierarchicalNodeCollection<TNode> IHierarchicalNode<TNode>.Nodes => this.Nodes;
	#endregion

	#region 公共方法
	protected TNode FindRoot()
	{
		var current = (TNode)this;
		var stack = new Stack<TNode>();

		while(current != null)
		{
			if(current.Parent == null)
				return current;

			//如果当前节点是否已经在遍历的栈中，则抛出循环引用的异常
			if(stack.Contains(current))
				throw new InvalidOperationException($"The “{this.Name}” {this.GetType().Name} has circular references in the hierarchy tree.");

			//将当前节点加入到遍历栈中
			stack.Push(current);

			//指向当前节点的父节点
			current = current.Parent;
		}

		return current;
	}
	#endregion

	#region 公共方法
	public TNode Find(ReadOnlySpan<char> path) => this.FindNode(path);
	protected TNode FindNode(ReadOnlySpan<char> path, Func<HierarchicalNodeToken, TNode> onStep = null)
	{
		//注意：一定要确保空字符串路径是返回自身
		if(path.IsEmpty)
			return (TNode)this;

		//准备开始查找下级节点
		this.OnFinding(path);

		//当前节点默认为本节点
		TNode current = (TNode)this;

		int last = 0;
		int spaces = 0;
		int index = 0;

		for(int i = 0; i < path.Length; i++)
		{
			if(path[i] == PathSeparator)
			{
				if(index++ == 0)
				{
					if(last == i)
						current = this.FindRoot();
				}

				if(i - last > spaces)
				{
					current = FindStep(current, index - 1, path, i, last, spaces, onStep);

					if(current == null)
						return null;
				}

				spaces = -1;
				last = i + 1;
			}
			else if(char.IsWhiteSpace(path[i]))
			{
				if(i == last)
					last = i + 1;
				else
					spaces++;
			}
			else
			{
				spaces = 0;
			}
		}

		if(last < path.Length - spaces - 1)
			current = FindStep(current, index, path, path.Length, last, spaces, onStep);

		//查找下级节点完成
		this.OnFounded(path, current);

		//返回查找到的节点
		return current;
	}
	#endregion

	#region 虚拟方法
	protected virtual void OnFinding(ReadOnlySpan<char> path) { }
	protected virtual void OnFounded(ReadOnlySpan<char> path, TNode node) { }
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static TNode FindStep(TNode current, int index, ReadOnlySpan<char> path, int position, int last, int spaces, Func<HierarchicalNodeToken, TNode> onStep)
	{
		var part = path.Slice(last, position - last - spaces);
		TNode parent = null;

		switch(part)
		{
			case "":
			case ".":
				return current;
			case "..":
				if(current.Parent != null)
					current = current.Parent;

				break;
			default:
				parent = current;
				current = parent.Nodes.TryGetValue(part.ToString(), out var child) ? child : null;
				break;
		}

		if(onStep != null)
			current = onStep(new HierarchicalNodeToken(index, part, current, parent));

		return current;
	}
	#endregion

	#region 嵌套子类
	internal protected readonly struct HierarchicalNodeToken
	{
		internal HierarchicalNodeToken(int index, ReadOnlySpan<char> name, TNode current, TNode parent = null)
		{
			this.Index = index;
			this.Name = name.ToString();
			this.Current = current;
			this.Parent = parent ?? (current?.Parent);
		}

		public readonly string Name;
		public readonly int Index;
		public readonly TNode Parent;
		public readonly TNode Current;

		public override string ToString() => $"{this.Name}#{this.Index}";
	}
	#endregion
}
