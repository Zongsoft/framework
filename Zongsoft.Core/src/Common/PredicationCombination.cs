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

namespace Zongsoft.Common;

/// <summary>表示断言的逻辑组合方式。</summary>
public enum PredicationCombination
{
	/// <summary>表示如果某个断言返回成功，则不再执行后续断言测试而直接返回成功；如果返回失败，则进行后续断言测试。即整个断言链中所有断言测试均失败则断言链返回失败。</summary>
	Or,
	/// <summary>表示如果某个断言返回成功，则进行下一个断言测试，如果返回失败，则不再执行后续断言测试而直接返回失败。即整个断言链中所有断言测试均成功则断言链返回成功。</summary>
	And,
}
