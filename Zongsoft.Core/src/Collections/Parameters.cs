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
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Collections
{
	public partial class Parameters : IDictionary<object, object>
	{
		#region 成员字段
		private int _initialization;
		private Dictionary<object, object> _cache;
		#endregion

		#region 构造函数
		public Parameters() { }
		public Parameters(IEnumerable<KeyValuePair<string, object>> parameters)
		{
			_initialization = 0;

			if(parameters != null && parameters.Any())
				_cache = new(parameters.Select(entry => new KeyValuePair<object, object>(entry.Key ?? string.Empty, entry.Value)), Comparer.Instance);
		}
		#endregion

		#region 公共属性
		public int Count => _cache?.Count ?? 0;
		public bool IsEmpty => _cache == null || _cache.Count == 0;
		public bool HasValue => _cache != null && _cache.Count > 0;

		public object this[string name]
		{
			get => this.GetValue(name);
			set => this.SetValue(name, value);
		}
		public object this[Type type]
		{
			get => this.GetValue(type);
			set => this.SetValue(type, value);
		}
		object IDictionary<object, object>.this[object key]
		{
			get => this.GetValue(key);
			set => this.SetValue(key, value);
		}
		#endregion

		#region 静态方法
		public static Parameters Parameter(string name, object value) => new([new KeyValuePair<string, object>(name ?? string.Empty, value)]);
		public static Parameters Parameter(object value) => Parameter(value != null ? value.GetType() : throw new ArgumentNullException(nameof(value)), value);
		public static Parameters Parameter<T>(object value) => Parameter(typeof(T), value);
		public static Parameters Parameter(Type type, object value)
		{
			var parameters = new Parameters();
			parameters.SetValue(type, value);
			return parameters;
		}
		#endregion

		#region 类型转换
		public static implicit operator Parameters(Dictionary<string, object> parameters) => new(parameters);
		#endregion

		#region 公共方法
		public void Clear() => _cache?.Clear();

		public bool Contains<T>() => this.Contains((object)typeof(T));
		public bool Contains(Type type) => this.Contains((object)type);
		public bool Contains(string name) => this.Contains((object)(name ?? string.Empty));
		private bool Contains(object key) => key != null && _cache != null && _cache.ContainsKey(key);

		public object GetValue(Type type) => this.GetValue((object)type);
		public object GetValue(string name) => this.GetValue((object)name);
		public T GetValue<T>(T defaultValue = default) => (T)(this.GetValue((object)typeof(T)) ?? defaultValue);
		private object GetValue(object key)
		{
			var parameters = _cache;
			if(parameters == null)
				return null;

			object value;

			return key is Type type ?
				parameters.TryGetValue(type, out value) ? value : this.Find(type):
				parameters.TryGetValue(key ?? string.Empty, out value) ? value : null;
		}

		public bool TryGetValue<T>(out T value)
		{
			if(this.TryGetValue((object)typeof(T), out var result))
			{
				value = (T)result;
				return true;
			}

			value = default;
			return false;
		}

		public bool TryGetValue(Type type, out object value) => this.TryGetValue((object)type, out value);
		public bool TryGetValue(string name, out object value) => this.TryGetValue((object)(name ?? string.Empty), out value);
		private bool TryGetValue(object key, out object value)
		{
			value = null;
			var parameters = _cache;
			if(parameters == null)
				return false;

			return key is Type type ?
				parameters.TryGetValue(type, out value) ? true : this.Find(type, out value) :
				parameters.TryGetValue(key ?? string.Empty, out value);
		}

		public void SetValue(object value) => this.SetValue(value?.GetType(), value);
		public void SetValue<T>(object value) => this.SetValue(typeof(T), value);
		public void SetValue(Type type, object value)
		{
			if(value == null)
			{
				if(type == null)
					return;

				if(type.IsValueType && !Common.TypeExtension.IsNullable(type))
					throw new ArgumentException($"The specified parameter value is null, but the declared type is not the Nullable type.");
			}
			else
			{
				if(type == null)
					type = value.GetType();
				else if(!type.IsAssignableFrom(value.GetType()))
					throw new ArgumentException($"The specified parameter value cannot be converted to the declared type '{type.FullName}'.");
			}

			this.SetValue((object)type, value);
		}

		public void SetValue(string name, object value) => this.SetValue((object)name ?? string.Empty, value);
		private void SetValue(object key, object value)
		{
			if(key == null)
				throw new ArgumentNullException(nameof(key));

			if(_cache == null)
			{
				var initialized = Interlocked.Exchange(ref _initialization, 1);
				if(initialized == 0)
					_cache = new(Comparer.Instance);
			}

			_cache[key] = value;
		}

		public void SetValue(IEnumerable<KeyValuePair<string, object>> values)
		{
			if(values == null)
				return;

			foreach(var entry in values)
				this.SetValue(entry.Key, entry.Value);
		}

		public void SetValue(IEnumerable<KeyValuePair<Type, object>> values)
		{
			if(values == null)
				return;

			foreach(var entry in values)
				this.SetValue(entry.Key, entry.Value);
		}

		public bool Remove<T>() => this.Remove((object)typeof(T));
		public bool Remove<T>(out object value) => this.Remove((object)typeof(T), out value);
		public bool Remove(Type type) => this.Remove((object)type);
		public bool Remove(Type type, out object value) => this.Remove((object)type, out value);
		public bool Remove(string name) => this.Remove((object)name);
		public bool Remove(string name, out object value) => this.Remove((object)name, out value);
		private bool Remove(object key) => _cache != null && _cache.Remove(key);
		private bool Remove(object key, out object value)
		{
			value = null;
			return _cache != null && _cache.Remove(key, out value);
		}
		#endregion

		#region 显式实现
		bool ICollection<KeyValuePair<object, object>>.IsReadOnly => false;
		ICollection<object> IDictionary<object, object>.Keys => _cache == null ? [] : _cache.Keys;
		ICollection<object> IDictionary<object, object>.Values => _cache == null ? [] : _cache.Values;
		void IDictionary<object, object>.Add(object key, object value) => this.SetValue(key, value);
		bool IDictionary<object, object>.Remove(object key) => this.Remove(key);
		bool IDictionary<object, object>.TryGetValue(object key, out object value) => this.TryGetValue(key, out value);
		bool IDictionary<object, object>.ContainsKey(object key) => key != null && _cache != null && _cache.ContainsKey(key);
		void ICollection<KeyValuePair<object, object>>.Add(KeyValuePair<object, object> entry) => this.SetValue(entry.Key, entry.Value);
		bool ICollection<KeyValuePair<object, object>>.Contains(KeyValuePair<object, object> entry) => this.Contains(entry.Key);
		bool ICollection<KeyValuePair<object, object>>.Remove(KeyValuePair<object, object> entry) => this.Remove(entry.Key);
		void ICollection<KeyValuePair<object, object>>.CopyTo(KeyValuePair<object, object>[] array, int index)
		{
			if(array == null) throw new ArgumentNullException(nameof(array));
			if(index < 0 || index >= array.Length) throw new ArgumentOutOfRangeException(nameof(index));

			if(_cache == null)
				return;

			foreach(var parameter in _cache)
			{
				array[index++] = parameter;
				if(index >= array.Length)
					break;
			}
		}
		#endregion

		#region 私有方法
		private object Find(Type type) => this.Find(type, out var value) ? value : null;
		private bool Find(Type type, out object value)
		{
			if(type != null)
			{
				foreach(var entry in _cache)
				{
					if(entry.Key is Type key && type.IsAssignableFrom(key))
					{
						value = entry.Value;
						return true;
					}
				}
			}

			value = null;
			return false;
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<object, object>> GetEnumerator()
		{
			var parameters = _cache;

			if(parameters != null)
			{
				foreach(var entry in parameters)
					yield return entry;
			}
		}
		#endregion

		#region 嵌套子类
		private class Comparer : IEqualityComparer<object>
		{
			public static readonly Comparer Instance = new();

			public new bool Equals(object x, object y)
			{
				if(x == null)
					return y == null;

				if(y == null)
					return false;

				if(x.GetType() != y.GetType())
					return false;

				if(x.GetType() == typeof(string))
					return string.Equals((string)x, (string)y, StringComparison.OrdinalIgnoreCase);

				return object.Equals(x, y);
			}

			public int GetHashCode(object obj)
			{
				if(obj == null)
					return 0;

				return obj is string text ? text.ToUpperInvariant().GetHashCode() : obj.GetHashCode();
			}
		}
		#endregion
	}
}
