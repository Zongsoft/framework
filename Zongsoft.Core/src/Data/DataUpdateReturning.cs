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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

public sealed class DataUpdateReturning
{
	#region 成员字段
	private readonly Dictionary<MemberKey, object> _members;
	private readonly MemberView _newer;
	private readonly MemberView _older;
	#endregion

	#region 构造函数
	public DataUpdateReturning(IEnumerable<string> newer, IEnumerable<string> older)
	{
		_members = new Dictionary<MemberKey, object>();
		_newer = new MemberView(MemberKind.Newer, _members);
		_older = new MemberView(MemberKind.Older, _members);

		if(newer != null)
		{
			foreach(var name in newer)
				_members.Add(new MemberKey(name, MemberKind.Newer), null);
		}

		if(older != null)
		{
			foreach(var name in older)
				_members.Add(new MemberKey(name, MemberKind.Older), null);
		}
	}
	#endregion

	#region 公共属性
	public bool IsEmpty => _members.Count == 0;
	public bool HasValue => _members.Count > 0;
	public IDictionary<string, object> Newer => _newer;
	public IDictionary<string, object> Older => _older;
	public bool HasNewer => _newer != null && _newer.Count > 0;
	public bool HasOlder => _older != null && _older.Count > 0;
	#endregion

	#region 重写方法
	public override string ToString() => $"Newer:{(_newer == null ? 0 : _newer.Count)}, Older:{(_older == null ? 0 : _older.Count)}";
	#endregion

	#region 嵌套结构
	private enum MemberKind
	{
		Newer,
		Older,
	}

	private readonly struct MemberKey : IEquatable<MemberKey>
	{
		#region 私有变量
		private readonly int _code;
		#endregion

		#region 构造函数
		public MemberKey(string name, MemberKind kind)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			_code = HashCode.Combine(name.ToUpperInvariant(), kind);
			this.Name = name;
			this.Kind = kind;
		}
		#endregion

		#region 公共字段
		public readonly string Name;
		public readonly MemberKind Kind;
		#endregion

		#region 重写方法
		public bool Equals(MemberKey other) => this.Kind == other.Kind && string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is MemberKey other && this.Equals(other);
		public override int GetHashCode() => _code;
		public override string ToString() => $"{this.Kind}:{this.Name}";

		public static bool operator ==(MemberKey left, MemberKey right) => left.Equals(right);
		public static bool operator !=(MemberKey left, MemberKey right) => !(left == right);
		#endregion
	}

	private sealed class MemberView(MemberKind kind, IDictionary<MemberKey, object> members) : IDictionary<string, object>
	{
		private readonly MemberKind _kind = kind;
		private readonly IDictionary<MemberKey, object> _members = members;

		public object this[string key]
		{
			get => _members[new MemberKey(key, _kind)];
			set => _members[new MemberKey(key, _kind)] = value;
		}

		public ICollection<string> Keys => _members.Where(member => member.Key.Kind == _kind).Select(member => member.Key.Name).ToArray();
		public ICollection<object> Values => _members.Where(member => member.Key.Kind == _kind).Select(member => member.Value).ToArray();
		public int Count => _members.Count(member => member.Key.Kind == _kind);
		bool ICollection<KeyValuePair<string, object>>.IsReadOnly => false;

		public void Add(string key, object value) => _members.Add(new MemberKey(key, _kind), value);
		void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item) => this.Add(item.Key, item.Value);

		public bool Contains(string key) => _members.ContainsKey(new MemberKey(key, _kind));
		public bool ContainsKey(string key) => _members.ContainsKey(new MemberKey(key, _kind));
		bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item) => _members.ContainsKey(new MemberKey(item.Key, _kind));
		void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
		{
			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			var keys = _members.Keys.Where(key => key.Kind == _kind).ToArray();

			for(int i = 0; i < Math.Min(array.Length, keys.Length); i++)
			{
				array[arrayIndex + i] = new KeyValuePair<string, object>(keys[i].Name, _members[keys[i]]);
			}
		}

		public bool Remove(string key) => _members.Remove(new MemberKey(key, _kind));
		bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item) => this.Remove(item.Key);
		public bool TryGetValue(string key, out object value) => _members.TryGetValue(new MemberKey(key, _kind), out value);

		public void Clear()
		{
			var keys = _members.Keys.ToArray();

			for(int i = 0; i < keys.Length; i++)
			{
				if(keys[i].Kind == _kind)
					_members.Remove(keys[i]);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _members.Where(member => member.Key.Kind == _kind).Select(member => new KeyValuePair<string, object>(member.Key.Name, member.Value)).GetEnumerator();
	}
	#endregion
}
