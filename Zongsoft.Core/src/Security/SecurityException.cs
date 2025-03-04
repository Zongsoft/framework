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
using System.Runtime.CompilerServices;

namespace Zongsoft.Security
{
	[Serializable]
	public class SecurityException : Exception
	{
		#region 构造函数
		public SecurityException() => this.Reason = SecurityReasons.Unknown;
		public SecurityException(string message, Exception innerException = null) : this(SecurityReasons.Unknown, message, innerException) { }
		public SecurityException(string reason, string message, Exception innerException = null) : base(message, innerException)
		{
			this.Reason = reason;
			this.HasMessage = !string.IsNullOrEmpty(message);
		}
		#endregion

		#region 保护属性
		/// <summary>获取一个值，指示是否显式指定了异常消息文本。</summary>
		protected bool HasMessage { get; }
		#endregion

		#region 公共方法
		/// <summary>获取或设置异常理由的短语。</summary>
		public string Reason { get; set; }

		/// <summary>获取异常的消息文本。</summary>
		public override string Message => this.HasMessage ? base.Message : Resources.ResourceUtility.GetResourceString(this.GetType(), [$"Security.{this.Reason}.Message", $"{this.Reason}.Message"]);
		#endregion
	}
}
