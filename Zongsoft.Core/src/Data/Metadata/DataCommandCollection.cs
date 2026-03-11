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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Data.Metadata;

public class DataCommandCollection() : ICollection<IDataCommand>
{
	#region 成员字段
	private readonly ConcurrentDictionary<string, IDataCommand> _dictionary = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共属性
	public int Count => _dictionary.Count;
	public IDataCommand this[string qualifiedName] => _dictionary[qualifiedName];
	public IDataCommand this[string name, string @namespace] => _dictionary[DataUtility.Qualify(name, @namespace)];
	#endregion

	#region 集合方法
	public void Add(IDataCommand command)
	{
		ArgumentNullException.ThrowIfNull(command);
		if(!_dictionary.TryAdd(command.QualifiedName, command))
			throw new InvalidOperationException($"The specified '{command.QualifiedName}' data command already exists in the collection.");
	}

	public bool TryAdd(IDataCommand command) => command != null && _dictionary.TryAdd(command.QualifiedName, command);
	public IDataCommand GetOrAdd(IDataCommand command) => command == null ? null : _dictionary.GetOrAdd(command.QualifiedName, command);

	public bool Contains(string qualifiedName) => qualifiedName != null && _dictionary.ContainsKey(qualifiedName);
	public bool Contains(string name, string @namespace) => name != null && _dictionary.ContainsKey(DataUtility.Qualify(name, @namespace));

	public void Clear() => _dictionary.Clear();
	public bool Remove(string qualifiedName) => qualifiedName != null && _dictionary.TryRemove(qualifiedName, out _);
	public bool Remove(string name, string @namespace) => name != null && _dictionary.TryRemove(DataUtility.Qualify(name, @namespace), out _);
	public bool Remove(string qualifiedName, out IDataCommand command) => _dictionary.TryRemove(qualifiedName, out command);
	public bool Remove(string name, string @namespace, out IDataCommand command) => _dictionary.TryRemove(DataUtility.Qualify(name, @namespace), out command);
	public bool TryGetValue(string qualifiedName, out IDataCommand command) => _dictionary.TryGetValue(qualifiedName, out command);
	public bool TryGetValue(string name, string @namespace, out IDataCommand command) => _dictionary.TryGetValue(DataUtility.Qualify(name, @namespace), out command);
	#endregion

	#region 公共方法
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

		return _dictionary.GetOrAdd(key, (key, argument) =>
		{
			var command = new DataCommand(null, key, argument.mutability).Script(argument.driver, argument.script);

			if(argument.parameters != null)
			{
				foreach(var parameter in argument.parameters)
					command.Parameters.Add(parameter);
			}

			return command;
		}, (driver, mutability, script, parameters));
	}
	#endregion

	#region 显式实现
	bool ICollection<IDataCommand>.IsReadOnly => false;
	bool ICollection<IDataCommand>.Contains(IDataCommand command) => command != null && _dictionary.ContainsKey(command.QualifiedName);
	bool ICollection<IDataCommand>.Remove(IDataCommand command) => command != null && _dictionary.TryRemove(command.QualifiedName, out _);
	void ICollection<IDataCommand>.CopyTo(IDataCommand[] array, int arrayIndex)
	{
		ArgumentNullException.ThrowIfNull(array);
		ArgumentOutOfRangeException.ThrowIfLessThan(arrayIndex, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(arrayIndex, array.Length);

		using var enumerator = _dictionary.GetEnumerator();

		for(int i = arrayIndex; i < array.Length; i++)
		{
			if(enumerator.MoveNext())
				array[i] = enumerator.Current.Value;
			else
				break;
		}
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<IDataCommand> GetEnumerator() => _dictionary.Values.GetEnumerator();
	#endregion
}
