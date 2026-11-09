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
using System.Collections.ObjectModel;

using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics.Configuration;

public class DiagnostorOptions
{
	#region 公共属性
	public Filtering Meters { get; set; }
	public Filtering Traces { get; set; }
	public Logging Logs { get; set; }
	#endregion

	#region 嵌套子类
	public sealed class Logging
	{
		public bool IncludeScopes { get; set; } = true;
		public bool IncludeFormattedMessage { get; set; } = true;
		public FilteringExporterCollection Exporters { get; set; }
	}

	public sealed class Filtering
	{
		[ConfigurationProperty("filter")]
		public ICollection<string> Filters { get; set; }
		public FilteringExporterCollection Exporters { get; set; }
	}

	public sealed class FilteringExporter
	{
		public string Name { get; set; }
		public bool Enabled { get; set; } = true;
		public string Driver { get; set; }
		public string Settings { get; set; }
	}

	public sealed class FilteringExporterCollection() : KeyedCollection<string, FilteringExporter>(StringComparer.OrdinalIgnoreCase)
	{
		protected override string GetKeyForItem(FilteringExporter exporter) => exporter.Name;
	}
	#endregion
}
