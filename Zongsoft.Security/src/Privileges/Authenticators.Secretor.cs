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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data;
using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Configuration.Options;
using Zongsoft.Security.Privileges.Models;

namespace Zongsoft.Security.Privileges;

partial class Authenticators
{
	public class SecretorAuthenticator() : Authentication.SecretorAuthenticatorBase(Module.Current.Services)
	{
		#region 成员字段
		private Configuration.AuthorityOptions _options;
		#endregion

		#region 公共属性
		[Options("Security/Privileges/Authentication")]
		public Configuration.AuthorityOptions Options
		{
			get => _options ??= ApplicationContext.Current.Configuration.GetOption<Configuration.AuthorityOptions>("Security/Privileges/Authentication");
			set => _options = value;
		}
		#endregion

		#region 重写属性
		protected override IDataAccess Accessor => Module.Current.Accessor;
		#endregion

		#region 重写方法
		protected override async ValueTask<IUser> GetUserAsync(string identifier, CancellationToken cancellation)
		{
			var result = this.Accessor.SelectAsync<UserModel>(this.GetCriteria(identifier), cancellation);
			await using var enumerator = result.GetAsyncEnumerator(cancellation);
			return await enumerator.MoveNextAsync() ? enumerator.Current : null;
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