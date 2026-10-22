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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Data.Archiving;

namespace Zongsoft.Data;

partial class DataServiceBase<TModel> : IDataExportable
{
	#region 公共属性
	public virtual bool CanExport => true;
	#endregion

	#region 数据文件生成
	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, object data, CancellationToken cancellation) => this.ExportAsync(output, data, default(string), default(DataExportOptions), cancellation);
	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, object data, string format, CancellationToken cancellation) => this.ExportAsync(output, data, format, default(DataExportOptions), cancellation);
	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, object data, string format = null, DataExportOptions options = null, CancellationToken cancellation = default) => this.ExportAsync(output, data, null, format, options, cancellation);

	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, object data, string[] members, CancellationToken cancellation) => this.ExportAsync(output, data, members, default(string), default(DataExportOptions), cancellation);
	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, object data, string[] members, string format, CancellationToken cancellation) => this.ExportAsync(output, data, members, format, default(DataExportOptions), cancellation);
	public async ValueTask<DataArchiveFormat> ExportAsync(Stream output, object data, string[] members, string format = null, DataExportOptions options = null, CancellationToken cancellation = default)
	{
		if(output == null)
			throw new ArgumentNullException(nameof(output));

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.Export(), options, cancellation);

		//执行数据文件生成导出
		return await this.OnExportAsync(output, data, members, format, options, cancellation);
	}

	protected virtual async ValueTask<DataArchiveFormat> OnExportAsync(Stream output, object data, string[] members, string format, DataExportOptions options, CancellationToken cancellation)
	{
		var generator = this.ServiceProvider.Find<IDataArchiveGenerator>(format) ?? throw OperationException.Unfound();
		await generator.GenerateAsync(
			output,
			this.Descriptor.Model,
			data,
			members == null || members.Length == 0 ? null : new DataArchiveGeneratorOptions(members),
			cancellation);

		return generator.Format;
	}
	#endregion

	#region 数据模板渲染
	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, string template, object argument, CancellationToken cancellation) => this.ExportAsync(output, template, argument, default(string), default(DataExportOptions), cancellation);
	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, string template, object argument, string format, CancellationToken cancellation) => this.ExportAsync(output, template, argument, format, default(DataExportOptions), cancellation);
	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, string template, object argument, string format = null, DataExportOptions options = null, CancellationToken cancellation = default) => this.ExportAsync(output, template, argument, null, format, options, cancellation);

	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, string template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation) => this.ExportAsync(output, template, argument, parameters, default(string), default(DataExportOptions), cancellation);
	public ValueTask<DataArchiveFormat> ExportAsync(Stream output, string template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, string format, CancellationToken cancellation) => this.ExportAsync(output, template, argument, parameters, format, default(DataExportOptions), cancellation);
	public async ValueTask<DataArchiveFormat> ExportAsync(Stream output, string template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, string format = null, DataExportOptions options = null, CancellationToken cancellation = default)
	{
		if(output == null)
			throw new ArgumentNullException(nameof(output));

		if(string.IsNullOrEmpty(template))
			throw new ArgumentNullException(nameof(template));

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.Export(), options, cancellation);

		//获取指定的数据模板
		var templateObject = DataTemplateUtility.GetTemplate(this.ServiceProvider, template, format) ??
		(
			string.IsNullOrEmpty(format) ?
			throw OperationException.Unfound($"The data template with specified the name '{template}' was not found.") :
			throw OperationException.Unfound($"The data template with specified the name '{template}' and the format '{format}' was not found.")
		);

		//获取数据模板对应的模型数据和参数
		var model = await this.GetExportModelAsync(templateObject, argument, parameters, options, cancellation);

		//执行数据模板渲染导出
		return await this.OnExportAsync(output, templateObject, model.Data, model.Parameters, options, cancellation);
	}

	protected virtual async ValueTask<IDataTemplateModel> GetExportModelAsync(IDataTemplate template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, DataExportOptions options, CancellationToken cancellation)
	{
		var provider = this.ServiceProvider.Find<IDataTemplateModelProvider>(template) ??
			throw OperationException.Unfound($"No data template model provider found for '{template.Name}' template.");

		//获取指定模板和参数对应数据模型
		var model = await provider.GetModelAsync(template, argument, cancellation);

		//如果传入的参数集不为空，则需传入的参数集加入到模型参数集中
		if(parameters != null)
		{
			if(model == null)
				model = new DataTemplateModel(null, parameters);
			else
			{
				foreach(var parameter in parameters)
					model.Parameters[parameter.Key] = parameter.Value;
			}
		}

		return model;
	}

	protected virtual async ValueTask<DataArchiveFormat> OnExportAsync(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, DataExportOptions options, CancellationToken cancellation)
	{
		var renderer = this.ServiceProvider.Find<IDataTemplateRenderer>(template) ?? throw OperationException.Unfound($"No renderer found for '{template.Name}' template.");
		await renderer.RenderAsync(output, template, data, parameters, cancellation);
		return renderer.Format;
	}
	#endregion
}
