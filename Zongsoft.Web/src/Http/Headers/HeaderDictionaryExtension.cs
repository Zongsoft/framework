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
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Http.Extensions;

namespace Zongsoft.Web.Http.Headers
{
	public static class HeaderDictionaryExtension
	{
		private static readonly string X_Schema_Header = "x-data-schema";
		private static readonly string X_Paging_Header = "x-data-paging";

		public static string GetDataSchema(this IHeaderDictionary headers)
		{
			return headers.TryGetValue(X_Schema_Header, out var value) ? (string)value : null;
		}

		public static void SetDataPaging(this IHeaderDictionary headers, Zongsoft.Data.Paging paging)
		{
			if(paging == null || Zongsoft.Data.Paging.IsDisabled(paging))
				headers.Remove(X_Paging_Header);
			else
				headers[X_Paging_Header] =
				    paging.PageIndex.ToString() + "/" +
				    paging.PageCount.ToString() + "(" +
				    paging.TotalCount.ToString() + ")";
		}
	}
}
