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
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;

namespace Zongsoft.Externals.Wechat
{
	public class Channel
	{
		#region 静态变量
		private static readonly System.Security.Cryptography.SHA1 SHA1 = System.Security.Cryptography.SHA1.Create();
		#endregion

		#region 成员字段
		private volatile UserProvider _users;
		private volatile ChannelMessager _messager;
		private volatile ChannelAuthentication _authentication;
		#endregion

		#region 构造函数
		public Channel(Account account)
		{
			if(account.IsEmpty)
				throw new ArgumentNullException(nameof(account));

			this.Account = account;
		}
		#endregion

		#region 公共属性
		public Account Account { get; }

		public UserProvider Users
		{
			get
			{
				if(_users == null)
					Interlocked.CompareExchange(ref _users, new UserProvider(this.Account), null);

				return _users;
			}
		}

		public ChannelMessager Messager
		{
			get
			{
				if(_messager == null)
					Interlocked.CompareExchange(ref _messager, new ChannelMessager(this.Account), null);

				return _messager;
			}
		}

		public ChannelAuthentication Authentication
		{
			get
			{
				if(_authentication == null)
					Interlocked.CompareExchange(ref _authentication, new ChannelAuthentication(this.Account), null);

				return _authentication;
			}
		}
		#endregion

		#region 获取凭证
		public ValueTask<string> GetCredentialAsync(bool refresh, CancellationToken cancellation = default) => CredentialManager.GetCredentialAsync(this.Account, refresh, cancellation);
		#endregion

		#region 计算邮戳
		public async ValueTask<(byte[] data, string nonce, long timestamp, TimeSpan period)> PostmarkAsync(string url, CancellationToken cancellation = default)
		{
			var result = await CredentialManager.GetTicketAsync(this.Account, "jsapi", false, cancellation);

			if(string.IsNullOrEmpty(result.ticket))
				return default;

			var nonce = Randomizer.GenerateString(16);
			var timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
			var period = result.period;

			var text = $"jsapi_ticket={result.ticket}&noncestr={nonce}&timestamp={timestamp}&url={url}";
			return (SHA1.ComputeHash(Encoding.UTF8.GetBytes(text)), nonce, timestamp, period);
		}
		#endregion
	}
}
