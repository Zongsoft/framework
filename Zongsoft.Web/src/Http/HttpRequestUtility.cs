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
using System.Linq;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Zongsoft.Web.Http
{
    public static class HttpRequestUtility
    {
        public static IEnumerable<KeyValuePair<string, object>> GetParameters(this HttpRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var result = request.RouteValues
                .Concat(request.Query.Map())
                .Concat(request.Headers.Where(header => header.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase)).Map());

            if (request.HasFormContentType && request.Form != null && request.Form.Count > 0)
                result = result.Concat(request.Form.Map());

            return result;
        }

        private static IEnumerable<KeyValuePair<string, object>> Map(this IEnumerable<KeyValuePair<string, StringValues>> collection)
        {
            if (collection == null)
                return Array.Empty<KeyValuePair<string, object>>();

            return collection.Select(entry => new KeyValuePair<string, object>(entry.Key, entry.Value.Count switch
            {
                0 => null,
                1 => entry.Value.ToString(),
                _ => entry.Value.ToArray(),
            }));
        }
    }
}