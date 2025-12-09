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

public enum DateTimePart
{
	Year,
	Month,
	Day,
	Hour,
	Minute,
	Second,
	Millisecond,
	Microsecond,
}

public static class DateTimeExtension
{
	public static DateTime Reset(this DateTime datetime, DateTimePart part, int value = 0) => part switch
	{
		DateTimePart.Year => new DateTime(value, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Month => new DateTime(datetime.Year, value, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Day => new DateTime(datetime.Year, datetime.Month, value, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Hour => new DateTime(datetime.Year, datetime.Month, datetime.Day, value, datetime.Minute, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Minute => new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, value, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Second => new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, value, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Millisecond => new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, value, datetime.Microsecond, datetime.Kind),
		DateTimePart.Microsecond => new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, value, datetime.Kind),
		_ => datetime,
	};

	public static TimeSpan GetElapsed(this DateTime start) => start.Kind == DateTimeKind.Utc ? DateTime.UtcNow - start : DateTime.Now - start;
	public static TimeSpan GetElapsed(this DateTimeOffset start) => DateTimeOffset.UtcNow - start;
	public static long GetElapsedMilliseconds(this DateTime start) => (long)GetElapsed(start).TotalMilliseconds;
	public static long GetElapsedMilliseconds(this DateTimeOffset start) => (long)GetElapsed(start).TotalMilliseconds;
}