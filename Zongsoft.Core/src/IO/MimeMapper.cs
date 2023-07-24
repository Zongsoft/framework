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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.IO
{
	public static class Mime
	{
		public static readonly ICollection<IMimeMapper> Mappers = new List<IMimeMapper>();

		public static string GetMimeType(string path)
		{
			if(string.IsNullOrEmpty(path))
				return null;

			if(Mappers.Count == 0)
			{
				var mappers = (List<IMimeMapper>)Mappers;

				lock(mappers)
				{
					if(mappers.Count == 0)
					{
						mappers.AddRange(ApplicationContext.Current.Services.ResolveAll<IMimeMapper>());
						foreach(var module in ApplicationContext.Current.Modules)
							mappers.AddRange(module.Services.ResolveAll<IMimeMapper>());
					}
				}
			}

			foreach(var mapper in Mappers)
			{
				var result = mapper.GetMimeType(path);

				if(!string.IsNullOrEmpty(result))
					return result;
			}

			return null;
		}
	}
}