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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;

namespace Zongsoft.Data.Metadata.Profiles
{
	internal static class MetadataUtility
	{
		public static void SetEntity(this IDataMetadataContainer container, MetadataEntity entity)
		{
			if(container == null || entity == null)
				return;

			if(container.Entities.TryAdd(entity))
			{
				if(entity.Container == null)
					entity.Container = container;

				return;
			}

			var existed = (MetadataEntity)container.Entities[entity.Name, entity.Namespace];

			foreach(var property in entity.Properties)
			{
				existed.Properties.Add(property);
			}

			if(!existed.HasKey && entity.HasKey)
			{
				existed.SetKey(entity.Key.Select(key => key.Name).ToArray());
				existed.Immutable = entity.Immutable;
			}

			if(string.IsNullOrEmpty(existed.Alias))
				existed.Alias = entity.Alias;
			if(string.IsNullOrEmpty(existed.BaseName))
				existed.BaseName = entity.BaseName;
			if(string.IsNullOrEmpty(existed.Driver))
				existed.Driver = entity.Driver;
		}

		public static void SetCommand(this IDataMetadataContainer container, MetadataCommand command)
		{
			if(container == null || command == null)
				return;

			if(container.Commands.TryAdd(command))
			{
				if(command.Container == null)
					command.Container = container;

				return;
			}

			throw new DataException($"The specified '{command}' data command mapping cannot be defined repeatedly.");
		}
	}
}
