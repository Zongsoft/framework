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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Data;

namespace Zongsoft.Data.Metadata;

public class DataCommandParameter : IDataCommandParameter
{
	#region 构造函数
	public DataCommandParameter(IDataCommand command, string name, DataType type, ParameterDirection direction = ParameterDirection.Input)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Command = command;
		this.Name = name.Trim();
		this.Type = type;
		this.Direction = direction;
	}
	#endregion

	#region 公共属性
	/// <summary>获取参数所属的命令对象。</summary>
	public IDataCommand Command { get; }

	/// <summary>获取命令参数的名称。</summary>
	public string Name { get; }

	/// <summary>获取或设置命令参数的别名。</summary>
	public string Alias { get; set; }

	/// <summary>获取命令参数的类型。</summary>
	public DataType Type { get; set; }

	/// <summary>获取或设置命令参数的最大长度。</summary>
	public int Length { get; set; }

	/// <summary>获取或设置命令参数的值。</summary>
	public object Value { get; set; }

	/// <summary>获取或设置命令参数的传递方向。</summary>
	public ParameterDirection Direction { get; set; }
	#endregion
}
