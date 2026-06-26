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
 * Copyright (C) 2025-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Learning library.
 *
 * The Zongsoft.Learning is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Learning is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Learning library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.ObjectModel;

namespace Zongsoft.Learning;

public class DatasetField
{
	#region 构造函数
	public DatasetField(string name, Type type, int ordinal = -1, DatasetFieldRole role = DatasetFieldRole.None)
	{
		this.Name = name;
		this.Type = type;
		this.Role = role;
		this.Ordinal = ordinal;
	}

	public DatasetField(string name, Type type, string alias, int ordinal = -1, DatasetFieldRole role = DatasetFieldRole.None)
	{
		this.Name = name;
		this.Type = type;
		this.Role = role;
		this.Alias = alias;
		this.Ordinal = ordinal;
	}
	#endregion

	#region 公共属性
	public string Name { get; set; }
	public Type Type { get; set; }
	public int Ordinal { get; set; }
	public string Alias { get; set; }
	public DatasetFieldRole Role { get; set; }
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(this.Ordinal < 0)
			return string.IsNullOrEmpty(this.Alias) ?
				$"{this.Name}:{Common.TypeAlias.GetAlias(this.Type, true)}({this.Role})" :
				$"{this.Name}:{Common.TypeAlias.GetAlias(this.Type, true)}@{this.Alias}({this.Role})";
		else
			return string.IsNullOrEmpty(this.Alias) ?
				$"[{this.Ordinal}]{this.Name}:{Common.TypeAlias.GetAlias(this.Type, true)}({this.Role})" :
				$"[{this.Ordinal}]{this.Name}:{Common.TypeAlias.GetAlias(this.Type, true)}@{this.Alias}({this.Role})";
	}
	#endregion
}

public enum DatasetFieldRole
{
	None,
	Key,
	Label,
	Feature,
}

public class DatasetFieldCollection() : KeyedCollection<string, DatasetField>(StringComparer.OrdinalIgnoreCase)
{
	protected override string GetKeyForItem(DatasetField field) => field.Name;
}
