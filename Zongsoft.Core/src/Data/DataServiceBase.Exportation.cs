/*
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Data.Templates;

namespace Zongsoft.Data
{
	public partial class DataServiceBase<TModel> : IDataExportable
	{
		#region 公共属性
		public virtual bool CanExport => true;
		#endregion

		#region 数据文件生成
		public void Export(Stream output, object data, string format = null, DataExportOptions options = null) => this.Export(output, data, null, format, options);
		public void Export(Stream output, object data, string[] members, string format = null, DataExportOptions options = null)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));

			//进行授权验证
			this.Authorize(DataServiceMethod.Export(), options);

			//执行数据文件生成导出
			this.OnExport(output, data, members, format, options);
		}

		public ValueTask<DataArchiveFormat> ExportAsync(Stream output, object data, string format = null, DataExportOptions options = null, CancellationToken cancellation = default) => this.ExportAsync(output, data, null, format, options, cancellation);
		public ValueTask<DataArchiveFormat> ExportAsync(Stream output, object data, string[] members, string format = null, DataExportOptions options = null, CancellationToken cancellation = default)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));

			//进行授权验证
			this.Authorize(DataServiceMethod.Export(), options);

			//执行数据文件生成导出
			return this.OnExportAsync(output, data, members, format, options, cancellation);
		}

		protected virtual DataArchiveFormat OnExport(Stream output, object data, string[] members, string format, DataExportOptions options)
		{
			var generator = this.ServiceProvider.Resolve<IDataArchiveGenerator>(format) ?? throw OperationException.Unfound();
			var task = generator.GenerateAsync(output, this.GetDescriptor(), data, (members == null || members.Length == 0 ? null : new DataArchiveGeneratorOptions(members)));

			if(!task.IsCompletedSuccessfully)
				task.AsTask().GetAwaiter().GetResult();

			return generator.Format;
		}

		protected virtual async ValueTask<DataArchiveFormat> OnExportAsync(Stream output, object data, string[] members, string format, DataExportOptions options, CancellationToken cancellation)
		{
			var generator = this.ServiceProvider.Resolve<IDataArchiveGenerator>(format) ?? throw OperationException.Unfound();
			await generator.GenerateAsync(output, this.GetDescriptor(), data, (members == null || members.Length == 0 ? null : new DataArchiveGeneratorOptions(members)), cancellation);
			return generator.Format;
		}
		#endregion

		#region 数据模板渲染
		public void Export(Stream output, string template, object argument, string format = null, DataExportOptions options = null) => this.Export(output, template, argument, null, format, options);
		public void Export(Stream output, string template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, string format = null, DataExportOptions options = null)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));

			if(string.IsNullOrEmpty(template))
				throw new ArgumentNullException(nameof(template));

			//进行授权验证
			this.Authorize(DataServiceMethod.Export(), options);

			//获取指定的数据模板
			var templateObject = DataTemplateUtility.GetTemplate(this.ServiceProvider, template, format);
			if(templateObject == null)
				throw OperationException.Unfound();

			//获取数据模板对应的模型数据和参数
			var model = this.GetExportModel(templateObject, argument, parameters, options);

			//执行数据模板渲染导出
			this.OnExport(output, templateObject, model.Data, model.Parameters, options);
		}

		public ValueTask<DataArchiveFormat> ExportAsync(Stream output, string template, object argument, string format = null, DataExportOptions options = null, CancellationToken cancellation = default) => this.ExportAsync(output, template, argument, null, format, options, cancellation);
		public ValueTask<DataArchiveFormat> ExportAsync(Stream output, string template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, string format = null, DataExportOptions options = null, CancellationToken cancellation = default)
		{
			if(output == null)
				throw new ArgumentNullException(nameof(output));

			if(string.IsNullOrEmpty(template))
				throw new ArgumentNullException(nameof(template));

			//进行授权验证
			this.Authorize(DataServiceMethod.Export(), options);

			//获取指定的数据模板
			var templateObject = DataTemplateUtility.GetTemplate(this.ServiceProvider, template, format) ??
			(
				string.IsNullOrEmpty(format) ?
				throw OperationException.Unfound($"The data template with specified the name '{template}' was not found.") :
				throw OperationException.Unfound($"The data template with specified the name '{template}' and the format '{format}' was not found.")
			);

			//获取数据模板对应的模型数据和参数
			var model = this.GetExportModel(templateObject, argument, parameters, options);

			//执行数据模板渲染导出
			return this.OnExportAsync(output, templateObject, model.Data, model.Parameters, options, cancellation);
		}

		protected virtual IDataTemplateModel GetExportModel(IDataTemplate template, object argument, IEnumerable<KeyValuePair<string, object>> parameters, DataExportOptions options)
		{
			var provider = this.ServiceProvider.Resolve<IDataTemplateModelProvider>(template) ??
				throw OperationException.Unfound($"No data template model provider found for '{template.Name}' template.");

			//获取指定模板和参数对应数据模型
			var model = provider.GetModel(template, argument);

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

		protected virtual DataArchiveFormat OnExport(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, DataExportOptions options)
		{
			var renderer = this.ServiceProvider.Resolve<IDataTemplateRenderer>(template);
			var task = renderer.RenderAsync(output, template, data, parameters);

			if(!task.IsCompletedSuccessfully)
				task.AsTask().GetAwaiter().GetResult();

			return renderer.Format;
		}

		protected virtual async ValueTask<DataArchiveFormat> OnExportAsync(Stream output, IDataTemplate template, object data, IEnumerable<KeyValuePair<string, object>> parameters, DataExportOptions options, CancellationToken cancellation)
		{
			var renderer = this.ServiceProvider.Resolve<IDataTemplateRenderer>(template) ?? throw OperationException.Unfound($"No renderer found for '{template.Name}' template.");
			await renderer.RenderAsync(output, template, data, parameters, cancellation);
			return renderer.Format;
		}
		#endregion
	}
}