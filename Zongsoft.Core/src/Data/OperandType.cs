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

namespace Zongsoft.Data;

/// <summary>表示操作元类型的枚举。</summary>
public enum OperandType
{
	/// <summary>加号</summary>
	Add,
	/// <summary>减号</summary>
	Subtract,
	/// <summary>乘号(*)</summary>
	Multiply,
	/// <summary>除号(/)</summary>
	Divide,
	/// <summary>取模(%)</summary>
	Modulo,

	/// <summary>负号</summary>
	Negate,
	/// <summary>逻辑非/位取反</summary>
	Not,

	/// <summary>逻辑与/位与</summary>
	And,
	/// <summary>逻辑或/位或</summary>
	Or,
	/// <summary>位异或</summary>
	Xor,

	/// <summary>字段</summary>
	Field,
	/// <summary>常量</summary>
	Constant,
	/// <summary>函数</summary>
	Function,
}
