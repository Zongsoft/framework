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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Externals.Wechat
{
	/// <summary>
	/// 提供微信后台接口的URL定义。
	/// </summary>
	internal static class Urls
	{
		#region 常量定义
		public const string Host = "api.weixin.qq.com";
		public const string Scheme = "https";
		#endregion

		public static readonly Uri BaseAddress = new Uri(Uri.UriSchemeHttps + "://api.weixin.qq.com");

		#region 公共方法
		public static string GetTicketUrl(string token, string type = null)
		{
			if(string.IsNullOrEmpty(type))
				type = "jsapi";

			return $"{Scheme}://{Host}/cgi-bin/ticket/getticket?access_token={token}&type={type}";
		}

		public static string GetAccessToken(string grantType, string appId, string secret)
		{
			if(string.IsNullOrEmpty(grantType))
				grantType = "client_credential";

			return $"{Scheme}://{Host}/cgi-bin/token?grant_type={grantType}&appid={appId}&secret={secret}";
		}

		public static string GetUrl(string path, string appId, string appSecret, string[] names, object[] values)
		{
			if(names == null || names.Length == 0)
				return GetUrl(path, new[] { "appId", "secret" }, new string[] { appId, appSecret });

			Array.Resize(ref names, names.Length + 2);
			Array.Resize(ref values, values.Length + 2);

			names[^2] = "appId";
			names[^1] = "secret";
			values[^2] = appId;
			values[^1] = appSecret;

			return GetUrl(path, names, values);
		}

		public static string GetUrl(string path, string[] names, object[] values)
		{
			var url = Scheme + "://" + Host + "/" + path;

			if(names == null || names.Length == 0)
				return url;

			var queryString = string.Empty;

			for(int i=0; i<names.Length; i++)
			{
				if(string.IsNullOrWhiteSpace(names[i]))
					continue;

				if(queryString != null && queryString.Length > 0)
					queryString += "&";

				queryString += names[i];
				queryString += "=";

				if(i < values.Length && values[i] != null)
					queryString += values[i].ToString();
			}

			return url + "?" + queryString;
		}
		#endregion
	}
}
