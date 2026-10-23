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

public static class ChannelManager
{
	#region 静态字段
	private static readonly ConcurrentDictionary<string, Channel> _channels = new ConcurrentDictionary<string, Channel>();
	#endregion

	#region 公共属性
	public static IAccountProvider Provider { get; set; }
	#endregion

	#region 公共方法
	public static async ValueTask<Channel> GetChannelAsync(string code, CancellationToken cancellation = default)
	{
		var result = await FindChannelAsync(code ?? string.Empty, cancellation);
		return result ?? throw new WechatException(string.Format(Properties.Resources.Wechat_ChannelNotFound_Message, code ?? string.Empty));
	}

	public static ValueTask<Channel> GetChannelAsync(this Account account, CancellationToken cancellation = default)
	{
		if(account.IsEmpty)
			throw new ArgumentNullException(nameof(account));

		if(account.Type != AccountType.Channel)
			throw new ArgumentException(string.Format(Properties.Resources.Wechat_AccountNotChannel_Message, account.Code));

		return account.Component == null ? GetChannelAsync(account.Code, cancellation) : account.Component.GetChannelAsync(account.Code, cancellation);
	}

	public static async ValueTask<Channel> FindChannelAsync(string code, CancellationToken cancellation = default)
	{
		Initialize();

		if(_channels.TryGetValue(code, out var result))
			return result;

		var provider = Provider ?? Zongsoft.Services.ApplicationContext.Current?.Services.GetService(typeof(IAccountProvider)) as IAccountProvider;
		if(provider == null)
			return null;

		var account = await provider.GetAccountAsync(code, AccountType.Channel, cancellation);
		if(account.IsEmpty)
			return null;

		return account.Component == null ? _channels.GetOrAdd(code, _ => new Channel(account)) : new Channel(account);
	}

	public static ValueTask<Channel> FindChannelAsync(this Account account, CancellationToken cancellation = default) =>
		account.IsEmpty || account.Type != AccountType.Channel ? ValueTask.FromResult<Channel>(null) : account.Component == null ? FindChannelAsync(account.Code, cancellation) : account.Component.GetChannelAsync(account.Code, cancellation);
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

		var options = Utility.GetOptions<Options.ChannelOptionsCollection>($"/Externals/Wechat/Channels");
		if(options == null || options.Count == 0)
			return;

		foreach(var option in options)
		{
			_channels.TryAdd(option.Name, new Channel(Account.Channel(option.Name, option.Secret)));
		}

		//设置默认公众号
		var @default = options.GetDefault();
		if(@default != null && _channels.TryGetValue(@default.Name, out var applet))
			_channels.TryAdd(string.Empty, applet);
	}
	#endregion
}
