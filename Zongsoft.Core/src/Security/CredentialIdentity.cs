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
using System.Security.Principal;

namespace Zongsoft.Security
{
	/// <summary>
	/// 表示带凭证的用户标识类。
	/// </summary>
	[Obsolete]
	public class CredentialIdentity : IIdentity
	{
		#region 公共字段
		public static readonly CredentialIdentity Empty = new CredentialIdentity();
		#endregion

		#region 成员字段
		private string _credentialId;
		private Credential _credential;
		private ICredentialProvider _provider;
		#endregion

		#region 构造函数
		private CredentialIdentity()
		{
			_credentialId = string.Empty;
		}

		public CredentialIdentity(Credential credential)
		{
			_credential = credential ?? throw new ArgumentNullException(nameof(credential));
			_credentialId = credential.CredentialId;
		}

		public CredentialIdentity(string credentialId, ICredentialProvider provider)
		{
			if(string.IsNullOrWhiteSpace(credentialId))
				throw new ArgumentNullException(nameof(credentialId));

			_credentialId = credentialId;
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				var credential = this.Credential;

				if(credential == null || credential.User == null)
					return string.Empty;

				return credential.User.Name;
			}
		}

		public bool IsAuthenticated
		{
			get
			{
				if(string.IsNullOrEmpty(_credentialId))
					return false;

				//获取当前凭证对象
				var credential = this.Credential;

				//只有当凭证对象不为空并且对应的用户对象也不为空才算验证通过
				return credential != null && credential.User != null;
			}
		}

		public string CredentialId
		{
			get
			{
				return _credentialId;
			}
		}

		public virtual Credential Credential
		{
			get
			{
				if(_credential == null)
				{
					if(string.IsNullOrEmpty(_credentialId))
						return null;

					var provider = this.Provider;

					if(provider != null)
						_credential = provider.GetCredential(_credentialId);
				}

				return _credential;
			}
		}

		public ICredentialProvider Provider
		{
			get
			{
				return _provider;
			}
			set
			{
				_provider = value ?? throw new ArgumentNullException();
			}
		}
		#endregion

		#region 显式实现
		string IIdentity.AuthenticationType
		{
			get
			{
				return "Zongsoft Credentials Authentication";
			}
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return this.IsAuthenticated ?
				"Authenticated:" + this.CredentialId :
				"Not Authenticated";
		}
		#endregion
	}
}
