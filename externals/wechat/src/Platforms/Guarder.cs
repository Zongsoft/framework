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
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Configuration.Options;
using Zongsoft.Externals.Wechat.Options;
using Zongsoft.Externals.Wechat.Platforms.Options;

namespace Zongsoft.Externals.Wechat.Platforms
{
	public class Guarder
	{
		#region 公共属性
		[Options("/Externals/Wechat/Platform")]
		public PlatformOptions Options { get; set; }

		public ICache Cache { get; set; }
		#endregion

		public bool Refresh(string appId)
		{
			return false;
		}

		public bool OnTicket(string appId, string ciphertext)
		{
			if(string.IsNullOrEmpty(ciphertext))
				return false;

			if(!this.Options.Apps.TryGetValue(appId, out var app))
				return false;

			var text = CryptographyUtility.Decrypt(ciphertext, app.Password, out _);
			var ticket = GetTicketValue(text, out _);

			if(string.IsNullOrEmpty(ticket))
				return false;

			this.Cache.SetValue($"Zongsoft.Wechat.Ticket:{appId}", ticket);
			return true;
		}

		private string GetTicketValue(string content, out string type)
		{
			type = null;

			if(string.IsNullOrEmpty(content))
				return null;

			var match = Regex.Match(content, @"<ComponentVerifyTicket>?<value>([^<>])</ComponentVerifyTicket>");
			if(match.Success)
				return match.Groups["value"].Value;

			return null;
		}
	}
}
