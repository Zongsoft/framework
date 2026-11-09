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

public class MeterDescriptor : IMeterDescriptor
{
	public MeterDescriptor(string name, Type type, string version, string label = null, string description = null) : this(name, type, version, null, label, description) { }
	public MeterDescriptor(string name, Type type, string version, IEnumerable<KeyValuePair<string, object>> tags, string label = null, string description = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Type = type ?? throw new ArgumentNullException(nameof(type));
		this.Version = version;
		this.Tags = tags;
		this.Label = label;
		this.Description = description;
	}

	public string Name { get; }
	public Type Type { get; }
	public string Label { get; set; }
	public string Version { get; set; }
	public string Description { get; set; }
	public IEnumerable<KeyValuePair<string, object>> Tags { get; set; }
}
