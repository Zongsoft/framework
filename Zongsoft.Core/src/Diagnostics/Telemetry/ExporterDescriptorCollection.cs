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
using System.Collections.ObjectModel;

namespace Zongsoft.Diagnostics.Telemetry;

public class ExporterDescriptorCollection<TDescriptor> : KeyedCollection<string, TDescriptor>, IExporterDescriptorCollection<TDescriptor> where TDescriptor : IExporterDescriptor
{
	protected override string GetKeyForItem(TDescriptor descriptor) => descriptor.Name;
}

public class ExporterDescriptorCollection : ExporterDescriptorCollection<ExporterDescriptor> { }
