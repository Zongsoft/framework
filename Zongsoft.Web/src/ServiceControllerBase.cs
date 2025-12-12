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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web library.
 *
 * The Zongsoft.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.IO;
using Zongsoft.Data;
using Zongsoft.Web.Http;

namespace Zongsoft.Web;

[ApiController]
[Route("[area]/[controller]")]
public abstract class ServiceControllerBase<TModel, TService> : ControllerBase where TService : class, IDataService<TModel>
{
	#region 单例字段
	private static readonly WebFileAccessor _accessor = new WebFileAccessor();
	#endregion

	#region 成员字段
	private TService _dataService;
	#endregion

	#region 构造函数
	protected ServiceControllerBase() => this.OptionsBuilder = new DataOptionsBuilder(this);
	#endregion

	#region 属性定义
	protected virtual bool CanDelete => this.DataService.CanDelete;
	protected virtual bool CanCreate => this.DataService.CanInsert;
	protected virtual bool CanUpdate => this.DataService.CanUpdate;
	protected virtual bool CanUpsert => this.CanCreate && this.CanUpdate && this.DataService.CanUpsert;
	protected virtual bool CanImport => this.DataService is IDataImportable importable && importable.CanImport;
	protected virtual bool CanExport => this.DataService is IDataExportable exportable && exportable.CanExport;
	protected TService DataService => _dataService ??= this.GetService() ?? throw new InvalidOperationException("Missing required data service.");
	internal DataOptionsBuilder OptionsBuilder { get; }
	#endregion

	#region 查询方法
	[HttpPost("[action]")]
	public virtual async ValueTask<IActionResult> QueryAsync([FromQuery]Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))]Sorting[] sort = null, CancellationToken cancellation = default)
	{
		if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

		var criteria = await Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria, cancellation: cancellation);
		return this.Paginate(page ??= Paging.First(), this.OnQueryAsync(criteria, page, sort, null, cancellation));
	}
	#endregion

	#region 导入导出
	[HttpPost("[action]")]
	public virtual async ValueTask<IActionResult> ImportAsync(IFormFile file, [FromQuery] string format = null, CancellationToken cancellation = default)
	{
		if(file == null || file.Length == 0)
			return this.BadRequest();

		if(!this.CanImport)
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

		var count = this.DataService is IDataImportable importable ?
			await importable.ImportAsync(file.OpenReadStream(), format, this.OptionsBuilder.Import(), cancellation) : 0;

		return count > 0 ? this.Content(count.ToString()) : this.NoContent();
	}

	[HttpGet("[action]")]
	[HttpGet("{key}/[action]")]
	public virtual async ValueTask<IActionResult> ExportAsync(string key, [FromQuery] string format = null, [FromQuery] Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))] Sorting[] sort = null, CancellationToken cancellation = default)
	{
		if(!this.CanExport)
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

		if(this.DataService is IDataExportable exportable)
		{
			var data = await this.OnGetAsync(key, page ??= Paging.First(), sort, null, cancellation);

			//设置响应分页头
			this.Response.Headers.SetPagination(page);

			//执行数据导出操作
			return await this.OnExportAsync(exportable, data, format, cancellation);
		}

		return this.StatusCode(StatusCodes.Status405MethodNotAllowed);
	}

	[HttpPost("[action]")]
	public virtual async ValueTask<IActionResult> ExportAsync([FromQuery] string format = null, [FromQuery] Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))] Sorting[] sort = null, CancellationToken cancellation = default)
	{
		if(!this.CanExport)
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

		if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

		if(this.DataService is IDataExportable exportable)
		{
			var criteria = await Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria, null, cancellation);
			var data = this.OnQueryAsync(criteria,
				page ??= Paging.First(),
				sort, null, cancellation);

			//设置响应分页头
			this.Response.Headers.SetPagination(page);

			//执行数据导出操作
			return await this.OnExportAsync(exportable, data, format, cancellation);
		}

		return this.StatusCode(StatusCodes.Status405MethodNotAllowed);
	}

	[HttpGet("[action]/{template}/{argument?}")]
	public virtual async ValueTask<IActionResult> ExportAsync(string template, string argument, [FromQuery] string format = null, CancellationToken cancellation = default)
	{
		if(!this.CanExport)
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

		if(this.DataService is IDataExportable exportable)
			return await this.OnExportAsync(exportable, template, argument, format, cancellation);
		else
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);
	}

	[HttpPost("[action]/{template}")]
	public virtual async ValueTask<IActionResult> ExportAsync(string template, [FromQuery] string format = null, CancellationToken cancellation = default)
	{
		if(!this.CanExport)
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

		if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

		if(this.DataService is IDataExportable exportable)
			return await this.OnExportAsync(exportable, template, await Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria, null, cancellation), format, cancellation);
		else
			return this.StatusCode(StatusCodes.Status405MethodNotAllowed);
	}
	#endregion

	#region 上传方法
	protected async ValueTask<FileInfo> UploadAsync(string path, Func<FileInfo, bool> uploaded = null, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		if(!Path.TryParse(path, out var pathInfo))
			throw new ArgumentException($"Invalid path format.");

		//如果上传的内容为空，则返回文件信息的空集
		if(this.Request.Body == null || this.Request.ContentLength == null || this.Request.ContentLength == 0)
		{
			uploaded?.Invoke(null);
			return null;
		}

		//将上传的文件内容依次写入到指定的目录中
		var files = _accessor.Write(this.Request, pathInfo.GetDirectoryUrl(), option =>
		{
			if(pathInfo.IsFile)
				option.FileName = pathInfo.FileName;

			option.Cancel = option.Index > 1;
		}, cancellation);

		//依次遍历写入的文件对象
		await foreach(var file in files)
		{
			if(file == null)
				continue;

			try
			{
				//上传回调方法返回真(True)则将其加入到结果集中，否则删除刚保存的文件
				if(uploaded == null || uploaded(file))
					return file;
			}
			catch
			{
				DeleteFile(file.Path.Url);
			}
		}

		return null;
	}

	protected IAsyncEnumerable<T> UploadAsync<T>(string path, Func<FileInfo, T> uploaded, CancellationToken cancellation = default) => this.UploadAsync(path, uploaded, 0, cancellation);
	protected async IAsyncEnumerable<T> UploadAsync<T>(string path, Func<FileInfo, T> uploaded, int limit, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		if(!Path.TryParse(path, out var pathInfo))
			throw new ArgumentException($"Invalid path format.");

		if(uploaded == null)
			throw new ArgumentNullException(nameof(uploaded));

		//如果上传的内容为空，则返回文件信息的空集
		if(this.Request.Body == null || this.Request.ContentLength == null || this.Request.ContentLength == 0)
			yield break;

		//将上传的文件内容依次写入到指定的目录中
		var files = _accessor.Write(this.Request, pathInfo.GetDirectoryUrl(), option =>
		{
			if(limit > 0)
				option.Cancel = option.Index > limit;

			if(!option.Cancel && limit != 1 && pathInfo.IsFile)
				option.FileName = pathInfo.FileName + "-" + Zongsoft.Common.Randomizer.GenerateString();
		}, cancellation);

		//依次遍历写入的文件对象
		await foreach(var file in files)
		{
			if(file == null)
				continue;

			T entry;

			//如果上传回调方法返回不为空则将其加入到结果集中，否则删除刚保存的文件
			if((entry = uploaded(file)) != null)
				yield return entry;
			else
				DeleteFile(file.Path.Url);
		}
	}
	#endregion

	#region 保护方法
	protected IActionResult Paginate(Paging paging, object data) => WebUtility.Paginate(this, data, paging);
	protected IActionResult Paginate(object data, Paging paging) => WebUtility.Paginate(this, data, paging);
	#endregion

	#region 虚拟方法
	protected virtual TService GetService()
	{
		/*
		 * 注：对于嵌套子服务的解析需要从其顶级服务类按嵌套层次依次获取其子服务。
		 */

		if(typeof(TService).IsNested)
		{
			/*
			 * 注：为单层嵌套子服务的执行性能考量，下面针对单层嵌套与多层嵌套分别进行处理。
			 */

			if(typeof(TService).DeclaringType.IsNested)
			{
				var type = typeof(TService);
				var stack = new Stack<Type>();

				while(type.IsNested)
				{
					stack.Push(type);
					type = type.DeclaringType;
				}

				if(this.HttpContext.RequestServices.GetService(type) is IDataService service)
				{
					while(service != null && stack.TryPop(out type))
					{
						service = service.GetSubservice(type);
					}

					if(service is TService result)
						return result;
				}
			}
			else
			{
				if(this.HttpContext.RequestServices.GetService(typeof(TService).DeclaringType) is IDataService service)
				{
					var result = service.GetSubservice<TService>();

					if(result != null)
						return result;
				}
			}
		}

		return (TService)this.HttpContext.RequestServices.GetService(typeof(TService)) ?? throw new InvalidOperationException("Missing the required service.");
	}

	protected virtual ValueTask<object> OnGetAsync(string key, Paging page, Sorting[] sort, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
	{
		return this.DataService.GetAsync(key, this.GetSchema(), page ?? Paging.First(), this.OptionsBuilder.Get(parameters), sort, cancellation);
	}

	protected virtual IAsyncEnumerable<TModel> OnQueryAsync(object criteria, Paging page, Sorting[] sort, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
	{
		return this.DataService.SelectAsync(Criteria.Transform(criteria as IModel), this.GetSchema(), page, this.OptionsBuilder.Select(), sort, cancellation);
	}

	protected virtual async ValueTask<IActionResult> OnExportAsync(IDataExportable exportable, object data, string format, CancellationToken cancellation)
	{
		var output = new System.IO.MemoryStream();
		var result = await exportable.ExportAsync(output, data, this.GetExportFields(), format, this.OptionsBuilder.Export(), cancellation);
		output.Seek(0, System.IO.SeekOrigin.Begin);
		return this.File(output, result.Type, this.DataService.Name + result.Extension);
	}

	protected virtual async ValueTask<IActionResult> OnExportAsync(IDataExportable exportable, string template, object argument, string format, CancellationToken cancellation)
	{
		var output = new System.IO.MemoryStream();
		var result = await exportable.ExportAsync(output, template, argument, this.GetParameters(DataServiceMethod.Export()), format, this.OptionsBuilder.Export(), cancellation);
		output.Seek(0, System.IO.SeekOrigin.Begin);
		return this.File(output, result.Type, template + result.Extension);
	}

	protected virtual IEnumerable<KeyValuePair<string, object>> GetParameters(DataServiceMethod method) => Http.HttpRequestUtility.GetParameters(this.Request);
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static void DeleteFile(string filePath)
	{
		try
		{
			if(filePath != null && filePath.Length > 0)
				FileSystem.File.Delete(filePath);
		}
		catch { }
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	internal string GetSchema() => HeaderDictionaryExtension.GetDataSchema(this.Request.Headers);

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private string[] GetExportFields() => this.Request.Headers.TryGetValue(Headers.ExportFields, out var content) && content.Count > 0 ?
		content.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : null;
	#endregion

	#region 嵌套子类
	internal class DataOptionsBuilder
	{
		private readonly ServiceControllerBase<TModel, TService> _controller;
		public DataOptionsBuilder(ServiceControllerBase<TModel, TService> controller) => _controller = controller;

		public DataSelectOptions Get(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Get()).Concat(parameters ?? []));
		public DataSelectOptions Select(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Select()).Concat(parameters ?? []));
		public DataDeleteOptions Delete(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Delete()).Concat(parameters ?? []));
		public DataInsertOptions Insert(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Insert()).Concat(parameters ?? []));
		public DataUpsertOptions Upsert(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Upsert()).Concat(parameters ?? []));
		public DataUpdateOptions Update(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Update()).Concat(parameters ?? []));
		public DataExistsOptions Exists(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Exists()).Concat(parameters ?? []));
		public DataExportOptions Export(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Export()).Concat(parameters ?? []));
		public DataImportOptions Import(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Import()).Concat(parameters ?? []));
		public DataAggregateOptions Count(IEnumerable<KeyValuePair<string, object>> parameters = null) => new(_controller.GetParameters(DataServiceMethod.Aggregate(DataAggregateFunction.Count)).Concat(parameters ?? []));
	}
	#endregion
}