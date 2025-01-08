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
 * This file is part of Zongsoft.Diagnostics library.
 *
 * The Zongsoft.Diagnostics is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Diagnostics is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Diagnostics library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Globalization;
using System.ComponentModel;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics.Configuration;

[TypeConverter(typeof(Converter))]
public class DiagnostorConfigurator : IDiagnostorConfigurator
{
	private const string NAME = "Configuration";

	public DiagnostorConfigurator(string path)
	{
		if(string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		this.Path = path.Trim();
	}

	public string Name => NAME;
	public string Path { get; set; }

	public void Configure(IDiagnostor diagnostor, object argument = null)
	{
		if(diagnostor == null)
			return;

		var options = ApplicationContext.Current.Configuration.GetOption<DiagnostorOptions>(this.Path);
		if(options == null)
			return;

		if(options.Meters != null)
			diagnostor.Meters = new DiagnostorFiltering(options.Meters.GetFilters(), options.Meters.Exporters.Select(exporter => new System.Collections.Generic.KeyValuePair<string, string>(exporter.Driver.Name, exporter.Value)));

		if(options.Traces != null)
			diagnostor.Traces = new DiagnostorFiltering(options.Traces.GetFilters(), options.Traces.Exporters.Select(exporter => new System.Collections.Generic.KeyValuePair<string, string>(exporter.Driver.Name, exporter.Value)));
	}

	private sealed class Converter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if(value is string text)
				return new DiagnostorConfigurator(text);

			return null;
		}
	}
}
