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

namespace Zongsoft.Security;

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
