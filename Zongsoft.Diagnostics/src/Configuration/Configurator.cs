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
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics.Configuration;

[Diagnostor.ConfiguratorFactory<Factory>]
public sealed class Configurator(string path) : Diagnostor.Configurator()
{
	#region 成员字段
	private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));
	#endregion

	#region 重写方法
	public override void Configure(Diagnostor diagnostor)
	{
		if(diagnostor == null)
			return;

		var options = ApplicationContext.Current.Configuration.GetOption<DiagnostorOptions>(_path);
		if(options == null)
			return;

		if(options.Meters != null)
			diagnostor.Meters = new Diagnostor.Filtering(options.Meters.Filters, options.Meters.Exporters?.Select(exporter => new KeyValuePair<string, string>(exporter.Name, exporter.Settings)));

		if(options.Traces != null)
			diagnostor.Traces = new Diagnostor.Filtering(options.Traces.Filters, options.Traces.Exporters?.Select(exporter => new KeyValuePair<string, string>(exporter.Name, exporter.Settings)));
	}
	#endregion

	#region 嵌套子类
	private sealed class Factory : Diagnostor.ConfiguratorFactory
	{
		public override Diagnostor.Configurator Create(string argument) => new Configurator(argument);
	}
	#endregion
}
