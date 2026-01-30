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
using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Metadata;

public class DataCommandCollection() : KeyedCollection<string, IDataCommand>(StringComparer.OrdinalIgnoreCase)
{
	#region 成员字段
	private readonly Dictionary<string, IDataCommand[]> _aliases = new();
	#endregion

	#region 公共属性
	public IDataCommand this[string name, string @namespace = null] => base[GetKey(name, @namespace)];
	#endregion

	#region 公共方法
	public bool TryAdd(IDataCommand command)
	{
		if(command == null)
			throw new ArgumentNullException(nameof(command));

		if(this.Contains(command))
			return false;

		this.Add(command);
		return true;
	}

	public bool Contains(string name, string @namespace = null) => base.Contains(GetKey(name, @namespace));
	public bool Remove(string name, string @namespace = null) => base.Remove(GetKey(name, @namespace));
	public bool TryGetValue(string name, string @namespace, out IDataCommand value) => base.TryGetValue(GetKey(name, @namespace), out value);

	/// <summary>根据别名查找对应的数据命令。</summary>
	/// <param name="alias">指定要查找的数据命令别名。</param>
	/// <returns>返回找到的数据命令数组，如果为空则表示查找失败。</returns>
	public IDataCommand[] Find(string alias)
	{
		if(string.IsNullOrEmpty(alias))
			return null;

		return _aliases.TryGetValue(alias, out var commands) ? commands : base.TryGetValue(alias, out var command) ? [command] : null;
	}

	/// <summary>定义一个匿名的数据命令并加入到当前命令集中。</summary>
	/// <param name="driver">指定的数据驱动名。</param>
	/// <param name="script">指定的数据命令脚本。</param>
	/// <param name="parameters">指定的数据命令参数集。</param>
	/// <returns>返回定义的数据命令。</returns>
	public IDataCommand Script(string driver, string script, params IEnumerable<DataCommandParameter> parameters) => this.Script(driver, DataCommandMutability.None, script, parameters);

	/// <summary>定义一个匿名的数据命令并加入到当前命令集中。</summary>
	/// <param name="driver">指定的数据驱动名。</param>
	/// <param name="mutable">指定的命令可变性，如果为真(<c>True</c>)则表示命令为写操作。</param>
	/// <param name="script">指定的数据命令脚本。</param>
	/// <param name="parameters">指定的数据命令参数集。</param>
	/// <returns>返回定义的数据命令。</returns>
	public IDataCommand Script(string driver, bool mutable, string script, params IEnumerable<DataCommandParameter> parameters) => this.Script(driver, mutable ? DataCommandMutability.Delete | DataCommandMutability.Insert | DataCommandMutability.Update : DataCommandMutability.None, script, parameters);

	/// <summary>定义一个匿名的数据命令并加入到当前命令集中。</summary>
	/// <param name="driver">指定的数据驱动名。</param>
	/// <param name="mutability">指定的命令可变性。</param>
	/// <param name="script">指定的数据命令脚本。</param>
	/// <param name="parameters">指定的数据命令参数集。</param>
	/// <returns>返回定义的数据命令。</returns>
	public IDataCommand Script(string driver, DataCommandMutability mutability, string script, params IEnumerable<DataCommandParameter> parameters)
	{
		ArgumentException.ThrowIfNullOrEmpty(driver);
		ArgumentException.ThrowIfNullOrEmpty(script);

		var key = $"#{Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(Encoding.UTF8.GetBytes($"{driver.ToUpperInvariant()}:{script}")))}";

		if(this.TryGetValue(key, out var command))
			return command;

		command = new DataCommand(null, key, mutability).Script(driver, script);
		if(parameters != null)
		{
			foreach(var parameter in parameters)
				command.Parameters.Add(parameter);
		}

		base.Remove(key);
		base.Add(command);

		return command;
	}
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(IDataCommand command) => command.QualifiedName;

	protected override void ClearItems()
	{
		base.ClearItems();
		_aliases.Clear();
	}

	protected override void InsertItem(int index, IDataCommand command)
	{
		if(command == null)
			return;

		base.InsertItem(index, command);

		if(!string.IsNullOrEmpty(command.Alias))
		{
			if(!_aliases.TryAdd(command.Alias, [command]))
			{
				if(_aliases.TryGetValue(command.Alias, out var entities))
					_aliases[command.Alias] = [.. entities, command];
				else
					_aliases[command.Alias] = [command];
			}
		}
	}

	protected override void RemoveItem(int index)
	{
		var item = this.Items[index];

		base.RemoveItem(index);

		if(item != null && !string.IsNullOrEmpty(item.Alias))
			_aliases.Remove(item.Alias);
	}

	protected override void SetItem(int index, IDataCommand command)
	{
		if(command == null)
			return;

		if(index >= 0 && index < this.Items.Count)
		{
			var item = this.Items[index];

			if(item != null && !string.IsNullOrEmpty(item.Alias))
				_aliases.Remove(item.Alias);
		}

		base.SetItem(index, command);

		if(!string.IsNullOrEmpty(command.Alias))
		{
			if(!_aliases.TryAdd(command.Alias, [command]))
			{
				if(_aliases.TryGetValue(command.Alias, out var entities))
					_aliases[command.Alias] = [.. entities, command];
				else
					_aliases[command.Alias] = [command];
			}
		}
	}
	#endregion

	#region 私有方法
	private static string GetKey(string name, string @namespace) => string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";
	#endregion
}
