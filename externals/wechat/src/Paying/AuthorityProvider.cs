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
using System.Collections.Generic;

namespace Zongsoft.Externals.Wechat.Paying
{
	public class AuthorityProvider
	{
		private static readonly IDictionary<string, IAuthority> _authorities = new Dictionary<string, IAuthority>(StringComparer.OrdinalIgnoreCase);

		public static IAuthority GetAuthority(string name)
		{
			if(string.IsNullOrEmpty(name))
				return null;

			if(_authorities.TryGetValue(name, out var authority) && authority != null)
				return authority;

			lock(_authorities)
			{
				if(_authorities.TryGetValue(name, out authority))
					return authority;

				return _authorities.TryAdd(name, authority = CreateAuthority(name)) ? authority : _authorities[name];
			}
		}

		private static IAuthority CreateAuthority(string name)
		{
			var options = Utility.GetOptions<Options.AuthorityOptions>($"/Externals/Wechat/Paying/{name}");
			if(options == null)
				return null;

			var provider = new CertificateProvider(options.Directory);
			var certificate = provider.GetCertificate(options.Code);

			if(certificate == null)
				return null;

			if(certificate.Issuer == null || string.IsNullOrEmpty(certificate.Issuer.Identifier))
				certificate.Issuer = new CertificateIssuer(options.Code, options.Name);

			var app = options.Apps.GetDefault();
			return app == null ? null : new Authority(options.Name, options.Code, options.Secret, new Applet(app.Name, app.Secret), certificate);
		}
	}
}
