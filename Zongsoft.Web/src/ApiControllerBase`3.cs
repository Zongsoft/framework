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

using Microsoft.AspNetCore.Mvc;

using Zongsoft.Data;

namespace Zongsoft.Web
{
	public class ApiControllerBase<TModel, TCriteria, TService> : ApiControllerBase<TModel, TService>
	                                                                  where TCriteria : class, IModel
	                                                                  where TService : class, IDataService<TModel>
	{
		#region 构造函数
		protected ApiControllerBase(IServiceProvider serviceProvider) : base(serviceProvider) { }
		#endregion

		#region 公共方法
		[HttpPost("Count")]
		public virtual IActionResult Count([FromBody]TCriteria criteria)
		{
			if(criteria == null)
				return this.Content(this.DataService.Count().ToString(), "text/plain");
			else
				return this.Content(this.DataService.Count(Criteria.Transform(criteria), null, null).ToString(), "text/plain");
		}

		[HttpPost("Exists")]
		public virtual IActionResult Exists([FromBody]TCriteria criteria)
		{
			bool existed;

			if(criteria == null)
				existed = this.DataService.Exists((ICondition)null);
			else
				existed = this.DataService.Exists(Criteria.Transform(criteria), null);

			if(existed)
				return this.Ok();
			else
				return this.NotFound();
		}

		[HttpPost("Query")]
		public virtual IActionResult Query([FromBody]TCriteria criteria, [FromQuery]Paging page = null, [FromQuery(Name = "sorting")][ModelBinder(typeof(Binders.SortingBinder))]Sorting[] sortings = null)
		{
			return this.Paginate(this.DataService.Select(Criteria.Transform(criteria), Http.Headers.HeaderDictionaryExtension.GetDataSchema(this.Request.Headers), page ?? Paging.Page(1), sortings));
		}
		#endregion
	}
}