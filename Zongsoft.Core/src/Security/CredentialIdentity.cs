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
using System.IO;
using System.Security.Claims;

namespace Zongsoft.Security;

public class CredentialIdentity : ClaimsIdentity
{
	#region 成员字段
	private readonly string _name;
	#endregion

	#region 构造函数
	public CredentialIdentity(string name, string authenticationType, string issuer, string originalIssuer = null) : base(authenticationType)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		_name = name.Trim();

		//尝试设置身份签发者的Claim
		if(!string.IsNullOrEmpty(issuer))
			base.AddClaim(new Claim(ClaimTypes.System, issuer, ClaimValueTypes.String, issuer, originalIssuer, this));

		//设置身份名称对应的Claim
		base.AddClaim(new Claim(base.NameClaimType, _name, ClaimValueTypes.String, issuer, originalIssuer, this));
	}

	public CredentialIdentity(BinaryReader reader) : base(reader)
	{
		_name = base.Name;
	}
	#endregion

	#region 重写属性
	public override string Name => _name;
	public override bool IsAuthenticated => _name != null && _name.Length > 0;
	#endregion
}
