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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Concurrent;

namespace Zongsoft.Security.Web.Controllers;

internal static class Utility
{
	private static readonly ConcurrentDictionary<Type, Type> _types = new();

	public static Type GetModelType(object service, Type modelType, Type servicePrototype)
	{
		if(service == null)
			return null;

		return _types.GetOrAdd(service.GetType(), type =>
		{
			var contracts = type.GetInterfaces();

			for(int i = 0; i < contracts.Length; i++)
			{
				var contract = contracts[i];

				if(IsModel(contract, modelType, servicePrototype))
					return contract.GenericTypeArguments[0];
			}

			return null;
		});

		static bool IsModel(Type contract, Type modelType, Type servicePrototype) =>
			contract.IsGenericType &&
			contract.GetGenericTypeDefinition() == servicePrototype &&
			contract.GenericTypeArguments[0] != modelType &&
			modelType.IsAssignableFrom(contract.GenericTypeArguments[0]);
	}

	public static (string identity, string @namespace) Identify(string identifier)
	{
		if(string.IsNullOrEmpty(identifier))
			return default;

		var index = identifier.IndexOf(':');
		if(index < 0)
			return (identifier, null);

		return (identifier[(index + 1)..], identifier[..index]);
	}
}
