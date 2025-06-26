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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.ClosedXml library.
 *
 * The Zongsoft.Externals.ClosedXml is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.ClosedXml is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.ClosedXml library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using ClosedXML;
using ClosedXML.Report;

using Zongsoft.Data.Archiving;

namespace Zongsoft.Externals.ClosedXml;

[Zongsoft.Services.Service(typeof(IDataTemplateRenderer))]
public class SpreadsheetRenderer : IDataTemplateRenderer, Services.IMatchable
{
	#region 公共属性
	public string Name => Spreadsheet.Format.Name;
	public DataArchiveFormat Format => Spreadsheet.Format;
	#endregion

	#region 模板渲染
	public ValueTask RenderAsync(Stream output, IDataTemplate template, object data, CancellationToken cancellation = default) => this.RenderAsync(output, template, data, null, null, cancellation);
	public ValueTask RenderAsync(Stream output, IDataTemplate template, object data, string format, CancellationToken cancellation = default) => this.RenderAsync(output, template, data, null, format, cancellation);
	public ValueTask RenderAsync(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default) => this.RenderAsync(output, template, data, parameters, null, cancellation);
	public ValueTask RenderAsync(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, string format, CancellationToken cancellation = default)
	{
		if(output == null)
			throw new ArgumentNullException(nameof(output));
		if(template == null)
			throw new ArgumentNullException(nameof(template));

		//确保模板类型是受支持的电子表格类型
		if(!Spreadsheet.Format.Equals(template.Format))
			throw new InvalidOperationException($"Unsupported template format: '{template.Format}'.");

		//确保指定的渲染格式是受支持的电子表格类型
		if(!string.IsNullOrEmpty(format) && Spreadsheet.Format.Equals(format))
			throw new InvalidOperationException($"Unsupported rendering format: '{format}'.");

		using var stream = template.Open();
		using var report = new XLTemplate(stream);

		//添加报表数据
		if(data != null)
		{
			//处理 IAsynEnumerable 异步可枚举接口类型
			var items = Collections.Enumerable.IsAsyncEnumerable(data, out var elementType) ?
				Collections.Enumerable.Enumerate(data, elementType) : data;

			report.AddVariable(items);
		}

		//添加报表参数
		if(parameters != null)
		{
			foreach(var parameter in parameters)
				report.AddVariable(parameter.Key, parameter.Value);
		}

		//生成报表内容
		report.Generate();
		//输出报表内容
		report.SaveAs(output);

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 服务匹配
	bool Services.IMatchable.Match(object parameter) => parameter switch
	{
		string format => Spreadsheet.Format.Equals(format),
		IDataTemplate template => Spreadsheet.Format.Equals(template.Format),
		_ => false,
	};
	#endregion
}