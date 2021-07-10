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

namespace Zongsoft.Common
{
	public static class TimeSpanExtension
	{
		public static bool TryParse(string text, out TimeSpan value)
		{
			if(string.IsNullOrEmpty(text) || text.Length < 2)
			{
				value = TimeSpan.Zero;
				return false;
			}

			var span = text.AsSpan();
			int number;

			switch(text[^1])
			{
				case 'd':
				case 'D':
					if(int.TryParse(span[0..^1], out number))
					{
						value = TimeSpan.FromDays(number);
						return true;
					}

					break;
				case 'h':
				case 'H':
					if(int.TryParse(span[0..^1], out number))
					{
						value = TimeSpan.FromHours(number);
						return true;
					}

					break;
				case 'm':
					if(int.TryParse(span[0..^1], out number))
					{
						value = TimeSpan.FromMinutes(number);
						return true;
					}

					break;
				case 's':
					if(int.TryParse(span[0..^1], out number))
					{
						value = TimeSpan.FromSeconds(number);
						return true;
					}

					break;
			}

			return TimeSpan.TryParse(text, out value);
		}
	}
}
