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
using System.Reflection;
using System.Diagnostics.Metrics;
using System.Collections.Generic;

namespace Zongsoft.Diagnostics.Telemetry;

#if NET8_0_OR_GREATER
public static partial class MeterExtension
{
	private static readonly MethodInfo _UpDownCounterMethod_ = typeof(Meter).GetMethod(
		nameof(Meter.CreateUpDownCounter),
		1,
		BindingFlags.Public | BindingFlags.Instance,
		null,
		[typeof(string), typeof(string), typeof(string), typeof(IEnumerable<KeyValuePair<string, object>>)],
		null);

	public static Instrument CreateUpDownCounter(this Meter meter, string name, Type type) => CreateUpDownCounter(meter, name, type, null, null, null);
	public static Instrument CreateUpDownCounter(this Meter meter, string name, Type type, string unit, string description = null) => CreateUpDownCounter(meter, name, type, unit, description, null);
	public static Instrument CreateUpDownCounter(this Meter meter, string name, Type type, string unit, IEnumerable<KeyValuePair<string, object>> tags = null) => CreateUpDownCounter(meter, name, type, unit, null, tags);
	public static Instrument CreateUpDownCounter(this Meter meter, string name, Type type, string unit, string description, IEnumerable<KeyValuePair<string, object>> tags = null)
	{
		if(meter == null)
			throw new ArgumentNullException(nameof(meter));
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(type == null)
			throw new ArgumentNullException(nameof(type));
		if(!type.IsValueType)
			throw new ArgumentException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.Type_ValueTypeRequired_Message, nameof(type)), nameof(type));

		return (Instrument)_UpDownCounterMethod_.MakeGenericMethod(type).Invoke(meter, [name, unit, description, tags]);
	}
}
#endif