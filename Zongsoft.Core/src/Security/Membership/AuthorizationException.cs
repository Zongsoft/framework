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

namespace Zongsoft.Security.Membership
{
	/// <summary>
	/// 表示授权验证失败时引发的异常。
	/// </summary>
	[Serializable]
	public class AuthorizationException : SecurityException
	{
		#region 构造函数
		public AuthorizationException() : base(SecurityReasons.Forbidden, Properties.Resources.Text_AuthorizationException_Message)
		{
		}

		public AuthorizationException(string reason) : base(reason, Properties.Resources.Text_AuthorizationException_Message)
		{
		}

		public AuthorizationException(string reason, string message) : base(reason, message ?? Properties.Resources.Text_AuthorizationException_Message)
		{
		}

		public AuthorizationException(string reason, string message, Exception innerException) : base(reason, message ?? Properties.Resources.Text_AuthorizationException_Message, innerException)
		{
		}
		#endregion
	}
}
