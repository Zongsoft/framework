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
 * This file is part of Zongsoft.Web.OpenApi library.
 *
 * The Zongsoft.Web.OpenApi is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Web.OpenApi is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Web.OpenApi library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Web.OpenApi.Configuration;

public class AuthenticationOption
{
	#region 构造函数
	public AuthenticationOption() => this.Authenticators = [];
	#endregion

	#region 公共属性
	public bool Persisted { get; set; }
	[Zongsoft.Configuration.ConfigurationProperty()]
	public AuthenticatorOptionCollection Authenticators { get; }
	#endregion

	#region 嵌套枚举
	public enum AuthenticatorKind
	{
		Http,
		Custom,
		OAuth2,
		OpenID,
	}

	public enum AuthenticatorLocation
	{
		Header,
		Cookie,
		Query,
		Path,
	}
	#endregion

	#region 嵌套子类
	[Zongsoft.Configuration.Configuration(nameof(Properties))]
	public class AuthenticatorOption
	{
		public AuthenticatorOption()
		{
			this.Kind = AuthenticatorKind.Http;
			this.Location = AuthenticatorLocation.Header;
			this.Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}

		public string Name { get; set; }
		public string Scheme { get; set; }
		public AuthenticatorKind Kind { get; set; }
		public AuthenticatorLocation Location { get; set; }
		public IDictionary<string, string> Properties { get; }
	}

	public sealed class AuthenticatorOptionCollection : KeyedCollection<string, AuthenticatorOption>
	{
		public AuthenticatorOptionCollection() : base(StringComparer.OrdinalIgnoreCase) { }
		protected override string GetKeyForItem(AuthenticatorOption option) => option.Name;
	}
	#endregion
}
