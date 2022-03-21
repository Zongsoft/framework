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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Collections
{
	public class Parameters : IEnumerable<KeyValuePair<string, object>>
	{
		#region 成员字段
		private Dictionary<string, object> _dictionary;
		#endregion

		#region 构造函数
		public Parameters() { }

		public Parameters(Parameters parameters)
		{
			var dictionary = parameters._dictionary;

			if(dictionary != null && dictionary.Count > 0)
				_dictionary = new Dictionary<string, object>(dictionary, StringComparer.OrdinalIgnoreCase);
			else
				_dictionary = null;
		}

		public Parameters(IEnumerable<KeyValuePair<string, object>> parameters)
		{
			if(parameters != null && parameters.Any())
				_dictionary = new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase);
			else
				_dictionary = null;
		}
		#endregion

		#region 公共属性
		public int Count { get => _dictionary == null ? 0 : _dictionary.Count; }
		public bool IsEmpty { get => _dictionary == null || _dictionary.Count == 0; }
		public object this[string name]
		{
			get => _dictionary?[name];
			set => this.SetValue(name, value);
		}
		#endregion

		#region 公共方法
		public void Clear() => _dictionary?.Clear();
		public bool Contains(string name) => name != null && _dictionary != null && _dictionary.ContainsKey(name);

		public bool TryGetValue(string name, out object value)
		{
			value = null;
			return name != null && _dictionary != null && _dictionary.TryGetValue(name, out value);
		}

		public bool HasValue(string name) => name != null && _dictionary != null && _dictionary.ContainsKey(name);
		public bool HasValue(string name, out object value)
		{
			value = null;
			return name != null && _dictionary != null && _dictionary.TryGetValue(name, out value);
		}

		public void SetValue(string name, object value)
		{
			if(name == null)
				throw new ArgumentNullException(nameof(name));

			if(_dictionary == null)
				System.Threading.Interlocked.CompareExchange(ref _dictionary, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

			_dictionary[name] = value;
		}

		public void SetValue(IEnumerable<KeyValuePair<string, object>> parameters)
		{
			if(parameters == null || !parameters.Any())
				return;

			if(_dictionary == null)
				System.Threading.Interlocked.CompareExchange(ref _dictionary, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

			foreach(var parameter in parameters)
				_dictionary[parameter.Key] = parameter.Value;
		}

		public bool Remove(string name) => name != null && _dictionary != null && _dictionary.Remove(name);
		public bool Remove(string name, out object value)
		{
			value = null;
			return name != null && _dictionary != null && _dictionary.Remove(name, out value);
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			var dictionary = _dictionary;

			if(dictionary == null)
				yield break;

			foreach(var entry in dictionary)
				yield return entry;
		}
		#endregion
	}
}
