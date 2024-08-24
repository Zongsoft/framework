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
using System.IO;

using Zongsoft.Configuration;
using Zongsoft.Externals.Wechat.Options;

namespace Zongsoft.Externals.Wechat.Options
{
	/// <summary>
	/// 表示微信支付账户的选项类。
	/// </summary>
	public class AuthorityOptions
	{
		#region 构造函数
		public AuthorityOptions()
		{
			this.Apps = new AppOptionsCollection();
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置账户名称。</summary>
		public string Name { get; set; }

		/// <summary>获取或设置账户号码。</summary>
		public string Code { get; set; }

		/// <summary>获取或设置账户密钥。</summary>
		[ConfigurationProperty("secret")]
		public string Secret { get; set; }

		/// <summary>获取或设置证书文件的存放目录。</summary>
		public DirectoryInfo Directory { get; set; }

		/// <summary>获取微信小程序应用设置选项集。</summary>
		public AppOptionsCollection Apps { get; }
		#endregion
	}
}
