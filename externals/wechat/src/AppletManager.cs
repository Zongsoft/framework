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
using System.Threading;
using System.Collections.Concurrent;

namespace Zongsoft.Externals.Wechat
{
	public static class AppletManager
	{
		#region 静态字段
		private static readonly ConcurrentDictionary<string, Applet> _applets = new ConcurrentDictionary<string, Applet>();
		#endregion

		#region 公共属性
		public static IAccountProvider Provider { get; set; }
		#endregion

		#region 公共方法
		public static Applet GetApplet(string code)
		{
			Initialize();

			return _applets.GetOrAdd(code ?? string.Empty, key =>
			{
				var provider = Provider;
				if(provider == null)
					throw new WechatException($"The specified '{key}' WeChat applet does not exist.");

				return new Applet(provider.GetAccount(key, AccountType.Applet));
			});
		}

		public static Applet GetApplet(this Account account)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			if(account.Type != AccountType.Applet)
				throw new ArgumentException($"The specified '{account.Code}' account is not a WeChat applet.");

			return GetApplet(account.Code);
		}

		public static bool TryGetApplet(string code, out Applet result)
		{
			Initialize();

			if(_applets.TryGetValue(code, out result))
				return true;

			var provider = Provider;

			if(provider != null)
			{
				var account = provider.GetAccount(code, AccountType.Applet);

				lock(_applets)
				{
					if(_applets.TryGetValue(code, out result))
						return true;

					result = _applets[code] = new Applet(account);
				}
			}

			return result != null;
		}

		public static bool TryGetApplet(this Account account, out Applet result)
		{
			result = null;

			if(account.IsEmpty || account.Type != AccountType.Applet)
				return false;

			return TryGetApplet(account.Code, out result);
		}
		#endregion

		#region 私有方法
		private volatile static int _initialized;
		private static void Initialize()
		{
			if(_initialized != 0)
				return;

			var initialized = Interlocked.CompareExchange(ref _initialized, 1, 0);
			if(initialized == 1)
				return;

			var options = Utility.GetOptions<Options.AppletOptionsCollection>($"/Externals/Wechat/Applets");
			if(options == null || options.Count == 0)
				return;

			foreach(var option in options)
			{
				_applets.TryAdd(option.Name, new Applet(Account.Applet(option.Name, option.Secret)));
			}

			//设置默认小程序
			var @default = options.GetDefault();
			if(@default != null && _applets.TryGetValue(@default.Name, out var applet))
				_applets.TryAdd(string.Empty, applet);
		}
		#endregion
	}
}
