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
using System.Collections.ObjectModel;

namespace Zongsoft.Externals.Wechat.Options;

/// <summary>微信支付商户配置；私钥与微信验签材料通过明确文件引用加载。</summary>
public class AuthorityOptions
{
	#region 公共属性
	public string Name { get; set; }
	public string Code { get; set; }
	public string ApiV2Key { get; set; }
	public string ApiV3Key { get; set; }
	public SigningOptions Signing { get; set; }
	public VerificationOptions Verification { get; set; }
	public AppOptionsCollection Apps { get; } = new();
	#endregion

	#region 嵌套类型
	public class SigningOptions
	{
		public string SerialNumber { get; set; }
		public string PrivateKeyFile { get; set; }
		public string CertificateFile { get; set; }
	}

	public class VerificationOptions
	{
		public VerificationMode Mode { get; set; }
		public string EncryptionKeyId { get; set; }
		public KeyOptionsCollection Keys { get; } = new();
	}

	public class KeyOptions
	{
		public string Name { get; set; }
		public string File { get; set; }
	}

	public class KeyOptionsCollection : KeyedCollection<string, KeyOptions>
	{
		public KeyOptionsCollection() : base(StringComparer.Ordinal) { }
		protected override string GetKeyForItem(KeyOptions item) => item.Name;
	}
	#endregion
}
