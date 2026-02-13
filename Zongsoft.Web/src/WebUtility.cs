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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Data;
using Zongsoft.Web.Http;

namespace Zongsoft.Web;

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

		//设置响应的分页头
		controller.Response.Headers.SetPagination(paging);

		//如果数据是可分页类型则挂载其分页事件
		if(data is IPageable pageable)
		{
			pageable.Paginated += OnPaginated;
		}
		else
		{
			//如果启用了分页并且无结果，则返回一个空集且设置一个零长度的分页头（注：这项特性是因为前端兼容性而临时添加）
			if(paging != null && paging.IsPaged() && paging.IsEmpty)
			{
				controller.Response.Headers[Headers.Pagination] = $"{paging.Index}/{paging.Count}({paging.Total})";
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

	public static bool HasTextContentType(this HttpRequest request) => request != null && Microsoft.Net.Http.Headers.MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) && contentType.IsSubsetOf(TEXT);
	public static async Task<string> ReadAsStringAsync(this HttpRequest request, CancellationToken cancellation = default)
	{
		if(request == null)
			throw new ArgumentNullException(nameof(request));

		if(Microsoft.Net.Http.Headers.MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType) && contentType.IsSubsetOf(TEXT))
		{
			var encoding = contentType.Encoding ?? Encoding.UTF8;

			using(var reader = new StreamReader(
				request.Body,
				encoding,
				detectEncodingFromByteOrderMarks: true,
				bufferSize: 1024,
				leaveOpen: true))
			{
				#if NET7_0_OR_GREATER
				return await reader.ReadToEndAsync(cancellation);
				#else
				return await reader.ReadToEndAsync();
				#endif
			}
		}

		return null;
	}

	public static IReadOnlyDictionary<string, object> ToDictionary(this IFormCollection form)
	{
		if(form == null)
			return null;

		var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		foreach(var field in form)
		{
			if(field.Value.Count == 1)
				dictionary[field.Key] = field.Value[0];
			else if(field.Value.Count > 1)
				dictionary[field.Key] = field.Value.ToArray();
			else
				dictionary[field.Key] = field.Value.ToString();
		}

		foreach(var file in form.Files)
		{
			dictionary[file.Name ?? file.FileName] = file.OpenReadStream();
		}

		return dictionary;
	}
	#endregion
}
