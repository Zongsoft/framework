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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

using Zongsoft.Web;
using Zongsoft.Web.Http;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Web.Controllers;

partial class RoleController
{
	[ControllerName("Privileges")]
	[Authorize(Roles = $"{IRole.Administrators},{IRole.Security}")]
	public class PrivilegeController : ControllerBase
	{
		#region 公共属性
		public IPrivilegeService Service => Authorization.Servicer.Privileges;
		#endregion

		#region 公共方法
		[HttpGet("/[area]/{id}/[controller]")]
		public IAsyncEnumerable<IPrivilege> Get(string id, CancellationToken cancellation = default)
		{
			return this.Service.GetPrivilegesAsync(new Identifier(typeof(IRole), id), new Parameters(this.Request.GetParameters()), cancellation);
		}

		[ActionName("Filtering")]
		[HttpGet("/[area]/{id}/[controller]/[action]")]
		public IAsyncEnumerable<IPrivilege> GetFiltering(string id, CancellationToken cancellation = default)
		{
			var filtering = this.Service.Filtering ?? throw new BadHttpRequestException(null, StatusCodes.Status501NotImplemented);
			return filtering.GetPrivilegesAsync(new Identifier(typeof(IRole), id), new Parameters(this.Request.GetParameters()), cancellation);
		}

		[HttpPut("/[area]/{id}/[controller]")]
		public async Task<IActionResult> Set(string id, [FromQuery]bool reset = false, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			var parameters = new Parameters(this.Request.GetParameters());
			var modelType = Utility.GetModelType(this.Service, typeof(IPrivilege), typeof(IPrivilegeService<>));
			if(modelType == null)
				return this.StatusCode(StatusCodes.Status501NotImplemented);

			var privileges = (IEnumerable)await Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, modelType.MakeArrayType(), cancellation);
			var count = await this.Service.SetPrivilegesAsync(new Identifier(typeof(IRole), id), privileges.OfType<IPrivilege>(), reset, parameters, cancellation);
			return count > 0 ? this.Content(count.ToString()) : this.NoContent();
		}

		[ActionName("Filtering")]
		[HttpPut("/[area]/{id}/[controller]/[action]")]
		public async Task<IActionResult> SetFiltering(string id, [FromQuery]bool reset = false, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(id))
				return this.BadRequest();

			var filtering = this.Service.Filtering;
			if(filtering == null)
				return this.StatusCode(StatusCodes.Status501NotImplemented);

			var parameters = new Parameters(this.Request.GetParameters());
			var modelType = Utility.GetModelType(filtering, typeof(IPrivilege), typeof(IPrivilegeService<>));
			if(modelType == null)
				return this.StatusCode(StatusCodes.Status501NotImplemented);

			var privileges = (IEnumerable)await Serialization.Serializer.Json.DeserializeAsync(this.Request.Body, modelType.MakeArrayType(), cancellation);
			var count = await filtering.SetPrivilegesAsync(new Identifier(typeof(IRole), id), privileges.OfType<IPrivilege>(), reset, parameters, cancellation);
			return count > 0 ? this.Content(count.ToString()) : this.NoContent();
		}
		#endregion
	}
}
