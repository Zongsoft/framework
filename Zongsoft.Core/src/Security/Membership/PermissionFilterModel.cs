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

namespace Zongsoft.Security.Membership
{
	public struct PermissionFilterModel : IEquatable<PermissionFilterModel>
	{
		#region 成员变量
		private string _target;
		private string _action;
		private string _filter;
		#endregion

		#region 构造函数
		public PermissionFilterModel(uint memberId, MemberType memberType, string target, string action, string filter)
		{
			if(string.IsNullOrEmpty(target))
				throw new ArgumentNullException(nameof(target));
			if(string.IsNullOrEmpty(action))
				throw new ArgumentNullException(nameof(action));
			if(string.IsNullOrEmpty(filter))
				throw new ArgumentNullException(nameof(filter));

			this.MemberId = memberId;
			this.MemberType = memberType;

			_target = target.Trim();
			_action = action.Trim();
			_filter = filter.Trim();
		}
		#endregion

		#region 公共属性
		public uint MemberId { get; set; }
		public MemberType MemberType { get; set; }

		public string Target
		{
			get => _target;
			set
			{
				if(string.IsNullOrEmpty(value))
					throw new ArgumentNullException();

				_target = value.Trim();
			}
		}

		public string Action
		{
			get => _action;
			set
			{
				if(string.IsNullOrEmpty(value))
					throw new ArgumentNullException();

				_action = value.Trim();
			}
		}

		public string Filter
		{
			get => _filter;
			set
			{
				if(string.IsNullOrEmpty(value))
					throw new ArgumentNullException();

				_filter = value.Trim();
			}
		}
		#endregion

		#region 重写方法
		public bool Equals(PermissionFilterModel other)
		{
			return this.MemberId == other.MemberId && this.MemberType == other.MemberType &&
			       string.Equals(_target, other._target, StringComparison.OrdinalIgnoreCase) &&
			       string.Equals(_action, other._action, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj) => obj is PermissionFilterModel other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.MemberId, this.MemberType, _target, _action);
		public override string ToString() => $"{this.MemberType}:{this.MemberId}-{this.Target}-{this.Action}" + Environment.NewLine + this.Filter;
		#endregion
	}
}
