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

using Microsoft.AspNetCore.Http;

namespace Zongsoft.Web.Http;

public static class HeaderDictionaryExtension
{
	public static string GetDataSchema(this IHeaderDictionary headers)
	{
		return headers.TryGetValue(Headers.DataSchema, out var value) ? (string)value : null;
	}

	/// <summary>设置分页信息头。</summary>
	/// <param name="headers">待设置的头集合。</param>
	/// <param name="paging">待设置的分页信息。</param>
	/// <returns>如果分页数大于零则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static bool SetPagination(this IHeaderDictionary headers, Data.Paging paging)
	{
		var result = paging != null && paging.Count > 0 && paging.IsPaged();

		if(result)
			headers[Headers.Pagination] = paging.ToString();
		else
			headers.Remove(Headers.Pagination);

		return result;
	}
}
