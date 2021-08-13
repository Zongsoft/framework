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
 * This file is part of Zongsoft.Reporting library.
 *
 * The Zongsoft.Reporting is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Reporting is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Reporting library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Reporting.Resources
{
	public class Resource : IResource, IEquatable<Resource>
	{
		#region 构造函数
		public Resource() { }
		public Resource(string name, string type, string title = null, string extra = null, string description = null)
		{
			this.Name = name;
			this.Type = type;
			this.Title = title;
			this.Extra = extra;
			this.Description = description;
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public string Type { get; }
		public string Title { get; set; }
		public string Extra { get; set; }
		public string Description { get; set; }

		public IDictionary<string, ResourceEntry> Dictionary { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(Resource other) =>
			string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &
			string.Equals(this.Type, other.Type, StringComparison.OrdinalIgnoreCase);

		public override bool Equals(object obj) => obj is Resource info && this.Equals(info);
		public override int GetHashCode() => HashCode.Combine(this.Name.ToUpperInvariant(), this.Type.ToUpperInvariant());
		public override string ToString() => $"{this.Name}@{this.Type}";

		public static bool operator ==(Resource left, Resource right) => left.Equals(right);
		public static bool operator !=(Resource left, Resource right) => !(left == right);
		#endregion
	}
}
