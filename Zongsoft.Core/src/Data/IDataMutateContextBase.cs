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

/// <summary>表示写入操作上下文的基础接口。</summary>
public interface IDataMutateContextBase : IDataAccessContextBase
{
	/// <summary>获取写入操作对应的实体。</summary>
	Metadata.IDataEntity Entity { get; }

	/// <summary>获取或设置写入操作的受影响记录数。</summary>
	int Count { get; set; }

	/// <summary>获取或设置写入操作的数据。</summary>
	object Data { get; set; }

	/// <summary>获取写入操作的选项。</summary>
	IDataMutateOptions Options { get; }

	/// <summary>获取当前操作独立的返回数据请求及结果，不改变调用方选项中的返回数据契约。</summary>
	/// <remarks>在执行前添加必需的列，操作成功后读取本次操作的结果。引擎不支持请求的列或种类时必须拒绝执行。</remarks>
	Returning Returning { get; }

	/// <summary>获取写入操作的验证器。</summary>
	IDataValidator Validator { get; }
}
