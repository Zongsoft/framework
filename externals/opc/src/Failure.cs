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
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
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
		new((int)status.Code, global::Opc.Ua.StatusCodes.GetBrowseName(status.Code), status.ToString());
	#endregion

	#region 重写方法
	public override string ToString() => $"[{this.Code}]{this.Reason}\n{this.Message}";
	#endregion
}
