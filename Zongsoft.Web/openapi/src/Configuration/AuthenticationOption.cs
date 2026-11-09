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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
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
