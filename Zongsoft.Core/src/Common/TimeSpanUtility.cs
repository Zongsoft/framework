﻿/*
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

namespace Zongsoft.Common;

public static class TimeSpanUtility
{
	public static bool TryParse(string text, out TimeSpan value) => TryParse(string.IsNullOrEmpty(text) ? default : text.AsSpan(), out value);
	public static bool TryParse(ReadOnlySpan<char> text, out TimeSpan value)
	{
		if(text.IsEmpty || text.Length < 2)
		{
			value = TimeSpan.Zero;
			return false;
		}

		double number;

		switch(text[^1])
		{
			case 'd':
			case 'D':
				if(double.TryParse(text[0..^1], out number))
				{
					value = TimeSpan.FromDays(number);
					return true;
				}

				break;
			case 'h':
			case 'H':
				if(double.TryParse(text[0..^1], out number))
				{
					value = TimeSpan.FromHours(number);
					return true;
				}

				break;
			case 'm':
			case 'M':
				if(double.TryParse(text[0..^1], out number))
				{
					value = TimeSpan.FromMinutes(number);
					return true;
				}

				break;
			case 's':
			case 'S':
				if(double.TryParse(text[0..^1], out number))
				{
					value = TimeSpan.FromSeconds(number);
					return true;
				}

				break;
		}

		return TimeSpan.TryParse(text, out value);
	}
}