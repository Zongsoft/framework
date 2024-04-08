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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Data;

namespace Zongsoft.Web
{
	public class ServiceController<TModel, TService> : ServiceControllerBase<TModel, TService> where TService : class, IDataService<TModel>
	{
		#region 构造函数
		protected ServiceController(IServiceProvider serviceProvider) : base(serviceProvider) { }
		#endregion

		#region 公共方法
		[HttpGet("{key}/[action]")]
		[HttpGet("[action]/{key:required}")]
		public virtual IActionResult Count(string key)
		{
			return this.Content(this.DataService.Count(key, null, this.OptionsBuilder.Count()).ToString());
		}

		[HttpPost("[action]")]
		public virtual async Task<IActionResult> Count()
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			return this.Content(this.DataService.Count(Criteria.Transform(criteria as IModel), null, this.OptionsBuilder.Count()).ToString());
		}

		[HttpGet("{key}/[action]")]
		[HttpGet("[action]/{key:required}")]
		public virtual IActionResult Exists(string key)
		{
			return this.DataService.Exists(key, this.OptionsBuilder.Exists()) ?
				   this.NoContent() : this.NotFound();
		}

		[HttpPost("[action]")]
		public virtual async Task<IActionResult> Exists()
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			return this.DataService.Exists(Criteria.Transform(criteria as IModel), this.OptionsBuilder.Exists()) ?
				   this.NoContent() : this.NotFound();
		}

		[HttpGet("{key?}")]
		public virtual IActionResult Get(string key, [FromQuery] Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))] Sorting[] sort = null)
		{
			return this.Paginate(this.OnGet(key, page, sort));
		}

		[HttpDelete("{key?}")]
		public virtual async Task<IActionResult> Delete(string key)
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
							count += this.OnDelete(part);

						transaction.Commit();
					}

					return count > 0 ? this.Content(count.ToString()) : this.NotFound();
				}
			}

			return this.OnDelete(key) > 0 ? this.NoContent() : this.NotFound();
		}

		[HttpPost]
		public virtual IActionResult Create([FromBody] TModel model)
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
		public virtual IActionResult Upsert([FromBody] TModel model)
		{
			if(!this.CanUpsert)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			//确认模型是否有效
			if(!this.TryValidateModel(model))
				return this.UnprocessableEntity();

			return this.OnUpsert(model) > 0 ? this.Ok(model) : this.Conflict();
		}

		[HttpPatch("{key}")]
		public virtual IActionResult Update(string key, [FromBody] TModel model)
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
		public virtual async Task<IActionResult> Query([FromQuery] Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))] Sorting[] sort = null)
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			var result = this.DataService.Select(Criteria.Transform(criteria as IModel), this.GetSchema(), page ?? Paging.Page(1), this.OptionsBuilder.Select(), sort);
			return this.Paginate(result);
		}
		#endregion

		#region 虚拟方法
		protected virtual object OnGet(string key, Paging page, Sorting[] sortings, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.Get(key, this.GetSchema(), page ?? Paging.Page(1), this.OptionsBuilder.Get(parameters), sortings);
		}

		protected virtual int OnDelete(string key, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return string.IsNullOrWhiteSpace(key) ? 0 : this.DataService.Delete(key, this.GetSchema(), this.OptionsBuilder.Delete(parameters));
		}

		protected virtual int OnCreate(TModel model, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.Insert(model, this.GetSchema(), this.OptionsBuilder.Insert(parameters));
		}

		protected virtual int OnUpdate(string key, TModel model, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return string.IsNullOrWhiteSpace(key) ?
				this.DataService.Update(model, this.GetSchema(), this.OptionsBuilder.Update(parameters)) :
				this.DataService.Update(key, model, this.GetSchema(), this.OptionsBuilder.Update(parameters));
		}

		protected virtual int OnUpsert(TModel model, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.Upsert(model, this.GetSchema(), this.OptionsBuilder.Upsert(parameters));
		}
		#endregion
	}

	public class SubserviceController<TModel, TService> : ServiceControllerBase<TModel, TService> where TService : class, IDataService<TModel>
	{
		#region 构造函数
		protected SubserviceController(IServiceProvider serviceProvider) : base(serviceProvider) { }
		#endregion

		#region 公共方法
		[HttpGet("[area]/[controller]/{key:required}/[action]")]
		public virtual IActionResult Count(string key)
		{
			if(string.IsNullOrWhiteSpace(key))
				return this.BadRequest();

			return this.Content(this.DataService.Count(key, null, this.OptionsBuilder.Count()).ToString());
		}

		[HttpPost("[area]/[controller]/[action]")]
		public virtual async Task<IActionResult> Count()
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			return this.Content(this.DataService.Count(Criteria.Transform(criteria as IModel), null, this.OptionsBuilder.Count()).ToString());
		}

		[HttpGet("[area]/[controller]/[action]/{key:required}")]
		public virtual IActionResult Exists(string key)
		{
			if(string.IsNullOrWhiteSpace(key))
				return this.BadRequest();

			return this.DataService.Exists(key, this.OptionsBuilder.Exists()) ?
				   this.NoContent() : this.NotFound();
		}

		[HttpPost("[area]/[controller]/[action]")]
		public virtual async Task<IActionResult> Exists()
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			return this.DataService.Exists(Criteria.Transform(criteria as IModel), this.OptionsBuilder.Exists()) ?
				   this.NoContent() : this.NotFound();
		}

		[HttpGet("[area]/{key:required}/[controller]")]
		[HttpGet("[area]/[controller]/{key:required}")]
		public virtual IActionResult Get(string key, [FromQuery] Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))] Sorting[] sort = null)
		{
			if(string.IsNullOrWhiteSpace(key))
				return this.BadRequest();

			return this.Paginate(this.OnGet(key, page, sort));
		}

		[HttpDelete("[area]/{key:required}/[controller]")]
		[HttpDelete("[area]/[controller]/{key?}")]
		public virtual async Task<IActionResult> Delete(string key)
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
							count += this.OnDelete(part);

						transaction.Commit();
					}

					return count > 0 ? this.Content(count.ToString()) : this.NotFound();
				}
			}

			return this.OnDelete(key) > 0 ? this.NoContent() : this.NotFound();
		}

		[HttpPost("[area]/{key:required}/[controller]")]
		public virtual IActionResult Create(string key, [FromBody]IEnumerable<TModel> data)
		{
			if(!this.CanCreate)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(string.IsNullOrWhiteSpace(key))
				return this.BadRequest();

			//确认模型是否有效
			if(!this.TryValidateModel(data))
				return this.UnprocessableEntity();

			if(this.OnCreate(key, data) > 0)
				return this.Created(this.Request.Path, data);

			return this.Conflict();
		}

		[HttpPut("[area]/{key:required}/[controller]")]
		public virtual IActionResult Upsert(string key, [FromBody]IEnumerable<TModel> data)
		{
			if(!this.CanUpsert)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(string.IsNullOrWhiteSpace(key))
				return this.BadRequest();

			//确认模型是否有效
			if(!this.TryValidateModel(data))
				return this.UnprocessableEntity();

			return this.OnUpsert(key, data) > 0 ? this.Ok(data) : this.Conflict();
		}

		[HttpPatch("[area]/{key:required}/[controller]")]
		public virtual IActionResult Update(string key, [FromBody]IEnumerable<TModel> data)
		{
			if(!this.CanUpdate)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			if(string.IsNullOrWhiteSpace(key))
				return this.BadRequest();

			//确认模型是否有效
			if(!this.TryValidateModel(data))
				return this.UnprocessableEntity();

			return this.OnUpdate(key, data) > 0 ?
				this.NoContent() : this.NotFound();
		}

		[HttpPost("[area]/[controller]/[action]")]
		public virtual async Task<IActionResult> Query([FromQuery]Paging page = null, [FromQuery][ModelBinder(typeof(Binders.SortingBinder))]Sorting[] sort = null)
		{
			if(this.DataService.Attribute == null || this.DataService.Attribute.Criteria == null)
				return this.StatusCode(StatusCodes.Status405MethodNotAllowed);

			var criteria = await Zongsoft.Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, this.DataService.Attribute.Criteria);
			var result = this.DataService.Select(Criteria.Transform(criteria as IModel), this.GetSchema(), page ?? Paging.Page(1), this.OptionsBuilder.Select(), sort);
			return this.Paginate(result);
		}
		#endregion

		#region 虚拟方法
		protected virtual object OnGet(string key, Paging page, Sorting[] sortings, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.Get(key, this.GetSchema(), page ?? Paging.Page(1), this.OptionsBuilder.Get(parameters), sortings);
		}

		protected virtual int OnDelete(string key, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return string.IsNullOrWhiteSpace(key) ? 0 : this.DataService.Delete(key, this.GetSchema(), this.OptionsBuilder.Delete(parameters));
		}

		protected virtual int OnCreate(string key, IEnumerable<TModel> data, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.InsertMany(key, data, this.GetSchema(), this.OptionsBuilder.Insert(parameters));
		}

		protected virtual int OnUpsert(string key, IEnumerable<TModel> data, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.UpsertMany(key, data, this.GetSchema(), this.OptionsBuilder.Upsert(parameters));
		}

		protected virtual int OnUpdate(string key, IEnumerable<TModel> data, IEnumerable<KeyValuePair<string, object>> parameters = null)
		{
			return this.DataService.UpdateMany(data, this.GetSchema(), this.OptionsBuilder.Update(parameters));
		}
		#endregion
	}
}