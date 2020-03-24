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
 * This file is part of Zongsoft.Security library.
 *
 * The Zongsoft.Security is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Zongsoft.Options;
using Zongsoft.Options.Configuration;

namespace Zongsoft.Security.Membership.Options.Configuration
{
	public class UserOption : OptionConfigurationElement, IUserOption
	{
		#region 常量定义
		private const string XML_VERIFICATION_ATTRIBUTE = "verification";
		private const string XML_PASSWORDLENGTH_ATTRIBUTE = "passwordLength";
		private const string XML_PASSWORDSTRENGTH_ATTRIBUTE = "passwordStrength";
		#endregion

		#region 公共属性
		[OptionConfigurationProperty(XML_PASSWORDLENGTH_ATTRIBUTE, DefaultValue = 6)]
		public int PasswordLength
		{
			get => (int)this[XML_PASSWORDLENGTH_ATTRIBUTE];
			set => this[XML_PASSWORDLENGTH_ATTRIBUTE] = value;
		}

		[OptionConfigurationProperty(XML_PASSWORDSTRENGTH_ATTRIBUTE, DefaultValue = PasswordStrength.None)]
		public PasswordStrength PasswordStrength
		{
			get => (PasswordStrength)this[XML_PASSWORDSTRENGTH_ATTRIBUTE];
			set => this[XML_PASSWORDSTRENGTH_ATTRIBUTE] = value;
		}

		[OptionConfigurationProperty(XML_VERIFICATION_ATTRIBUTE, DefaultValue = UserVerification.None)]
		public UserVerification Verification
		{
			get => (UserVerification)this[XML_VERIFICATION_ATTRIBUTE];
			set => this[XML_VERIFICATION_ATTRIBUTE] = value;
		}
		#endregion
	}
}
