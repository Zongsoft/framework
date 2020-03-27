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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Zongsoft.Security.Web
{
	internal static class Utility
	{
		/// <summary>
		/// 确认指定的标识模式文本是否为编号（即一个整数）。
		/// </summary>
		/// <param name="pattern">待解析的标识模式文本。</param>
		/// <param name="identity">如果指定的<paramref name="pattern"/>参数值不是数字，则为其中的标识部分。</param>
		/// <param name="prefix">如果指定<paramref name="pattern"/>参数值不是数字，则为其中的前缀部分，前缀以冒号(:)分隔。</param>
		/// <param name="suffix">如果指定<paramref name="pattern"/>参数值不是数字，则为其中的后缀部分，后缀以叹号(!)分隔。</param>
		/// <returns>如果指定的<paramref name="pattern"/>参数值是数字则返回其对应的数值，否则返回零。</returns>
		public static uint ResolvePattern(string pattern, out string identity, out string prefix, out string suffix)
		{
			prefix = null;
			suffix = null;
			identity = null;

			if(string.IsNullOrWhiteSpace(pattern))
				return 0;

			if(uint.TryParse(pattern, out var id))
				return id;

			var prefixIndex = pattern.IndexOf(':');
			var suffixIndex = pattern.LastIndexOf('!');

			if(suffixIndex > 0 && suffixIndex > prefixIndex)
			{
				identity = pattern.Substring(prefixIndex + 1, suffixIndex - prefixIndex - 1);
				suffix = pattern.Substring(suffixIndex + 1);
			}
			else
			{
				identity = pattern.Substring(prefixIndex + 1);
			}

			if(prefixIndex > 0)
				prefix = pattern.Substring(0, prefixIndex);

			return 0;
		}

		public static string GetDataSchema(this HttpRequest request)
		{
			return GetHttpHeaderValue(request.Headers, "x-data-schema");
		}

		public static string GetHttpHeaderValue(this IHeaderDictionary headers, string name)
		{
			if(headers != null && headers.TryGetValue(name, out var value))
				return value;

			return null;
		}

		public static async Task<string> ReadAsStringAsync(this HttpRequest request)
		{
			if(request == null)
				throw new ArgumentNullException(nameof(request));

			MediaTypeHeaderValue mediaType;
			MediaTypeHeaderValue.TryParse(request.ContentType, out mediaType);

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
	}
}
