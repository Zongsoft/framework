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

namespace Zongsoft.Externals.Opc;

public readonly struct Failure(int code, string reason, string message = null)
{
	#region 公共字段
	public readonly int Code = code;
	public readonly string Reason = reason;
	public readonly string Message = message;
	#endregion

	#region 静态方法
	internal static Failure GetFailure(global::Opc.Ua.StatusCode status) =>
		new((int)status.Code, status.SymbolicId, status.ToString());
	#endregion

	#region 重写方法
	public override string ToString() => string.IsNullOrEmpty(this.Message) ?
		$"[{this.Code}]{this.Reason}" :
		$"[{this.Code}]{this.Reason}\n{this.Message}";
	#endregion
}
