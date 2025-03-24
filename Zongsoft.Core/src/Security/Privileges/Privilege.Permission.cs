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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

partial class Privilege
{
	public sealed class Permission : IEquatable<Permission>, IIdentifiable<string>, IIdentifiable
	{
		#region 私有字段
		private readonly int _hashcode;
		#endregion

		#region 构造函数
		public Permission(string target, string action)
		{
			if(string.IsNullOrEmpty(target))
				throw new ArgumentNullException(nameof(target));

			this.Target = target;
			this.Action = string.IsNullOrEmpty(action) ? "*" : action;
			_hashcode = HashCode.Combine(this.Target.ToUpperInvariant(), this.Action.ToUpperInvariant());
		}
		#endregion

		#region 公共属性
		public string Target { get; }
		public string Action { get; }
		#endregion

		#region 重写方法
		public bool Equals(Permission other) => other is not null &&
			string.Equals(this.Target, other.Target, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(this.Action, other.Action, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is Permission other && this.Equals(other);
		public override int GetHashCode() => _hashcode;
		public override string ToString() => $"{this.Target}#{this.Action}";
		#endregion

		#region 显式实现
		Identifier IIdentifiable.Identifier { get => (Identifier)this; set => throw new NotSupportedException(); }
		Identifier<string> IIdentifiable<string>.Identifier { get => (Identifier<string>)this; set => throw new NotSupportedException(); }
		#endregion

		#region 隐式转换
		public static implicit operator Identifier(Permission permission) => new(typeof(Privilege), permission.ToString());
		public static implicit operator Identifier<string>(Permission permission) => new(typeof(Privilege), permission.ToString());
		#endregion

		#region 符号重写
		public static bool operator ==(Permission left, Permission right) => left is null ? right is null : left.Equals(right);
		public static bool operator !=(Permission left, Permission right) => !(left == right);
		#endregion
	}

	public sealed class PermissionCollection : ICollection<Permission>
	{
		#region 成员字段
		private int? _count;
		private readonly Dictionary<string, HashSet<string>> _permissions;
		#endregion

		#region 构造函数
		public PermissionCollection(params IEnumerable<Permission> permissions)
		{
			if(permissions == null)
				_permissions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			else
				_permissions = new(permissions
					.GroupBy(permission => permission.Target)
					.Select(group =>
						new KeyValuePair<string, HashSet<string>>(
							group.Key,
							new HashSet<string>(group.Select(g => g.Action), StringComparer.OrdinalIgnoreCase)
						)
					));
		}
		#endregion

		#region 公共属性
		public int Count => _count ??= _permissions.Values.Sum(actions => actions.Count);
		public IReadOnlyCollection<string> this[string target]
		{
			get => _permissions.TryGetValue(target, out var actions) ? actions : default;
		}
		#endregion

		#region 公共方法
		public void Add(Permission permission)
		{
			if(permission == null)
				return;

			if(_permissions.TryGetValue(permission.Target, out var actions) && actions != null)
				actions.Add(permission.Action);
			else
				_permissions[permission.Target] = new HashSet<string>([permission.Action], StringComparer.OrdinalIgnoreCase);

			//重置计数器
			_count = null;
		}

		public void Clear() { _permissions.Clear(); _count = 0; }

		public bool Remove(string target)
		{
			if(string.IsNullOrEmpty(target))
				return false;

			if(_permissions.Remove(target))
			{
				_count = null;
				return true;
			}

			return false;
		}

		public bool Remove(string target, string action)
		{
			if(string.IsNullOrEmpty(action))
				action = "*";

			if(_permissions.TryGetValue(target, out var actions) && actions != null && actions.Remove(action))
			{
				_count = null;
				return true;
			}

			return false;
		}

		public bool Contains(string target)
		{
			if(string.IsNullOrEmpty(target))
				return false;

			if(_permissions.TryGetValue(target, out var actions) && actions != null && actions.Count > 0)
			{
				_count = null;
				return true;
			}

			return false;
		}

		public bool Contains(string target, string action)
		{
			if(string.IsNullOrEmpty(target))
				return false;

			if(string.IsNullOrEmpty(action))
				action = "*";

			return _permissions.TryGetValue(target, out var actions) && actions != null && (actions.Contains(action) || actions.Contains("*"));
		}
		#endregion

		#region 显式实现
		bool ICollection<Permission>.IsReadOnly => false;
		bool ICollection<Permission>.Remove(Permission permission) => this.Remove(permission.Target, permission.Action);
		bool ICollection<Permission>.Contains(Permission permission) => this.Contains(permission.Target, permission.Action);
		void ICollection<Permission>.CopyTo(Permission[] array, int arrayIndex)
		{
			if(array == null)
				throw new ArgumentNullException(nameof(array));

			if(arrayIndex < 0 || arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex));

			foreach(var permission in _permissions)
			{
				if(permission.Value == null || permission.Value.Count == 0)
					continue;

				foreach(var action in permission.Value)
				{
					array[arrayIndex++] = new Permission(permission.Key, action);
					if(arrayIndex >= array.Length)
						return;
				}
			}
		}
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Permission> GetEnumerator()
		{
			foreach(var permission in _permissions)
			{
				if(permission.Value == null || permission.Value.Count == 0)
					continue;

				foreach(var action in permission.Value)
					yield return new Permission(permission.Key, action);
			}
		}
		#endregion
	}
}
