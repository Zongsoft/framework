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
using System.Collections.Concurrent;

namespace Zongsoft.Collections
{
	public struct Parameters : IEnumerable<KeyValuePair<string, object>>
	{
		#region 常量字段
		private const int SHRINK_COUNT = 1000; //收缩的数量底线
		private const int SHRINK_SINCE = 30;   //收缩的时长间隔（单位：秒）
		#endregion

		#region 静态字段
		private static readonly ConcurrentDictionary<WeakReference, Dictionary<string, object>> _cache = new();
		#endregion

		#region 成员字段
		private readonly WeakReference _owner;
		#endregion

		#region 构造函数
		public Parameters(object owner, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			if(owner == null)
				throw new ArgumentNullException(nameof(owner));

			_owner = new WeakReference(owner);

			if(parameters != null && parameters.Any())
				_cache.TryAdd(_owner, new Dictionary<string, object>(parameters, StringComparer.OrdinalIgnoreCase));
		}
		#endregion

		#region 公共属性
		public int Count { get => _owner.IsAlive && _cache.TryGetValue(_owner, out var dictionary) ? dictionary.Count : 0; }
		public bool IsEmpty { get => !_owner.IsAlive || !_cache.TryGetValue(_owner, out var dictionary) || dictionary.Count == 0; }
		public object this[string name]
		{
			get => _owner.IsAlive && _cache.TryGetValue(_owner, out var dictionary) ? dictionary[name] : null;
			set => this.SetValue(name, value);
		}
		#endregion

		#region 公共方法
		public void Clear()
		{
			if(_owner.IsAlive && _cache.TryGetValue(_owner, out var dictionary))
				dictionary.Clear();
		}

		public bool Contains(string name) => _owner.IsAlive && name != null && _cache.TryGetValue(_owner, out var dictionary) && dictionary.ContainsKey(name);

		public bool TryGetValue(string name, out object value)
		{
			value = null;
			return _owner.IsAlive && name != null && _cache.TryGetValue(_owner, out var dictionary) && dictionary.TryGetValue(name, out value);
		}

		public bool HasValue(string name) => _owner.IsAlive && name != null && _cache.TryGetValue(_owner, out var dictionary) && dictionary.ContainsKey(name);
		public bool HasValue(string name, out object value)
		{
			value = null;
			return _owner.IsAlive && name != null && _cache.TryGetValue(_owner, out var dictionary) && dictionary.TryGetValue(name, out value);
		}

		public void SetValue(string name, object value)
		{
			if(name == null)
				throw new ArgumentNullException(nameof(name));

			if(!_owner.IsAlive)
				return;

			if(_cache.Count > SHRINK_COUNT)
				Shrink();

			var dictionary = _cache.GetOrAdd(_owner, _ => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
			dictionary[name] = value;
		}

		public void SetValue(IEnumerable<KeyValuePair<string, object>> parameters)
		{
			if(parameters == null || !parameters.Any())
				return;

			if(!_owner.IsAlive)
				return;

			if(_cache.Count > SHRINK_COUNT)
				Shrink();

			var dictionary = _cache.GetOrAdd(_owner, _ => new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));
			foreach(var parameter in parameters)
				dictionary[parameter.Key] = parameter.Value;
		}

		public bool Remove(string name) => _owner.IsAlive && name != null && _cache.TryGetValue(_owner, out var dictionary) && dictionary.Remove(name);
		public bool Remove(string name, out object value)
		{
			value = null;
			return _owner.IsAlive && name != null && _cache.TryGetValue(_owner, out var dictionary) && dictionary.Remove(name, out value);
		}
		#endregion

		#region 收缩方法
		private static long _timestamp;
		private static void Shrink()
		{
			//如果距离上次收缩的时间低于指定秒数，则忽略本次收缩请求
			if(_timestamp > 0 && Environment.TickCount64 - _timestamp < SHRINK_SINCE * 1000)
				return;

			foreach(var key in _cache.Keys)
			{
				if(!key.IsAlive)
					_cache.TryRemove(key, out _);
			}

			_timestamp = Environment.TickCount64;
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
		{
			if(_owner.IsAlive && _cache.TryGetValue(_owner, out var dictionary))
			{
				foreach(var entry in dictionary)
					yield return entry;
			}
		}
		#endregion
	}
}
