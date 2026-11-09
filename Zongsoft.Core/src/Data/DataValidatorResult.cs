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

namespace Zongsoft.Data;

/// <summary>表示字段验证器提供的替代值。</summary>
public struct DataValidatorResult
{
	/// <summary>指示是否提供替代值；为真时允许将字段明确设置为空。</summary>
	public bool HasValue;
	/// <summary>获取或设置替代值，也可以是延迟执行的 <see cref="IDataValueBinder"/>。</summary>
	public object Value;
}
