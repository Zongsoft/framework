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
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata;

public static class DataCommandExtension
{
	public static DataCommand Add(this DataCommandCollection commands, string qualifiedName, DataCommandType type = DataCommandType.Text) => Add(commands, qualifiedName, DataCommandMutability.None, type);
	public static DataCommand Add(this DataCommandCollection commands, string qualifiedName, DataCommandMutability mutability, DataCommandType type = DataCommandType.Text)
	{
		ArgumentNullException.ThrowIfNull(commands);
		(var name, var @namespace) = DataUtility.ParseQualifiedName(qualifiedName);
		var command = new DataCommand(@namespace, name, mutability)
		{
			Type = type,
		};
		commands.Add(command);
		return command;
	}

	public static DataCommand Add(this DataCommandCollection commands, string qualifiedName, string alias, DataCommandType type = DataCommandType.Procedure) => Add(commands, qualifiedName, alias, DataCommandMutability.None, type);
	public static DataCommand Add(this DataCommandCollection commands, string qualifiedName, string alias, DataCommandMutability mutability, DataCommandType type = DataCommandType.Procedure)
	{
		ArgumentNullException.ThrowIfNull(commands);
		(var name, var @namespace) = DataUtility.ParseQualifiedName(qualifiedName);
		var command = new DataCommand(@namespace, name, mutability, alias)
		{
			Type = type,
		};
		commands.Add(command);
		return command;
	}

	public static IDataCommand Script(this IDataCommand command, string driver, string script, params IEnumerable<IDataCommandParameter> parameters)
	{
		ArgumentNullException.ThrowIfNull(command);
		command.Scriptor?.SetScript(driver, script);

		if(parameters != null)
		{
			foreach(var parameter in parameters)
				command.Parameters.Add(parameter);
		}

		return command;
	}

	public static IDataCommand Parameter(this IDataCommand command, string name, DataType type, ParameterDirection direction = ParameterDirection.Input) => Parameter(command, name, type, 0, direction);
	public static IDataCommand Parameter(this IDataCommand command, string name, DataType type, int length, ParameterDirection direction = ParameterDirection.Input)
	{
		ArgumentNullException.ThrowIfNull(command);
		command.Parameters.Add(new DataCommandParameter(command, name, type, length, direction));
		return command;
	}
}
