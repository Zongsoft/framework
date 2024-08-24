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

namespace Zongsoft.Data.Templates
{
	/// <summary>
	/// 表示数据文件格式的类。
	/// </summary>
	public sealed class DataArchiveFormat : IEquatable<DataArchiveFormat>, IEquatable<string>
	{
		#region 构造函数
		public DataArchiveFormat(string name, string type, string extension = null)
		{
			if(string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
			if(string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));

			this.Name = name;
			this.Type = type;

			if(!string.IsNullOrWhiteSpace(extension))
				this.Extension = extension.StartsWith('.') ? extension.Trim() : '.' + extension.Trim();
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据文件的格式名称。</summary>
		public string Name { get; }

		/// <summary>获取数据文件的格式类型。</summary>
		public string Type { get; }

		/// <summary>获取数据文件的扩展名称。</summary>
		/// <remarks>如果该属性不为空，则扩展名始终以<c>.</c>打头。</remarks>
		public string Extension { get; }
		#endregion

		#region 重写方法
		public bool Equals(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
		public bool Equals(DataArchiveFormat other) => string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);
		public override bool Equals(object obj) => obj is DataArchiveFormat other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.Name.ToUpperInvariant());
		public override string ToString() => $"{this.Name}({this.Type})";
		#endregion

		#region 符号重写
		public static bool operator ==(DataArchiveFormat left, DataArchiveFormat right) => left.Equals(right);
		public static bool operator !=(DataArchiveFormat left, DataArchiveFormat right) => !(left == right);
		#endregion
	}
}