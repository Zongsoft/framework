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

[System.Reflection.DefaultMember(nameof(Exporters))]
[System.ComponentModel.DefaultProperty(nameof(Exporters))]
public class ExporterProvider
{
	public static readonly ExporterProvider Default = new();

	public ExporterProvider()
	{
		this.Exporters = new ExporterDescriptorCollection();
	}

	public ExporterDescriptorCollection Exporters { get; }

	public IExporterDescriptor GetExporter(string name) => name != null && this.Exporters.TryGetValue(name, out var descriptor) ? descriptor : null;
	public IEnumerable<IExporterDescriptor> GetExporters() => this.Exporters;
}
