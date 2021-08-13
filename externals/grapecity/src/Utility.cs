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
 * This file is part of Zongsoft.Externals.Grapecity library.
 *
 * The Zongsoft.Externals.Grapecity is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Grapecity is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Grapecity library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace Zongsoft.Externals.Grapecity
{
	public static class Utility
	{
		public static byte[] ReadAll(Stream stream)
		{
			if(stream == null)
				return null;

			byte[] buffer;

			if(stream.CanSeek)
			{
				buffer = new byte[stream.Length];
				stream.Read(buffer, 0, buffer.Length);
				return buffer;
			}

			buffer = new byte[1024];
			var data = new List<byte>(1024);
			int bytesRead;

			while((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				if(bytesRead == buffer.Length)
					data.AddRange(buffer);
				else
				{
					var span = new ReadOnlySpan<byte>(buffer, 0, bytesRead);
					data.AddRange(span.ToArray());
				}
			}

			return data.ToArray();
		}
	}
}
