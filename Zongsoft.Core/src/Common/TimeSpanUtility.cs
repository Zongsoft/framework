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
	public static TimeSpan Clamp(this TimeSpan value, TimeSpan minimum, TimeSpan maximum)
	{
		if(minimum > maximum)
			throw new ArgumentException($"The minimum value '{minimum}' cannot be granter than maximum value '{maximum}'.");

		if(value < minimum) return minimum;
		if(value > maximum) return maximum;

		return value;
	}

	public static TimeSpan Clamp(this TimeSpan value, ReadOnlySpan<char> minimum, ReadOnlySpan<char> maximum)
	{
		if(minimum.IsEmpty)
			throw new ArgumentNullException(nameof(minimum));
		if(maximum.IsEmpty)
			throw new ArgumentNullException(nameof(maximum));

		if(!TryParse(minimum, out var min))
			throw new ArgumentException($"Unable to convert '{minimum}' to TimeSpan type.");
		if(!TryParse(maximum, out var max))
			throw new ArgumentException($"Unable to convert '{maximum}' to TimeSpan type.");

		return Clamp(value, min, max);
	}

	public static bool TryParse(string text, out TimeSpan value) => TryParse(string.IsNullOrEmpty(text) ? default : text.AsSpan(), out value);
	public static bool TryParse(ReadOnlySpan<char> text, out TimeSpan value)
	{
		if(text.IsEmpty || text == "0")
		{
			value = TimeSpan.Zero;
			return true;
		}

		if(text.Length < 2)
		{
			value = default;
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

				value = default;
				return false;
			case 'h':
			case 'H':
				if(double.TryParse(text[0..^1], out number))
				{
					value = TimeSpan.FromHours(number);
					return true;
				}

				value = default;
				return false;
			case 'm':
			case 'M':
				if(double.TryParse(text[0..^1], out number))
				{
					value = TimeSpan.FromMinutes(number);
					return true;
				}

				value = default;
				return false;
			case 's':
			case 'S':
				if(text[^2] == 'm' || text[^2] == 'M')
				{
					if(double.TryParse(text[0..^2], out number))
					{
						value = TimeSpan.FromMilliseconds(number);
						return true;
					}

					value = default;
					return false;
				}

				if(double.TryParse(text[0..^1], out number))
				{
					value = TimeSpan.FromSeconds(number);
					return true;
				}

				value = default;
				return false;
		}

		return TimeSpan.TryParse(text, out value);
	}
}