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
