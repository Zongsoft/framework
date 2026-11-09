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

namespace Zongsoft.Externals.Aliyun.Pushing;

public class PushingResult
{
	#region 构造函数
	public PushingResult() { }
	public PushingResult(string code, string message)
	{
		this.Code = code;
		this.Message = message;
	}
	#endregion

	#region 公共属性
	public string RequestId { get; set; }
	public string MessageId { get; set; }
	public string Code { get; set; }
	public string Message { get; set; }
	public bool IsSucceed => string.IsNullOrWhiteSpace(this.Code);
	#endregion
}