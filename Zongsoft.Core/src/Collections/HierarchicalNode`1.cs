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

namespace Zongsoft.Collections
{
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
		public TNode Find(string path, Func<HierarchicalNodeToken, TNode> step = null)
		{
			if(step == null)
				return (TNode)base.FindNode(path, 0, 0);
			else
				return (TNode)base.FindNode(path, 0, 0, token => step(new HierarchicalNodeToken(token)));
		}

		public TNode Find(string path, int startIndex, int length = 0, Func<HierarchicalNodeToken, TNode> step = null)
		{
			if(step == null)
				return (TNode)base.FindNode(path, startIndex, length);
			else
				return (TNode)base.FindNode(path, startIndex, length, token => step(new HierarchicalNodeToken(token)));
		}

		public TNode Find(string[] parts, Func<HierarchicalNodeToken, TNode> step = null)
		{
			if(step == null)
				return (TNode)base.FindNode(parts);
			else
				return (TNode)base.FindNode(parts, token => step(new HierarchicalNodeToken(token)));
		}

		public TNode Find(string[] parts, int startIndex, int count = 0, Func<HierarchicalNodeToken, TNode> step = null)
		{
			if(step == null)
				return (TNode)base.FindNode(parts, startIndex, count);
			else
				return (TNode)base.FindNode(parts, startIndex, count, token => step(new HierarchicalNodeToken(token)));
		}
		#endregion
	}
}
