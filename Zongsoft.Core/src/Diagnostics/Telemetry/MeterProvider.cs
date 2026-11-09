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
using System.Collections.Generic;

namespace Zongsoft.Diagnostics.Telemetry;

[System.Reflection.DefaultMember(nameof(Meters))]
[System.ComponentModel.DefaultProperty(nameof(Meters))]
public class MeterProvider : IMeterProvider
{
	public static readonly MeterProvider Default = new();

	public MeterProvider()
	{
		this.Meters = new MeterDescriptorCollection();
	}

	public MeterDescriptorCollection Meters { get; }

	public IMeterDescriptor GetMeter(string name) => name != null && this.Meters.TryGetValue(name, out var descriptor) ? descriptor : null;
	public IEnumerable<IMeterDescriptor> GetMeters() => this.Meters;
}
