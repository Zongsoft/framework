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
using System.Collections.ObjectModel;

namespace Zongsoft.Collections
{
	public abstract class HierarchicalNodeCollection<TNode>(TNode owner) :
		KeyedCollection<string, TNode>(StringComparer.OrdinalIgnoreCase),
		IHierarchicalNodeCollection<TNode>
		where TNode : IHierarchicalNode<TNode>
	{
		#region 常量定义
		private const string ROOTED_ERROR_MESSAGE = @"The root node cannot be added to the child nodes.";
		#endregion

		#region 成员字段
		private readonly TNode _owner = owner;
		#endregion

		#region 保护属性
		protected TNode Owner => _owner;
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(TNode node) => node.Name;

		protected override void RemoveItem(int index)
		{
			var node = base.Items[index];
			this.SetOwner(default, node);
		}

		protected override void InsertItem(int index, TNode node)
		{
			if(node == null)
				throw new ArgumentNullException(nameof(node));

			if(node.IsRoot())
				throw new ArgumentException(ROOTED_ERROR_MESSAGE);

			base.InsertItem(index, node);
			this.SetOwner(_owner, node);
		}

		protected override void SetItem(int index, TNode node)
		{
			if(node == null)
				throw new ArgumentNullException(nameof(node));

			if(node.IsRoot())
				throw new ArgumentException(ROOTED_ERROR_MESSAGE);

			base.SetItem(index, node);
			this.SetOwner(_owner, node);
		}
		#endregion

		#region 抽象方法
		protected  abstract void SetOwner(TNode owner, TNode node);
		#endregion
	}
}
