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
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat
{
	public class ChannelManager
	{
		public static Channel GetChannel(string key, IServiceProvider serviceProvider = null)
		{
			Account account = default;

			if(key != null && key.IndexOf(':') > 0)
			{
				if(serviceProvider == null)
					serviceProvider = ApplicationContext.Current.Services;

				if(serviceProvider != null)
					account = serviceProvider.ResolveRequired<IAccountProvider>().GetAccount(key);
			}
			else
			{
				var options = Utility.GetOptions<Options.AppOptionsCollection>($"/Externals/Wechat/Channels");

				if(options == null || options.Count == 0)
					return null;

				if(string.IsNullOrEmpty(key))
				{
					var app = options.GetDefault();

					if(app != null)
						account = new Account(app.Name, app.Secret);
				}
				else if(options.TryGetValue(key, out var app))
				{
					account = new Account(app.Name, app.Secret);
				}
			}

			return account.IsEmpty ? null : new Channel(account);
		}
	}
}
