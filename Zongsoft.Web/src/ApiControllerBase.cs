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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Zongsoft.IO;
using Zongsoft.Data;

namespace Zongsoft.Web
{
	[ApiController]
	public class ApiControllerBase<TModel, TService> : ControllerBase where TService : class, IDataService<TModel>
	{
		#region 单例字段
		private static readonly WebFileAccessor _accessor = new WebFileAccessor();
		#endregion

		#region 成员字段
		private TService _dataService;
		private IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		protected ApiControllerBase(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 属性定义
		protected virtual bool CanDelete
		{
			get => this.DataService.CanDelete;
		}

		protected virtual bool CanCreate
		{
			get => this.DataService.CanInsert;
		}

		protected virtual bool CanUpdate
		{
			get => this.DataService.CanUpdate;
		}

		protected virtual bool CanUpsert
		{
			get => this.CanCreate && this.CanUpdate && this.DataService.CanUpsert;
		}

		protected TService DataService
		{
			get
			{
				if(_dataService == null)
					_dataService = this.GetService() ?? throw new InvalidOperationException("Missing required data service.");

				return _dataService;
			}
		}

		protected IServiceProvider ServiceProvider
		{
			get => _serviceProvider;
			set => _serviceProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 公共方法
		[HttpGet("{key}/[action]")]
		[HttpGet("[action]/{key:required}")]
		public virtual IActionResult Count(string key, [FromQuery]string filter = null)
		{
			return this.Content(this.DataService.Count(key, null, new DataAggregateOptions(filter)).ToString());
		}

		[HttpPost("[action]")]
		public virtual async Task<IActionResult> Count()
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			return this.Content(this.DataService.Count(Criteria.Transform(criteria as IModel)).ToString());
		}

		[HttpGet("{key}/[action]")]
		[HttpGet("[action]/{key:required}")]
		public virtual IActionResult Exists(string key, [FromQuery]string filter = null)
		{
			return this.DataService.Exists(key, new DataExistsOptions(filter)) ?
			       this.NoContent() : this.NotFound();
		}

		[HttpPost("[action]")]
		public virtual async Task<IActionResult> Exists()
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			return this.DataService.Exists(Criteria.Transform(criteria as IModel)) ?
			       this.NoContent() : this.NotFound();
		}

		[HttpGet("[action]/{**keyword}")]
		public virtual IActionResult Search(string keyword, [FromQuery]string filter = null, [FromQuery]Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))]Sorting[] sort = null)
		{
			var searcher = this.DataService.Searcher;

			if(searcher == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(string.IsNullOrWhiteSpace(keyword))
				return this.BadRequest("Missing keyword for search.");

			return this.Paginate(searcher.Search(keyword, this.GetSchema(), page ?? Paging.Page(1), filter, sort));
		}

		[HttpGet("{key?}")]
		public virtual IActionResult Get(string key, [FromQuery]string filter = null, [FromQuery]Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))]Sorting[] sort = null)
		{
			return this.Paginate(this.OnGet(key, filter, page, sort));
		}

		[HttpDelete("{key?}")]
		public virtual async Task<IActionResult> Delete(string key, [FromQuery]string filter = null)
		{
			if(!this.CanDelete)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(string.IsNullOrWhiteSpace(key))
			{
				var content = await this.Request.ReadAsStringAsync();

				if(string.IsNullOrWhiteSpace(content))
					return this.BadRequest();

				var parts = Common.StringExtension.Slice(content, ',', '|');

				if(parts != null && parts.Any())
				{
					var count = 0;

					using(var transaction = new Zongsoft.Transactions.Transaction())
					{
						foreach(var part in parts)
							count += this.OnDelete(part, filter);

						transaction.Commit();
					}

					return count > 0 ? (IActionResult)this.Content(count.ToString()) : this.NotFound();
				}
			}

			return this.OnDelete(key, filter) > 0 ? this.NoContent() : this.NotFound();
		}

		[HttpPost]
		public virtual IActionResult Create([FromBody]TModel model)
		{
			if(!this.CanCreate)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			//确认模型是否有效
			if(!this.TryValidateModel(model))
				return this.UnprocessableEntity();

			static object GetModelMemberValue(ref TModel target, string member)
			{
				if(target is IModel model)
					return model.TryGetValue(member, out var value) ? value : null;
				else
					return Reflection.Reflector.TryGetValue(ref target, member, out var value) ? value : null;
			}

			if(this.OnCreate(model) > 0)
			{
				var keys = this.DataService.DataAccess.Metadata.Entities[this.DataService.Name].Key;

				if(keys == null || keys.Length == 0)
					return this.CreatedAtAction(nameof(Get), this.RouteData.Values, model);

				var text = new System.Text.StringBuilder(50);

				for(int i = 0; i < keys.Length; i++)
				{
					if(text.Length > 0)
						text.Append('-');

					text.Append(GetModelMemberValue(ref model, keys[0].Name)?.ToString());
				}

				this.RouteData.Values["key"] = text.ToString();
				return this.CreatedAtAction(nameof(Get), this.RouteData.Values, model);
			}

			return this.Conflict();
		}

		[HttpPut]
		public virtual IActionResult Upsert([FromBody]TModel model)
		{
			if(!this.CanUpsert)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			//确认模型是否有效
			if(!this.TryValidateModel(model))
				return this.UnprocessableEntity();

			return this.OnUpsert(model) > 0 ? this.Ok(model) : this.Conflict();
		}

		[HttpPatch("{key}")]
		public virtual IActionResult Update(string key, [FromBody]TModel model)
		{
			if(!this.CanUpdate)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			//确认模型是否有效
			if(!this.TryValidateModel(model))
				return this.UnprocessableEntity();

			return this.OnUpdate(key, model) > 0 ?
				this.NoContent() : this.NotFound();
		}

		[HttpPost("[action]")]
		public virtual async Task<IActionResult> Query([FromQuery]Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))]Sorting[] sort = null)
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			return this.Paginate(this.DataService.Select(Criteria.Transform(criteria as IModel), Http.Headers.HeaderDictionaryExtension.GetDataSchema(this.Request.Headers), page ?? Paging.Page(1), sort));
		}
		#endregion

		#region 上传方法
		protected async Task<FileInfo> UploadAsync(string path, Func<FileInfo, bool> uploaded = null)
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
			});

			//依次遍历写入的文件对象
			await foreach(var file in files)
			{
				//如果上传回调方法返回真(True)则将其加入到结果集中，否则删除刚保存的文件
				if(uploaded == null || uploaded(file))
					return file;

				this.DeleteFile(file.Path.Url);
			}

			return null;
		}

		protected async Task<IEnumerable<T>> UploadAsync<T>(string path, Func<FileInfo, T> uploaded, int limit = 0)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			if(!Path.TryParse(path, out var pathInfo))
				throw new ArgumentException($"Invalid path format.");

			if(uploaded == null)
				throw new ArgumentNullException(nameof(uploaded));

			//如果上传的内容为空，则返回文件信息的空集
			if(this.Request.Body == null || this.Request.ContentLength == null || this.Request.ContentLength == 0)
				return Enumerable.Empty<T>();

			//将上传的文件内容依次写入到指定的目录中
			var files = _accessor.Write(this.Request, pathInfo.GetDirectoryUrl(), option =>
			{
				if(limit > 0)
					option.Cancel = option.Index > limit;

				if(!option.Cancel && limit != 1 && pathInfo.IsFile)
					option.FileName = pathInfo.FileName + "-" + Zongsoft.Common.Randomizer.GenerateString();
			});

			T item;
			var result = new List<T>();

			//依次遍历写入的文件对象
			await foreach(var file in files)
			{
				//如果上传回调方法返回不为空则将其加入到结果集中，否则删除刚保存的文件
				if((item = uploaded(file)) != null)
					result.Add(item);
				else
					this.DeleteFile(file.Path.Url);
			}

			return result;
		}
		#endregion

		#region 虚拟方法
		protected virtual TService GetService()
		{
			return (TService)_serviceProvider.GetService(typeof(TService)) ??
				throw new InvalidOperationException("Missing the required service.");
		}

		protected virtual object OnGet(string key, string filter, Paging page, Sorting[] sortings, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.Get(key, this.GetSchema(), page ?? Paging.Page(1), new DataSelectOptions(filter, parameters), sortings);
		}

		protected virtual int OnDelete(string key, string filter, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			if(string.IsNullOrWhiteSpace(key))
				return 0;

			return this.DataService.Delete(key, this.GetSchema(), new DataDeleteOptions(filter, parameters));
		}

		protected virtual int OnCreate(TModel model, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.Insert(model, this.GetSchema(), DataInsertOptions.Parameter(parameters));
		}

		protected virtual int OnUpdate(string key, TModel model, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			if(string.IsNullOrWhiteSpace(key))
				return this.DataService.Update(model, this.GetSchema(), DataUpdateOptions.Parameter(parameters));
			else
				return this.DataService.Update(key, model, this.GetSchema(), DataUpdateOptions.Parameter(parameters));
		}

		protected virtual int OnUpsert(TModel model, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.Upsert(model, this.GetSchema(), DataUpsertOptions.Parameter(parameters));
		}
		#endregion

		#region 保护方法
		protected IActionResult Paginate(object data)
		{
			if(data == null)
				return this.NotFound();

			if(data is IActionResult result)
				return result;

			//如果模型类型是值类型并且结果数据类型是该值类型并且结果数据等于空值，则返回HTTP状态为无内容
			if(typeof(TModel).IsValueType && data.GetType() == typeof(TModel) && EqualityComparer<TModel>.Default.Equals((TModel)data, default))
				return this.NoContent();

			return WebUtility.Paginate(data);
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void DeleteFile(string filePath)
		{
			try
			{
				if(filePath != null && filePath.Length > 0)
					FileSystem.File.Delete(filePath);
			}
			catch { }
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetSchema()
		{
			return Http.Headers.HeaderDictionaryExtension.GetDataSchema(this.Request.Headers);
		}
		#endregion
	}
}
