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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components;

public class CommandTreeNodeCollection : Collections.HierarchicalNodeCollection<CommandTreeNode>, ICollection<ICommand>
{
	#region 构造函数
	public CommandTreeNodeCollection() : base(null) { }
	internal CommandTreeNodeCollection(CommandTreeNode owner) : base(owner) { }
	#endregion

	#region 公共方法
	public CommandTreeNode Add(string name)
	{
		var node = new CommandTreeNode(name);
		this.Add(node);
		return node;
	}

	public CommandTreeNode Add(ICommand command)
	{
		var node = new CommandTreeNode(command ?? throw new ArgumentNullException(nameof(command)));
		this.Add(node);
		return node;
	}

	public bool Remove(ICommand command) => command != null && base.Remove(command.Name);
	public bool Contains(ICommand command) => command != null && this.Contains(command.Name);
	public void CopyTo(ICommand[] array, int arrayIndex)
	{
		if(array == null)
			throw new ArgumentNullException(nameof(array));

		if(arrayIndex < 0 || arrayIndex >= array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));

		var iterator = base.GetEnumerator();

		for(int i = arrayIndex; i < array.Length; i++)
		{
			if(iterator.MoveNext())
				array[i] = iterator.Current?.Command;
		}
	}
	#endregion

	#region 重写方法
	protected override void SetOwner(CommandTreeNode owner, CommandTreeNode node) => node?.SetParent(owner);
	#endregion

	#region 接口实现
	bool ICollection<ICommand>.IsReadOnly => false;
	void ICollection<ICommand>.Add(ICommand command) => this.Add(command);
	IEnumerator<ICommand> IEnumerable<ICommand>.GetEnumerator()
	{
		var iterator = base.GetEnumerator();

		while(iterator.MoveNext())
			yield return iterator.Current?.Command;
	}
	#endregion
}
