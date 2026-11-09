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

namespace Zongsoft.Common;

public static class TimeSpanUtility
{
	extension(TimeSpan)
	{
		public static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;
		public static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
	}

	public static TimeSpan Clamp(this TimeSpan value, TimeSpan minimum, TimeSpan maximum)
	{
		if(minimum > maximum)
			throw new ArgumentException(string.Format(Properties.Resources.Range_MinimumExceedsMaximum_Message, minimum, maximum));

		if(value < minimum)
			return minimum;
		if(value > maximum)
			return maximum;

		return value;
	}

	public static TimeSpan Clamp(this TimeSpan value, ReadOnlySpan<char> minimum, ReadOnlySpan<char> maximum)
	{
		if(minimum.IsEmpty)
			throw new ArgumentNullException(nameof(minimum));
		if(maximum.IsEmpty)
			throw new ArgumentNullException(nameof(maximum));

		if(!TryParse(minimum, out var min))
			throw new ArgumentException(string.Format(Properties.Resources.Conversion_ValueType_Message, minimum.ToString(), typeof(TimeSpan)));
		if(!TryParse(maximum, out var max))
			throw new ArgumentException(string.Format(Properties.Resources.Conversion_ValueType_Message, maximum.ToString(), typeof(TimeSpan)));

		return Clamp(value, min, max);
	}

	public static TimeSpan Parse(string text) => TryParse(text, out var result) ? result : throw new FormatException(string.Format(Properties.Resources.TimeSpan_InvalidValue_Message, text));
	public static TimeSpan Parse(ReadOnlySpan<char> text) => TryParse(text, out var result) ? result : throw new FormatException(string.Format(Properties.Resources.TimeSpan_InvalidValue_Message, text.ToString()));

	public static bool TryParse(string text, out TimeSpan value) => TryParse(string.IsNullOrEmpty(text) ? default : text.AsSpan(), out value);
	public static bool TryParse(ReadOnlySpan<char> text, out TimeSpan value)
	{
		if(text.IsEmpty || (int.TryParse(text, out var integer) && integer == 0))
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
