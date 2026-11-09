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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
		(var name, var @namespace) = DataUtility.Qualify(qualifiedName);
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
		(var name, var @namespace) = DataUtility.Qualify(qualifiedName);
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

	public static IDataCommand Parameter(this IDataCommand command, string name, DataType type, ParameterDirection direction = ParameterDirection.Input) => Parameter(command, name, type, 0, null, direction);
	public static IDataCommand Parameter(this IDataCommand command, string name, DataType type, object value, ParameterDirection direction = ParameterDirection.Input) => Parameter(command, name, type, 0, value, direction);
	public static IDataCommand Parameter(this IDataCommand command, string name, DataType type, int length, ParameterDirection direction = ParameterDirection.Input) => Parameter(command, name, type, length, null, direction);
	public static IDataCommand Parameter(this IDataCommand command, string name, DataType type, int length, object value, ParameterDirection direction = ParameterDirection.Input)
	{
		ArgumentNullException.ThrowIfNull(command);
		command.Parameters.Add(new DataCommandParameter(name, type, length, value, direction));
		return command;
	}
}
