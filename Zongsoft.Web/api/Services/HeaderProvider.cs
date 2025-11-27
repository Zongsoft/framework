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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Web.OpenApi library.
 *
 * The Zongsoft.Web.OpenApi is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web.OpenApi is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web.OpenApi library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net.Http;
using System.Collections.Generic;

namespace Zongsoft.Web.OpenApi.Services;

[Zongsoft.Services.Service<IHeaderProvider>]
public class HeaderProvider : IHeaderProvider
{
	#region 成员变量
	private Dictionary<HttpMethod, Dictionary<string, string>> _cache;
	#endregion

	#region 公共方法
	public IEnumerable<KeyValuePair<string, string>> GetHeaders(DocumentContext context, ControllerServiceDescriptor.ControllerOperationDescriptor operation, HttpMethod method)
	{
		if(_cache == null)
			Initialize(_cache = [], context.Configuration.GetHeaders());

		return _cache.TryGetValue(method, out var headers) ? headers : [];
	}
	#endregion

	#region 私有方法
	private static void Initialize(Dictionary<HttpMethod, Dictionary<string, string>> cache, IEnumerable<Configuration.HeaderOption> headers)
	{
		foreach(var header in headers)
		{
			if(string.IsNullOrEmpty(header.Method))
				continue;

			var methods = header.Method.Split([',', '|', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			for(int i = 0; i < methods.Length; i++)
			{
				var dictionary = Ensure(cache, HttpMethod.Parse(methods[i]));
				dictionary[header.Name] = header.Value;
			}
		}

		static Dictionary<string, string> Ensure(Dictionary<HttpMethod, Dictionary<string, string>> headers, HttpMethod method)
		{
			if(headers.TryGetValue(method, out var dictionary))
				return dictionary;

			if(headers.TryAdd(method, dictionary = []))
				return dictionary;

			return headers[method];
		}
	}
	#endregion
}
