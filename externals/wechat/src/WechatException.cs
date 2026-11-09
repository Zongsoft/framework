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

namespace Zongsoft.Externals.Wechat;

public class WechatException : ApplicationException
{
	#region 构造函数
	public WechatException(string message) : base(message) { }
	public WechatException(string message, Exception innerException) : base(message, innerException) { }
	public WechatException(int code, string message) : base(message) => this.Code = code;
	public WechatException(int code, string message, Exception innerException) : base(message, innerException) => this.Code = code;
	#endregion

	#region 公共属性
	public int Code { get; }
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(this.Code == 0)
			return base.ToString();
		else
			return $"[{this.Code}] {base.ToString()}";
	}
	#endregion
}
