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
 * This file is part of Zongsoft.Web.OpenApi library.
 *
 * The Zongsoft.Web.OpenApi is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web.OpenApi is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web.OpenApi library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Web.OpenApi;

public sealed class DocumentFormat : IEquatable<DocumentFormat>
{
	#region 静态字段
	public static readonly DocumentFormat Json = new(nameof(Json), "application/json;charset=utf-8");
	public static readonly DocumentFormat Yaml = new(nameof(Yaml), "text/plain+yaml;charset=utf-8");
	#endregion

	#region 私有构造
	private DocumentFormat(string name, string type)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));
		if(string.IsNullOrEmpty(type))
			throw new ArgumentNullException(nameof(type));

		this.Name = name.ToLowerInvariant();
		this.Type = type;
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Type { get; }
	#endregion

	#region 静态方法
	public static bool TryParse(ReadOnlySpan<char> text, out DocumentFormat format)
	{
		if(text.Equals(Json.Name, StringComparison.OrdinalIgnoreCase))
		{
			format = Json;
			return true;
		}

		if(text.Equals(Yaml.Name, StringComparison.OrdinalIgnoreCase) ||
		   text.Equals("yml", StringComparison.OrdinalIgnoreCase))
		{
			format = Yaml;
			return true;
		}

		format = null;
		return false;
	}
	#endregion

	#region 符号重写
	public static bool operator ==(DocumentFormat left, DocumentFormat right) => left is null ? right is null : left.Equals(right);
	public static bool operator !=(DocumentFormat left, DocumentFormat right) => !(left == right);
	#endregion

	#region 重写方法
	public bool Equals(DocumentFormat other) => other is not null && string.Equals(this.Name, other.Name);
	public override bool Equals(object obj) => this.Equals(obj as DocumentFormat);
	public override int GetHashCode() => this.Name.GetHashCode();
	public override string ToString() => this.Name;
	#endregion
}
