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

namespace Zongsoft.Externals.Wechat.Options
{
	/// <summary>
	/// 表示微信开放平台的第三方应用设置选项类。
	/// </summary>
	public class AppOptions
	{
		#region 公共属性
		/// <summary>获取或设置第三方平台应用标识，即微信开放平台的<c>component_appid</c>。</summary>
		public string Name { get; set; }

		/// <summary>获取或设置第三方平台应用口令，即微信开放平台的<c>component_appsecret</c>。</summary>
		public string Secret { get; set; }

		/// <summary>获取或设置第三方平台应用标记，即微信开放平台的“消息校验Token”。</summary>
		public string Nonce { get; set; }

		/// <summary>获取或设置第三方平台应用对称加解密的密钥，即微信开放平台的<c>symmetric_key</c>或<c>AESEncodingKey</c>。</summary>
		public string Key { get; set; }
		#endregion
	}
}
