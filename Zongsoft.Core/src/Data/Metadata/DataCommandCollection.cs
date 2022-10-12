/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data.Metadata
{
	public class DataCommandCollection : IDataCommandCollection
	{
		#region 成员字段
		private readonly IDataMetadataContainer _container;
		private readonly Dictionary<string, IDataCommand> _dictionary;
		#endregion

		#region 构造函数
		public DataCommandCollection(IDataMetadataContainer container)
		{
			_container = container ?? throw new ArgumentNullException(nameof(container));
			_dictionary = new Dictionary<string, IDataCommand>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		public int Count => _dictionary.Count;
		bool ICollection<IDataCommand>.IsReadOnly => false;
		public IDataCommand this[string name, string @namespace = null]
		{
			get
			{
				var key = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

				if(_dictionary.TryGetValue(key, out var command))
					return command;

				throw new DataException($"The specified '{key}' command mapping does not exist.");
			}
		}
		#endregion

		#region 公共方法
		public void Add(IDataCommand command)
		{
			CheckContainer(command);
			_dictionary.Add(GetKey(command), command);
			command.Container = _container;
		}

		public bool TryAdd(IDataCommand command)
		{
			CheckContainer(command);

			if(_dictionary.TryAdd(GetKey(command), command))
			{
				command.Container = _container;
				return true;
			}

			return false;
		}

		public void Clear() => _dictionary.Clear();
		public bool Contains(IDataCommand command) => command != null && _dictionary.ContainsKey(GetKey(command));
		public bool Contains(string name, string @namespace = null)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			return string.IsNullOrEmpty(@namespace) ? _dictionary.ContainsKey(name) : _dictionary.ContainsKey($"{@namespace}.{name}");
		}

		public bool Remove(IDataCommand command)
		{
			if(command != null && _dictionary.Remove(GetKey(command), out var value))
			{
				value.Container = null;
				return true;
			}

			return false;
		}

		public bool Remove(string name, string @namespace = null)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			var result = string.IsNullOrEmpty(@namespace) ? _dictionary.Remove(name, out var command) : _dictionary.Remove($"{@namespace}.{name}", out command);
			if(result)
				command.Container = null;

			return result;
		}

		public bool TryGetValue(string name, out IDataCommand value) => _dictionary.TryGetValue(name, out value);
		public bool TryGetValue(string name, string @namespace, out IDataCommand value) => _dictionary.TryGetValue(string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}", out value);

		public void CopyTo(IDataCommand[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException(nameof(array));
			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			_dictionary.Values.CopyTo(array, arrayIndex);
		}
		#endregion

		#region 枚举遍历
		public IEnumerator<IDataCommand> GetEnumerator() => _dictionary.Values.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		#endregion

		#region 私有方法
		private static string GetKey(IDataCommand command)
		{
			if(command is null)
				throw new ArgumentNullException(nameof(command));

			return command.QualifiedName;
		}

		private void CheckContainer(IDataCommand command)
		{
			if(command != null && command.Container != null && !object.ReferenceEquals(_container, command.Container))
				throw new DataException($"The specified '{command}' data command mapping did not detach the container.");
		}
		#endregion
	}
}
