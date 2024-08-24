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
using System.Collections.Generic;

namespace Zongsoft.Security.Membership
{
	public readonly struct Permission : IEquatable<Permission>
	{
		#region 构造函数
		public Permission(string schema, params Action[] actions)
		{
			this.Target = schema ?? throw new ArgumentNullException(nameof(schema));
			this.Actions = actions ?? Array.Empty<Action>();
		}

		public Permission(string schema, IEnumerable<Action> actions)
		{
			this.Target = schema ?? throw new ArgumentNullException(nameof(schema));
			this.Actions = actions == null ? Array.Empty<Action>() : actions.ToArray();
		}
		#endregion

		#region 公共属性
		/// <summary>授权的目标标识。</summary>
		public string Target { get; }

		/// <summary>授权的操作集。</summary>
		public Action[] Actions { get; }
		#endregion

		#region 公共方法
		public bool HasAction(string action) =>
			!string.IsNullOrEmpty(action) && this.Actions.Any(token => string.Equals(token.Name, action, StringComparison.OrdinalIgnoreCase));
		#endregion

		#region 重写符号
		public static bool operator ==(Permission left, Permission right) => left.Equals(right);
		public static bool operator !=(Permission left, Permission right) => !(left == right);
		#endregion

		#region 重写方法
		public bool Equals(Permission other) => string.Equals(this.Target, other.Target, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is Permission other && this.Equals(other);
		public override int GetHashCode() => this.Target.ToUpperInvariant().GetHashCode();
		public override string ToString() => this.Actions == null || this.Actions.Length == 0 ? this.Target : $"{this.Target}({string.Join(",", this.Actions)})";
		#endregion

		#region 嵌套结构
		public readonly struct Action : IEquatable<Action>
		{
			#region 构造函数
			public Action(string name, string filter = null)
			{
				this.Name = name ?? throw new ArgumentNullException(nameof(name));
				this.Filter = filter;
			}
			#endregion

			#region 公共属性
			/// <summary>授权的操作名称。</summary>
			public string Name { get; }

			/// <summary>授权的过滤表达式。</summary>
			public string Filter { get; }
			#endregion

			#region 重写符号
			public static bool operator ==(Action left, Action right) => left.Equals(right);
			public static bool operator !=(Action left, Action right) => !(left == right);
			#endregion

			#region 重写方法
			public bool Equals(Action other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
			public override bool Equals(object obj) => obj is Action other && this.Equals(other);
			public override int GetHashCode() => this.Name.ToUpperInvariant().GetHashCode();
			public override string ToString() => string.IsNullOrEmpty(this.Filter) ? this.Name : $"{this.Name}:{this.Filter}";
			#endregion
		}
		#endregion
	}
}
