﻿/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Collections;
using System.Collections.Generic;

using ClosedXML;
using ClosedXML.Report;

using Zongsoft.IO;
using Zongsoft.Data;
using Zongsoft.Data.Templates;

namespace Zongsoft.Externals.ClosedXml
{
	[Zongsoft.Services.Service(typeof(IDataTemplateRenderer))]
	public class SpreadsheetRenderer : IDataTemplateRenderer
	{
		#region 公共属性
		public string Format => SpreadsheetTemplateProvider.Format;
		#endregion

		#region 模板渲染
		public ValueTask RenderAsync(Stream output, IDataTemplate template, object data, CancellationToken cancellation = default) => this.RenderAsync(output, template, data, null, cancellation);
		public ValueTask RenderAsync(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));
			if(template == null)
				throw new ArgumentNullException(nameof(template));

			using var stream = template.GetContent();
			using var report = new XLTemplate(stream);

			//添加报表数据
			if(data != null)
				report.AddVariable(data);

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
	}
}