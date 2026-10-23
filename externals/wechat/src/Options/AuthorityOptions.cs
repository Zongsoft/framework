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
