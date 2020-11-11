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
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

using Zongsoft.Data;

namespace Zongsoft.Web
{
	public static class WebUtility
	{
		#region 公共方法
		public static IActionResult Paginate(object data, IPageable pageable = null)
		{
			static string GetPaging(Paging paging)
			{
				if(paging == null || Paging.IsDisabled(paging))
					return null;

				return paging.PageIndex.ToString() + "/" +
				       paging.PageCount.ToString() + "(" +
				       paging.TotalCount.ToString() + ")";
			}

			if(data == null)
				return new NotFoundResult();

			if(data is Array array && array.Length == 0)
				return new NoContentResult();

			if(data is ICollection collection && collection.Count == 0)
				return new NoContentResult();

			//如果数据类型是值类型并且其值等于默认值，则返回HTTP状态为无内容
			if(data.GetType().IsValueType && object.Equals(data, default))
				return new NoContentResult();

			if(pageable == null)
				pageable = data as IPageable;

			if(pageable != null && !pageable.Suppressed)
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

				return new OkObjectResult(result);
			}

			return new OkObjectResult(data);
		}

		public static async Task<string> ReadAsStringAsync(this HttpRequest request)
		{
			if(request == null)
				throw new ArgumentNullException(nameof(request));

			MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue mediaType);

			var encoding = mediaType?.Encoding;
			if(encoding == null || encoding == Encoding.UTF7)
				encoding = Encoding.UTF8;

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

		#region 嵌套结构
		internal class ResultEntity
		{
			public ResultEntity(object data)
			{
				this.Data = data;
				this.Paging = null;
			}

			[System.Text.Json.Serialization.JsonPropertyName("$")]
			[Serialization.SerializationMember("$")]
			public object Data { get; set; }

			[System.Text.Json.Serialization.JsonPropertyName("$$")]
			[Serialization.SerializationMember("$$")]
			public IDictionary<string, string> Paging { get; set; }
		}
		#endregion
	}
}
