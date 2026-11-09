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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components;

public class CommandNodeCollection : Collections.HierarchicalNodeCollection<CommandNode>, ICollection<ICommand>
{
	#region 构造函数
	public CommandNodeCollection() : base(null) { }
	internal CommandNodeCollection(CommandNode owner) : base(owner) { }
	#endregion

	#region 公共方法
	public CommandNode Add(string name)
	{
		var node = new CommandNode(name);
		this.Add(node);
		return node;
	}

	public CommandNode Add(ICommand command)
	{
		var node = new CommandNode(command ?? throw new ArgumentNullException(nameof(command)));
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
	protected override void SetOwner(CommandNode owner, CommandNode node) => node?.SetParent(owner);
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
