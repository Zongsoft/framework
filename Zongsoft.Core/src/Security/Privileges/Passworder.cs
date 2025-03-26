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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Security.Privileges;

/// <summary>
/// 提供密码获取、设置、校验功能的基类。
/// </summary>
public abstract class Passworder
{
	#region 公共方法
	public abstract ValueTask<Cipher> GetAsync(Identifier identifier, CancellationToken cancellation);
	public abstract ValueTask<Cipher> GetAsync(string identity, string @namespace, CancellationToken cancellation);
	public abstract ValueTask<bool> SetAsync(Identifier identifier, Cipher cipher, CancellationToken cancellation);
	public abstract ValueTask<bool> VerifyAsync(string password, Cipher cipher, CancellationToken cancellation);

	public ValueTask<bool> SetAsync(Identifier identifier, string password, CancellationToken cancellation) => this.SetAsync(identifier, this.GetCipher(password), cancellation);
	#endregion

	#region 保护方法
	protected abstract Cipher GetCipher(string password, string algorithm = null);
	#endregion

	public class Cipher
	{
		#region 构造函数
		public Cipher() { }
		public Cipher(string name, byte[] value, byte[] nonce)
		{
			this.Name = name;
			this.Value = value;
			this.Nonce = nonce;
		}
		#endregion

		#region 公共属性
		/// <summary>获取密钥所有者标识，通常为用户标识。</summary>
		public virtual Identifier Identifier { get; }
		/// <summary>获取或设置密钥名称，通常为哈希算法。</summary>
		public string Name { get; set; }
		/// <summary>获取或设置密钥内容，通常为密码哈希值。</summary>
		public byte[] Value { get; set; }
		/// <summary>获取或设置密钥随机，通常为密码随机盐。</summary>
		public byte[] Nonce { get; set; }
		#endregion

		#region 公共方法
		public void Reset(string password, string algorithm = null)
		{
			if(string.IsNullOrEmpty(algorithm))
				algorithm = "SHA1";

			this.Name = algorithm;

			if(string.IsNullOrEmpty(password))
			{
				this.Value = null;
				this.Nonce = null;
			}
			else
			{
				this.Nonce = BitConverter.GetBytes(Random.Shared.NextInt64());
				this.Value = PasswordUtility.HashPassword(password, this.Nonce, algorithm);
			}
		}

		public void Reset(string password, byte[] nonce, string algorithm = null)
		{
			if(string.IsNullOrEmpty(algorithm))
				algorithm = "SHA1";

			this.Name = algorithm;

			if(string.IsNullOrEmpty(password))
			{
				this.Value = null;
				this.Nonce = null;
			}
			else
			{
				this.Nonce = nonce;
				this.Value = PasswordUtility.HashPassword(password, nonce, algorithm);
			}
		}
		#endregion
	}
}
