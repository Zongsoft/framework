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
		[HttpGet("[action]/{key:required}")]
		public Task<IActionResult> CountAsync(string key)
		{
			return Task.FromResult((IActionResult)this.Content(this.DataService.Count<string>(key).ToString()));
		}

		[HttpGet("[action]/{key1:required}-{key2:required}")]
		public Task<IActionResult> CountAsync(string key1, string key2)
		{
			return Task.FromResult((IActionResult)this.Content(this.DataService.Count<string, string>(key1, key2).ToString()));
		}

		[HttpGet("[action]/{key1:required}-{key2:required}-{key3:required}")]
		public Task<IActionResult> CountAsync(string key1, string key2, string key3)
		{
			return Task.FromResult((IActionResult)this.Content(this.DataService.Count<string, string, string>(key1, key2, key3).ToString()));
		}

		[HttpGet("[action]/{key:required}")]
		public Task<IActionResult> ExistsAsync(string key)
		{
			return this.DataService.Exists(key) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpGet("[action]/{key1:required}-{key2:required}")]
		public Task<IActionResult> ExistsAsync(string key1, string key2)
		{
			return this.DataService.Exists(key1, key2) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpGet("[action]/{key1:required}-{key2:required}-{key3:required}")]
		public Task<IActionResult> ExistsAsync(string key1, string key2, string key3)
		{
			return this.DataService.Exists(key1, key2, key3) ?
				Task.FromResult((IActionResult)this.NoContent()) :
				Task.FromResult((IActionResult)this.NotFound());
		}

		[HttpGet("[action]")]
		public Task<IActionResult> SearchAsync([FromQuery]string keyword, [FromQuery]Paging paging = null)
		{
			var searcher = this.DataService.Searcher;

			if(searcher == null)
				return Task.FromResult((IActionResult)this.BadRequest("This resource does not support the search operation."));

			if(string.IsNullOrWhiteSpace(keyword))
				return Task.FromResult((IActionResult)this.BadRequest("Missing keyword for search."));

			return Task.FromResult((IActionResult)this.Paginate(searcher.Search(keyword, this.GetSchema(), paging)));
		}

		[HttpGet("{key?}")]
		public IActionResult Get(string key, [FromQuery]Paging paging = null)
		{
			if(string.IsNullOrWhiteSpace(key))
				return this.Paginate(this.DataService.Select(null, this.GetSchema(), paging));

			return key.Contains(':') && this.DataService.Searcher != null ?
				this.Paginate(this.DataService.Searcher.Search(key, this.GetSchema(), paging)) :
				this.Paginate(this.DataService.Get<string>(key, this.GetSchema(), paging, null, out var pageable), pageable);
		}

		[HttpGet("{key1:required}-{key2:required}")]
		public IActionResult Get(string key1, string key2, [FromQuery]Paging paging = null)
		{
			return this.Paginate(this.DataService.Get<string, string>(key1, key2, this.GetSchema(), paging, null, out var pageable), pageable);
		}

		[HttpGet("{key1:required}-{key2:required}-{key3:required}")]
		public IActionResult Get(string key1, string key2, string key3, [FromQuery]Paging paging = null)
		{
			return this.Paginate(this.DataService.Get<string, string, string>(key1, key2, key3, this.GetSchema(), paging, null, out var pageable), pageable);
		}

		[HttpDelete("{keys:required}")]
		public IActionResult Delete(string keys)
		{
			if(!this.CanDelete)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(string.IsNullOrWhiteSpace(keys))
				return this.BadRequest();

			int count = 0;
			var parts = Common.StringExtension.Slice(keys, ',', '|').ToArray();

			if(parts != null && parts.Length > 1)
			{
				using(var transaction = new Zongsoft.Transactions.Transaction())
				{
					foreach(var key in parts)
					{
						count += this.OnDelete(Common.StringExtension.Slice(key, '-').ToArray());
					}

					transaction.Commit();
				}

				return count > 0 ? (IActionResult)this.Ok(count) : this.NotFound();
			}

			count = this.OnDelete(Common.StringExtension.Slice(keys, '-').ToArray());
			return count > 0 ? (IActionResult)this.Ok(count) : this.NotFound();
		}

		[HttpPost]
		public IActionResult Create([FromBody]TModel model)
		{
			if(!this.CanCreate)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			//确认模型是否有效
			if(!this.TryValidateModel(model))
				return this.UnprocessableEntity();

			static object GetModelMemberValue(object target, string member)
			{
				if(target is IModel model)
					return model.TryGetValue(member, out var value) ? value : null;
				else
					return Reflection.Reflector.TryGetValue(target, member, out var value) ? value : null;
			}

			if(this.OnCreate(model) > 0)
			{
				var keys = this.DataService.DataAccess.Metadata.Entities.Get(this.DataService.Name).Key;

				object route = keys.Length switch
				{
					1 => new { key = GetModelMemberValue(model, keys[0].Name) },
					2 => new { key1 = GetModelMemberValue(model, keys[0].Name), key2 = GetModelMemberValue(model, keys[1].Name) },
					3 => new { key1 = GetModelMemberValue(model, keys[0].Name), key2 = GetModelMemberValue(model, keys[1].Name), key3 = GetModelMemberValue(model, keys[2].Name) },
					_ => new { key = string.Empty },
				};

				return this.CreatedAtAction(nameof(Get), route, model);
			}

			return this.Conflict();
		}

		[HttpPut]
		public IActionResult Upsert([FromBody]TModel model)
		{
			if(!(this.CanCreate && this.CanUpdate))
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			//确认模型是否有效
			if(!this.TryValidateModel(model))
				return this.UnprocessableEntity();

			return this.OnUpsert(model) > 0 ? (IActionResult)this.Ok(model) : this.Conflict();
		}

		[HttpPatch("{keys}")]
		public IActionResult Update(string keys, [FromBody]TModel model)
		{
			if(!this.CanUpdate)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			//确认模型是否有效
			if(!this.TryValidateModel(model))
				return this.UnprocessableEntity();

			return this.OnUpdate(model, string.IsNullOrEmpty(keys) ? Array.Empty<string>() : Common.StringExtension.Slice(keys, '-').ToArray()) > 0 ?
				(IActionResult)this.Ok() : this.NotFound();
		}
		#endregion

		#region 上传方法
		protected async Task<FileInfo> UploadAsync(string path, Func<FileInfo, bool> uploaded = null)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			//如果上传的内容为空，则返回文件信息的空集
			if(this.Request.Body == null || this.Request.ContentLength == null || this.Request.ContentLength == 0)
			{
				uploaded?.Invoke(null);
				return null;
			}

			//将上传的文件内容依次写入到指定的目录中
			var files = _accessor.Write(this.Request, path, option => option.Cancel = option.Index > 1);

			//依次遍历写入的文件对象
			await foreach(var file in files)
			{
				//如果上传回调方法返回真(True)则将其加入到结果集中，否则删除刚保存的文件
				if(uploaded == null || uploaded(file))
					return file;

				this.DeleteFile(file.Url);
			}

			return null;
		}

		protected async Task<IEnumerable<T>> UploadAsync<T>(string path, Func<FileInfo, T> uploaded, int limit = 0)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			if(uploaded == null)
				throw new ArgumentNullException(nameof(uploaded));

			//如果上传的内容为空，则返回文件信息的空集
			if(this.Request.Body == null || this.Request.ContentLength == null || this.Request.ContentLength == 0)
				return Enumerable.Empty<T>();

			//将上传的文件内容依次写入到指定的目录中
			var files = _accessor.Write(this.Request, path, option =>
			{
				if(limit > 0)
					option.Cancel = option.Index > limit;

				if(!option.Cancel && limit != 1)
					option.FileName = option.FileName + "-" + Zongsoft.Common.Randomizer.GenerateString();
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
					this.DeleteFile(file.Url);
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

		protected virtual int OnDelete(params string[] keys)
		{
			if(keys == null || keys.Length == 0)
				return 0;

			switch(keys.Length)
			{
				case 1:
					return this.DataService.Delete<string>(keys[0], this.GetSchema());
				case 2:
					return this.DataService.Delete<string, string>(keys[0], keys[1], this.GetSchema());
				case 3:
					return this.DataService.Delete<string, string, string>(keys[0], keys[1], keys[2], this.GetSchema());
				default:
					throw new ArgumentException("Too many keys specified.");
			}
		}

		protected virtual int OnCreate(TModel model)
		{
			return this.DataService.Insert(model, this.GetSchema());
		}

		protected virtual int OnUpdate(TModel model, params string[] keys)
		{
			if(keys == null || keys.Length == 0)
				return this.DataService.Update(model, this.GetSchema());

			switch(keys.Length)
			{
				case 1:
					return this.DataService.Update(model, keys[0], this.GetSchema());
				case 2:
					return this.DataService.Update(model, keys[0], keys[1], this.GetSchema());
				case 3:
					return this.DataService.Update(model, keys[0], keys[1], keys[2], this.GetSchema());
				default:
					throw new ArgumentException("Too many keys specified.");
			}
		}

		protected virtual int OnUpsert(TModel model)
		{
			return this.DataService.Upsert(model, this.GetSchema());
		}
		#endregion

		#region 保护方法
		protected IActionResult Paginate(object data, IPageable pageable = null)
		{
			if(data == null)
				return this.NoContent();

			//如果模型类型是值类型并且结果数据类型是该值类型并且结果数据等于空值，则返回HTTP状态为无内容
			if(typeof(TModel).IsValueType && data.GetType() == typeof(TModel) && EqualityComparer<TModel>.Default.Equals((TModel)data, default))
				return this.NoContent();

			if(pageable == null)
				pageable = data as IPageable;

			if(pageable != null)
			{
				var result = new ResultEntity(data);

				pageable.Paginated += Paginator_Paginated;

				void Paginator_Paginated(object sender, PagingEventArgs e)
				{
					pageable.Paginated -= Paginator_Paginated;

					if(result.Paging == null)
						result.Paging = new Dictionary<string, string>();

					result.Paging[string.IsNullOrEmpty(e.Name) ? "$" : e.Name] = GetPaging(e.Paging);
				}

				return this.Ok(result);
			}

			return this.Ok(data);
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

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetPaging(Paging paging)
		{
			if(paging == null || Paging.IsDisabled(paging))
				return null;

			return paging.PageIndex.ToString() + "/" +
				   paging.PageCount.ToString() + "(" +
				   paging.TotalCount.ToString() + ")";
		}
		#endregion

		#region 嵌套子类
		private class ResultEntity
		{
			[System.Text.Json.Serialization.JsonPropertyName("$")]
			[Serialization.SerializationMember("$")]
			public object Data { get; set; }

			[System.Text.Json.Serialization.JsonPropertyName("$$")]
			[Serialization.SerializationMember("$$")]
			public IDictionary<string, string> Paging { get; set; }

			public ResultEntity(object data)
			{
				this.Data = data;
				this.Paging = null;
			}
		}
		#endregion
	}
}
