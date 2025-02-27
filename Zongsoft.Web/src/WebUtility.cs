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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Data;
using Zongsoft.Web.Http;

namespace Zongsoft.Web
{
	public static class WebUtility
	{
		#region 常量定义
		private static readonly Microsoft.Net.Http.Headers.MediaTypeHeaderValue TEXT = new("text/*");
		#endregion

		#region 公共方法
		public static IActionResult Paginate(this ControllerBase controller, Paging paging, object data) => Paginate(controller, data, paging);
		public static IActionResult Paginate(this ControllerBase controller, object data, Paging paging)
		{
			ArgumentNullException.ThrowIfNull(controller);

			if(data == null)
				return new NoContentResult();

			if(data is IActionResult result)
				return result;

			//如果数据类型是值类型并且其值等于默认值，则返回HTTP状态为无内容
			if(data.GetType().IsValueType && object.Equals(data, Zongsoft.Common.TypeExtension.GetDefaultValue(data.GetType())))
				return new NoContentResult();

			//如果数据是可分页类型则挂载其分页事件
			if(data is IPageable pageable)
			{
				pageable.Paginated += OnPaginated;
			}
			else
			{
				//设置响应的分页头
				controller.Response.Headers.SetPagination(paging);

				//如果启用了分页并且结果集为空，则返回204(NoContent)
				if(paging != null && paging.Enabled && paging.IsEmpty)
				{
					controller.Response.Headers[Headers.Pagination] = $"{paging.PageIndex}/{paging.PageCount}({paging.TotalCount})";
					return new OkObjectResult(Array.Empty<object>());
				}
			}

			//返回数据
			return new OkObjectResult(data);

			void OnPaginated(object sender, PagingEventArgs args)
			{
				if(sender is IPageable pageable)
					pageable.Paginated -= OnPaginated;

				controller.Response.Headers.SetPagination(args.Paging);
			}
		}

		public static async Task<string> ReadAsStringAsync(this HttpRequest request)
		{
			if(request == null)
				throw new ArgumentNullException(nameof(request));

			var headers = request.GetTypedHeaders();
			var encoding = headers.ContentType?.Encoding ?? Encoding.UTF8;

			if(headers.ContentType == null || !headers.ContentType.IsSubsetOf(TEXT))
				return null;

			using(var reader = new StreamReader(
				request.Body,
				encoding,
				detectEncodingFromByteOrderMarks: true,
				bufferSize: 1024,
				leaveOpen: true))
			{
				return await reader.ReadToEndAsync();
			}
		}
		#endregion
	}
}
