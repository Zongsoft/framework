﻿/*
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
using System.Linq;

using Zongsoft.Data;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Security.Membership
{
	public class SecretAuthenticator : SecretAuthenticatorBase
	{
		#region 构造函数
		public SecretAuthenticator() { }
		#endregion

		#region 公共属性
		[Options("Security/Membership/Authentication")]
		public Configuration.AuthorityOptions Options { get; set; }
		#endregion

		#region 重写方法
		protected override IUserModel GetUser(string identifier)
		{
			ICondition criteria = Utility.GetIdentityCondition(identifier);
			return Module.Current.Accessor.Select<IUserModel>(Mapping.Instance.User, criteria).FirstOrDefault();
		}

		protected override TimeSpan GetPeriod(string scenario)
		{
			var period = TimeSpan.Zero;

			//获取指定场景对应的凭证有效期
			if(this.Options != null && this.Options.Expiration.TryGetValue(scenario, out var option))
				period = option.Period;

			return period > TimeSpan.Zero ? period : TimeSpan.FromHours(4);
		}
		#endregion
	}
}
