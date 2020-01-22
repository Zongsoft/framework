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
using System.Runtime.Serialization;

namespace Zongsoft.Security
{
	/// <summary>
	/// 安全凭证操作相关的异常。
	/// </summary>
	[Serializable]
	public class CredentialException : System.ApplicationException
	{
		#region 成员字段
		private string _credentialId;
		#endregion

		#region 构造函数
		public CredentialException() : base(Properties.Resources.Text_CredentialException_Message)
		{
		}

		public CredentialException(string message) : base(message ?? Properties.Resources.Text_CredentialException_Message)
		{
		}

		public CredentialException(string message, Exception innerException) : base(message ?? Properties.Resources.Text_CredentialException_Message, innerException)
		{
		}

		public CredentialException(string credentialId, string message) : base(message ?? Properties.Resources.Text_CredentialException_Message)
		{
			_credentialId = credentialId;
		}

		public CredentialException(string credentialId, string message, Exception innerException) : base(message ?? Properties.Resources.Text_CredentialException_Message, innerException)
		{
			_credentialId = credentialId;
		}

		protected CredentialException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
			_credentialId = info.GetString(nameof(CredentialId));
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取安全凭证号。
		/// </summary>
		public string CredentialId
		{
			get
			{
				return _credentialId;
			}
		}
		#endregion

		#region 重写方法
		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue(nameof(CredentialId), _credentialId);
		}
		#endregion
	}
}
