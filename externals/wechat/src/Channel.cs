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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Wechat
{
	public class Channel
	{
		#region 静态变量
		private static readonly System.Security.Cryptography.SHA1 SHA1 = System.Security.Cryptography.SHA1.Create();
		#endregion

		#region 构造函数
		public Channel(Applet applet, CredentialManager manager)
		{
			if(applet.IsEmpty)
				throw new ArgumentNullException(nameof(applet));

			this.Applet = applet;
			this.Manager = manager ?? throw new ArgumentNullException(nameof(manager));
		}
		#endregion

		#region 公共属性
		public Applet Applet { get; set; }
		public CredentialManager Manager { get; }
		#endregion

		#region 获取凭证
		public ValueTask<string> GetCredentialAsync(CancellationToken cancellation = default) => this.Manager.GetCredentialAsync(this.Applet, cancellation);
		#endregion

		#region 计算邮戳
		public byte[] Postmark(string url, out string nonce, out long timestamp, out TimeSpan period)
		{
			nonce = Zongsoft.Common.Randomizer.GenerateString(16);
			timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
			period = TimeSpan.Zero;

			if(string.IsNullOrEmpty(url))
				return null;

			var task = this.Manager.GetTicketAsync(this.Applet, "jsapi");
			var result = task.IsCompletedSuccessfully ? task.Result : task.ConfigureAwait(false).GetAwaiter().GetResult();

			if(string.IsNullOrEmpty(result.ticket))
				return null;

			period = result.period;

			var text = $"{nonce}&{timestamp}&{result.ticket}&{url}";
			return SHA1.ComputeHash(Encoding.UTF8.GetBytes(text));
		}
		#endregion
	}
}
