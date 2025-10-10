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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.ObjectModel;

namespace Zongsoft.Components;

public class CommandOptionDescriptorCollection : Collection<CommandOptionDescriptor>
{
	#region 成员字段
	private readonly Dictionary<string, CommandOptionDescriptor> _dictionary = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	public CommandOptionDescriptor this[string name]
	{
		get
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			return _dictionary.TryGetValue(name, out var option) ? option : null;
		}
	}
	#endregion

	#region 公共方法
	public bool TryGetValue(string name, out CommandOptionDescriptor value)
	{
		if(string.IsNullOrEmpty(name))
		{
			value = null;
			return false;
		}

		return _dictionary.TryGetValue(name, out value);
	}
	#endregion

	#region 重写方法
	protected override void ClearItems()
	{
		base.ClearItems();
		_dictionary.Clear();
	}

	protected override void InsertItem(int index, CommandOptionDescriptor item)
	{
		if(item == null)
			throw new ArgumentNullException(nameof(item));

		if(this.TryAdd(item))
			base.InsertItem(index, item);
		else
			throw ThrowDuplicated(item);
	}

	protected override void RemoveItem(int index)
	{
		if(index < 0 || index >= base.Count)
			throw new ArgumentOutOfRangeException(nameof(index));

		var item = base[index];

		_dictionary.Remove(item.Name);
		if(item.Symbol != '\0')
			_dictionary.Remove(item.Symbol.ToString());

		base.RemoveItem(index);
	}

	protected override void SetItem(int index, CommandOptionDescriptor item)
	{
		if(index < 0 || index >= base.Count)
			throw new ArgumentOutOfRangeException(nameof(index));

		var older = base[index];

		_dictionary.Remove(older.Name);
		if(item.Symbol != '\0')
			_dictionary.Remove(older.Symbol.ToString());

		if(this.TryAdd(item))
			base.SetItem(index, item);
		else
			throw ThrowDuplicated(item);
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static ArgumentException ThrowDuplicated(CommandOptionDescriptor option) =>
		option.Symbol == '\0' ?
			new ArgumentException($"Duplicate option name: “{option.Name}”.", nameof(option)) :
			new ArgumentException($"Duplicate option name or abbreviation: “{option.Name}” or ‘{option.Symbol}’.", nameof(option));

	private bool TryAdd(CommandOptionDescriptor option)
	{
		if(option == null)
			throw new ArgumentNullException(nameof(option));

		if(_dictionary.TryAdd(option.Name, option))
		{
			if(option.Symbol == '\0')
				return true;

			if(_dictionary.TryAdd(option.Symbol.ToString(), option))
				return true;
			else
				_dictionary.Remove(option.Name);
		}

		return false;
	}
	#endregion
}
