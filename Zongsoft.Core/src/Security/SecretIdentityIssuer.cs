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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Security.Claims;

using Zongsoft.Services;

namespace Zongsoft.Security
{
	[Service(typeof(IIdentityIssuer), typeof(IIdentityIssuer<string>))]
	public class SecretIdentityIssuer : IIdentityIssuer<string>
	{
		public string Name { get => "Secret"; }

		ClaimsIdentity IIdentityIssuer.Issue(object data, TimeSpan period, IDictionary<string, object> parameters)
		{
			return this.Issue(SecretIdentityUtility.GetTicket(data), period, parameters);
		}

		public ClaimsIdentity Issue(string data, TimeSpan period, IDictionary<string, object> parameters)
		{
			if(string.IsNullOrEmpty(data))
				return null;

			var identity = new ClaimsIdentity(this.Name);

			if(data.Contains('@'))
				identity.AddClaim(ClaimTypes.Email, data, ClaimValueTypes.Email, this.Name);
			else
				identity.AddClaim(ClaimTypes.MobilePhone, data, ClaimValueTypes.String, this.Name);

			if(period > TimeSpan.Zero)
				identity.AddClaim(new Claim(ClaimTypes.Expiration, period.ToString(), period.TotalHours > 24 ? ClaimValueTypes.YearMonthDuration : ClaimValueTypes.DaytimeDuration, this.Name, this.Name, identity));

			if(parameters != null && parameters.Count > 0)
			{
				foreach(var parameter in parameters)
				{
					if(parameter.Value != null)
						identity.AddClaim(new Claim(ClaimTypes.UserData + "#" + parameter.Key, parameter.Value.ToString(), ClaimValueTypes.String));
				}
			}

			return identity;
		}
	}
}