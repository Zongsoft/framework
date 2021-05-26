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
using System.IO;
using System.Threading.Tasks;

using Zongsoft.Services;

namespace Zongsoft.Security
{
	public static class SecretIdentityUtility
	{
		public static string GetValue(this Common.InstanceData data)
		{
			return data.GetValue(source =>
			{
				if(source is string text)
					return text;

				if(source is byte[] bytes)
					return System.Text.Encoding.UTF8.GetString(bytes);

				if(source is Stream stream)
				{
					using(var reader = new StreamReader(stream, System.Text.Encoding.UTF8))
					{
						return reader.ReadToEnd();
					}
				}

				throw new InvalidOperationException($"The identity verification data type '{source.GetType().FullName}' is not supported.");
			});
		}
	}
}
