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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Zongsoft.Diagnostics.Telemetry;

public static partial class MeterExtension
{
	private static readonly MethodInfo _HistogramMethod_ = typeof(Meter).GetMethod(
		nameof(Meter.CreateHistogram),
		1,
		BindingFlags.Public | BindingFlags.Instance,
		null,
		[typeof(string), typeof(string), typeof(string)],
		null);

	public static Instrument CreateHistogram(this Meter meter, string name, Type type, string unit = null, string description = null)
	{
		if(meter == null)
			throw new ArgumentNullException(nameof(meter));
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(type == null)
			throw new ArgumentNullException(nameof(type));
		if(!type.IsValueType)
			throw new ArgumentException("The type must be a value type.", nameof(type));

		return (Instrument)_HistogramMethod_.MakeGenericMethod(type).Invoke(meter, [name, unit, description]);
	}

#if NET8_0_OR_GREATER
	private static readonly MethodInfo _HistogramTagsMethod_ = typeof(Meter).GetMethod(
		nameof(Meter.CreateHistogram),
		1,
		BindingFlags.Public | BindingFlags.Instance,
		null,
		[typeof(string), typeof(string), typeof(string), typeof(IEnumerable<KeyValuePair<string, object>>)],
		null);

	public static Instrument CreateHistogram(this Meter meter, string name, Type type, string unit, IEnumerable<KeyValuePair<string, object>> tags = null) => CreateHistogram(meter, name, type, unit, null, tags);
	public static Instrument CreateHistogram(this Meter meter, string name, Type type, string unit, string description, IEnumerable<KeyValuePair<string, object>> tags = null)
	{
		if(meter == null)
			throw new ArgumentNullException(nameof(meter));
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(type == null)
			throw new ArgumentNullException(nameof(type));
		if(!type.IsValueType)
			throw new ArgumentException("The type must be a value type.", nameof(type));

		return (Instrument)_HistogramTagsMethod_.MakeGenericMethod(type).Invoke(meter, [name, unit, description, tags]);
	}
#endif
}
