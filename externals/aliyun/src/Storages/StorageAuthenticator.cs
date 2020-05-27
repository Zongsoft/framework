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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Zongsoft.Externals.Aliyun.Storages
{
	internal class StorageAuthenticator : HttpAuthenticator
	{
		#region 单例字段
		public static StorageAuthenticator Instance = new StorageAuthenticator("OSS");
		#endregion

		#region 常量定义
		private static readonly HashSet<string> AVAILABLE_RESOURCES = new HashSet<string>(new string[] { 
			"acl", "uploadId", "partNumber", "uploads", "cors", "logging",
			"website", "delete", "referer", "lifecycle", "security-token",
			"response-cache-control", "response-content-disposition", "response-content-encoding",
			"response-content-type", "response-content-language", "response-expires" }, StringComparer.OrdinalIgnoreCase);
		#endregion

		#region 私有构造
		private StorageAuthenticator(string name) : base(name, HttpSignatureMode.Header)
		{
		}
		#endregion

		#region 重写方法
		protected override bool IsCanonicalizedHeader(string name)
		{
			return name.StartsWith(Storages.StorageHeaders.OSS_PREFIX);
		}

		protected override string CanonicalizeResource(HttpRequestMessage request)
		{
			var parts = request.RequestUri.Host.Split(".".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			var path = string.Empty;

			if(parts.Length > 3)
				path = "/" + string.Join(".", parts, 0, parts.Length - 3);

			path += request.RequestUri.LocalPath;

			if(!string.IsNullOrWhiteSpace(request.RequestUri.Query))
			{
				parts = request.RequestUri.Query.Trim('?', '&', ' ', '\t').Split("&".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

				if(parts.Length > 0)
				{
					var count = 0;
					Array.Sort(parts, QueryStringComparer.Ordinal);

					for(int i = 0; i < parts.Length; i++)
					{
						var index = parts[i].IndexOf('=');
						var name = index > 0 ? parts[i].Substring(0, index) : parts[i];

						if(AVAILABLE_RESOURCES.Contains(name))
							path += (count++ == 0 ? "?" : "&") + parts[i];
					}
				}
			}

			return path;
		}
		#endregion
	}
}
