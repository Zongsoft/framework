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
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Wechat
{
	public class AccountCollection : KeyedCollection<string, Account>
	{
		#region 构造函数
		public AccountCollection(Account @default)
		{
			this.Default = @default;
		}

		internal AccountCollection(Options.AppOptionsCollection options) : base()
		{
			if(options != null && options.Count > 0)
			{
				var applets = Utility.GetOptions<Options.AppletOptionsCollection>($"/Externals/Wechat/Applets");
				var channels = Utility.GetOptions<Options.ChannelOptionsCollection>($"/Externals/Wechat/Channels");

				foreach(var option in options)
				{
					if(option.Type == AccountType.Applet)
						this.Add(applets.TryGetValue(option.Name, out var applet) ? Account.Applet(applet.Name, applet.Secret) : throw new WechatException($"The configured '{option.Name}' WeChat applet is not defined."));
					else
						this.Add(channels.TryGetValue(option.Name, out var channel) ? Account.Channel(channel.Name, channel.Secret) : throw new WechatException($"The configured '{option.Name}' WeChat channel is not defined."));
				}

				this.Default = options.Default != null && this.TryGetValue(options.Default, out var account) ? account : (this.Count > 0 ? this[0] : default);
			}
		}
		#endregion

		#region 公共属性
		public Account Default { get; }
		#endregion

		#region 公共方法
		public Account Get(string code) => string.IsNullOrEmpty(code) ? this.Default : this.TryGetValue(code, out var account) ? account : default;
		#endregion

		#region 重写方法
		protected override string GetKeyForItem(Account item) => item.Code;
		#endregion
	}
}
