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
	public class ApiControllerBase<TModel, TConditional, TService> : ApiControllerBase<TModel, TService>
	                                                                  where TModel : class
	                                                                  where TConditional : class, IModel
	                                                                  where TService : class, IDataService<TModel>
	{
		#region 构造函数
		protected ApiControllerBase(IServiceProvider serviceProvider) : base(serviceProvider)
		{
		}
		#endregion

		#region 公共方法
		[HttpPost]
		public virtual IActionResult Count([FromBody]TConditional conditional)
		{
			if(conditional == null)
				return this.Content(this.DataService.Count(null).ToString(), "text/plain");
			else
				return this.Content(this.DataService.Count(Conditional.ToCondition(conditional)).ToString(), "text/plain");
		}

		[HttpPost]
		public virtual IActionResult Exists([FromBody]TConditional conditional)
		{
			bool existed;

			if(conditional == null)
				existed = this.DataService.Exists(null);
			else
				existed = this.DataService.Exists(Conditional.ToCondition(conditional));

			if(existed)
				return this.Ok();
			else
				return this.NotFound();
		}

		[HttpPost]
		public virtual IActionResult Query([FromBody]TConditional conditional, [FromQuery]Paging paging = null)
		{
			return this.Paginate(this.DataService.Select(Conditional.ToCondition(conditional), Http.Headers.HeaderDictionaryExtension.GetDataSchema(this.Request.Headers), paging));
		}
		#endregion
	}
}