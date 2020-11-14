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

using Zongsoft.Options;
using Zongsoft.Options.Configuration;

namespace Zongsoft.Externals.Wechat.Options.Configuration
{
	public class AppElement : OptionConfigurationElement, IAppSetting
	{
		#region 常量定义
		private const string XML_ID_ATTRIBUTE = "id";
		private const string XML_SECRET_ATTRIBUTE = "secret";
		private const string XML_TOKEN_ATTRIBUTE = "token";
		private const string XML_KEY_ATTRIBUTE = "key";
		#endregion

		#region 公共属性
		[OptionConfigurationProperty(XML_ID_ATTRIBUTE, OptionConfigurationPropertyBehavior.IsKey)]
		public string Id
		{
			get => (string)this[XML_ID_ATTRIBUTE];
			set => this[XML_ID_ATTRIBUTE] = value;
		}

		[OptionConfigurationProperty(XML_SECRET_ATTRIBUTE, OptionConfigurationPropertyBehavior.IsRequired)]
		public string Secret
		{
			get => (string)this[XML_SECRET_ATTRIBUTE];
			set => this[XML_SECRET_ATTRIBUTE] = value;
		}

		[OptionConfigurationProperty(XML_TOKEN_ATTRIBUTE)]
		public string Token
		{
			get => (string)this[XML_TOKEN_ATTRIBUTE];
			set => this[XML_TOKEN_ATTRIBUTE] = value;
		}

		[OptionConfigurationProperty(XML_KEY_ATTRIBUTE)]
		public string Key
		{
			get => (string)this[XML_KEY_ATTRIBUTE];
			set => this[XML_KEY_ATTRIBUTE] = value;
		}
		#endregion
	}
}
