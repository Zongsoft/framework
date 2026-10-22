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
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Zongsoft.Externals.Wechat;

public static class AppletManager
{
	#region 静态字段
	private static readonly ConcurrentDictionary<string, Applet> _applets = new ConcurrentDictionary<string, Applet>();
	#endregion

	#region 公共属性
	public static IAccountProvider Provider { get; set; }
	#endregion

	#region 公共方法
	public static async ValueTask<Applet> GetAppletAsync(string code, CancellationToken cancellation = default)
	{
		var result = await FindAppletAsync(code ?? string.Empty, cancellation);
		return result ?? throw new WechatException(string.Format(Properties.Resources.Wechat_AppletNotFound_Message, code ?? string.Empty));
	}

	public static ValueTask<Applet> GetAppletAsync(this Account account, CancellationToken cancellation = default)
	{
		if(account.IsEmpty)
			throw new ArgumentNullException(nameof(account));

		if(account.Type != AccountType.Applet)
			throw new ArgumentException(string.Format(Properties.Resources.Wechat_AccountNotApplet_Message, account.Code));

		return GetAppletAsync(account.Code, cancellation);
	}

	public static async ValueTask<Applet> FindAppletAsync(string code, CancellationToken cancellation = default)
	{
		Initialize();

		if(_applets.TryGetValue(code, out var result))
			return result;

		var provider = Provider;
		if(provider == null)
			return null;

		var account = await provider.GetAccountAsync(code, AccountType.Applet, cancellation);
		if(account.IsEmpty)
			return null;

		return _applets.GetOrAdd(code, _ => new Applet(account));
	}

	public static ValueTask<Applet> FindAppletAsync(this Account account, CancellationToken cancellation = default) =>
		account.IsEmpty || account.Type != AccountType.Applet ? ValueTask.FromResult<Applet>(null) : FindAppletAsync(account.Code, cancellation);
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
