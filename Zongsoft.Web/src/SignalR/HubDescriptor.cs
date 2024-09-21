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
 * Copyright (C) 2015-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;

using Microsoft.AspNetCore.Mvc;

namespace Zongsoft.Web.SignalR
{
	public sealed class HubDescriptor : IEquatable<HubDescriptor>, IEquatable<TypeInfo>
	{
		#region 常量定义
		private const string HubSuffix = @"Hub";
		#endregion

		#region 构造函数
		public HubDescriptor(TypeInfo type, string pattern = null)
		{
			this.Type = type ?? throw new ArgumentNullException(nameof(type));

			if(string.IsNullOrEmpty(pattern))
			{
				var attribute = type.GetCustomAttribute<RouteAttribute>(true);

				if(attribute != null)
					pattern = attribute.Template;
				else
				{
					if(type.Name.Length > HubSuffix.Length && type.Name.EndsWith(HubSuffix))
						pattern = type.Name[..^HubSuffix.Length];
					else
						pattern = type.Name;
				}
			}

			this.Pattern = pattern;
		}
		#endregion

		#region 公共属性
		public TypeInfo Type { get; }
		public string Pattern { get; }
		#endregion

		#region 重写方法
		public bool Equals(TypeInfo type) => type is not null && type == this.Type;
		public bool Equals(HubDescriptor other) => other is not null && other.Type == this.Type;
		public override bool Equals(object obj) => obj is HubDescriptor other && this.Equals(other);
		public override int GetHashCode() => this.Type.GetHashCode();
		public override string ToString() => string.IsNullOrEmpty(this.Pattern) ? this.Type.FullName : $"[{this.Pattern}]{this.Type.FullName}";
		#endregion
	}
}