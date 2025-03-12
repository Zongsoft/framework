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

namespace Zongsoft.Security.Membership;

partial class Privilege
{
	public sealed class Permission : IEquatable<Permission>
	{
		#region 私有字段
		private readonly int _hashcode;
		#endregion

		#region 构造函数
		public Permission(string target, string action, string filter = null) : this(PermissionMode.Grant, target, action, filter) { }
		public Permission(PermissionMode mode, string target, string action, string filter = null)
		{
			if(string.IsNullOrEmpty(target))
				throw new ArgumentNullException(nameof(target));

			this.Mode = mode;
			this.Target = target;
			this.Action = string.IsNullOrEmpty(action) ? "*" : action;
			this.Filter = filter;
			_hashcode = HashCode.Combine(mode, this.Target.ToUpperInvariant(), this.Action.ToUpperInvariant());
		}
		#endregion

		#region 公共属性
		public PermissionMode Mode { get; }
		public string Target { get; }
		public string Action { get; }
		public string Filter { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(Permission other) => other is not null && this.Mode == other.Mode &&
			string.Equals(this.Target, other.Target, StringComparison.OrdinalIgnoreCase) &&
			string.Equals(this.Action, other.Action, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is Permission other && this.Equals(other);
		public override int GetHashCode() => _hashcode;
		public override string ToString() => this.Mode == PermissionMode.Grant ? $"{this.Target}#{this.Action}" : $"!{this.Target}#{this.Action}";
		#endregion

		#region 符号重写
		public static bool operator ==(Permission left, Permission right) => left is null ? right is null : left.Equals(right);
		public static bool operator !=(Permission left, Permission right) => !(left == right);
		#endregion
	}

	public sealed class PermissionCollection : ICollection<Permission>
	{
		private readonly Dictionary<PermissionKey, Permission> _permissions;

		public PermissionCollection(params IEnumerable<Permission> permissions)
		{
			if(permissions == null)
				_permissions = new Dictionary<PermissionKey, Permission>();
			else
				_permissions = new(permissions.Select(permission => new KeyValuePair<PermissionKey, Permission>((PermissionKey)permission, permission)));
		}

		public int Count => _permissions.Count;
		bool ICollection<Permission>.IsReadOnly => false;

		public Permission this[string target, string action]
		{
			get => _permissions.TryGetValue(new PermissionKey(target, action), out var permission) ? permission : null;
		}

		public void Set(Permission permission)
		{
			if(permission == null)
				return;

			_permissions[(PermissionKey)permission] = permission;
		}

		public bool Remove(string target, string action)
		{
			if(string.IsNullOrEmpty(target))
				return false;

			return _permissions.Remove(new PermissionKey(target, action));
		}

		public bool Contains(string target, string action)
		{
			if(string.IsNullOrEmpty(target))
				return false;

			return _permissions.ContainsKey(new PermissionKey(target, action)) || _permissions.ContainsKey(new PermissionKey(target, "*"));
		}

		public Permission Find(string target, string action)
		{
			if(string.IsNullOrEmpty(target))
				return null;

			if(_permissions.TryGetValue(new PermissionKey(target, action), out var permission))
				return permission;

			if(!string.IsNullOrEmpty(action) && action != "*" && _permissions.TryGetValue(new PermissionKey(target, "*"), out permission))
				return permission;

			return null;
		}

		public void Clear() => _permissions.Clear();
		void ICollection<Permission>.Add(Permission permission)
		{
			if(permission == null)
				throw new ArgumentNullException(nameof(permission));

			_permissions[(PermissionKey)permission] = permission;
		}

		bool ICollection<Permission>.Remove(Permission permission) => _permissions.Remove((PermissionKey)permission);
		bool ICollection<Permission>.Contains(Permission permission) => _permissions.ContainsKey((PermissionKey)permission);
		void ICollection<Permission>.CopyTo(Permission[] array, int arrayIndex) => _permissions.Values.CopyTo(array, arrayIndex);

		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<Permission> GetEnumerator() => _permissions.Values.GetEnumerator();

		private readonly struct PermissionKey : IEquatable<PermissionKey>
		{
			private readonly int _hashcode;

			public PermissionKey(string target, string action)
			{
				if(string.IsNullOrEmpty(target))
					throw new ArgumentNullException(nameof(target));

				this.Target = target;
				this.Action = string.IsNullOrEmpty(action) ? "*" : action;
				_hashcode = HashCode.Combine(this.Target.ToUpperInvariant(), this.Action.ToUpperInvariant());
			}

			public readonly string Target;
			public readonly string Action;

			public bool Equals(PermissionKey other) => string.Equals(this.Target, other.Target, StringComparison.OrdinalIgnoreCase) && string.Equals(this.Action, other.Action, StringComparison.OrdinalIgnoreCase);
			public override bool Equals(object obj) => obj is PermissionKey other && this.Equals(other);
			public override int GetHashCode() => _hashcode;
			public override string ToString() => $"{this.Target}#{this.Action}";

			public static implicit operator PermissionKey(Permission permission)
			{
				if(permission == null)
					throw new ArgumentNullException(nameof(permission));

				return new(permission.Target, permission.Action);
			}
		}
	}

	public enum PermissionMode
	{
		Grant,
		Deny,
	}
}
