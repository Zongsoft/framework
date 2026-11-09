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
		DateTimePart.Year => new DateTime(Math.Clamp(value, 1, 9999), datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Month => new DateTime(datetime.Year, Math.Clamp(value, 1, 12), datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Day => new DateTime(datetime.Year, datetime.Month, Math.Clamp(value, 1, DateTime.DaysInMonth(datetime.Year, datetime.Month)), datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Hour => new DateTime(datetime.Year, datetime.Month, datetime.Day, Math.Clamp(value, 0, 23), datetime.Minute, datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Minute => new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, Math.Clamp(value, 0, 59), datetime.Second, datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Second => new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, Math.Clamp(value, 0, 59), datetime.Millisecond, datetime.Microsecond, datetime.Kind),
		DateTimePart.Millisecond => new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, Math.Clamp(value, 0, 999), datetime.Microsecond, datetime.Kind),
		DateTimePart.Microsecond => new DateTime(datetime.Year, datetime.Month, datetime.Day, datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond, Math.Clamp(value, 0, 999), datetime.Kind),
		_ => datetime,
	};

	public static TimeSpan GetElapsed(this DateTime start) => start.Kind == DateTimeKind.Utc ? DateTime.UtcNow - start : DateTime.Now - start;
	public static TimeSpan GetElapsed(this DateTimeOffset start) => DateTimeOffset.UtcNow - start;
	public static long GetElapsedMilliseconds(this DateTime start) => (long)GetElapsed(start).TotalMilliseconds;
	public static long GetElapsedMilliseconds(this DateTimeOffset start) => (long)GetElapsed(start).TotalMilliseconds;
}