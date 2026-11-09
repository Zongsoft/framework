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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data.Archiving;

/// <summary>表示数据模板模型提供程序的接口。</summary>
public interface IDataTemplateModelProvider
{
	/// <summary>获取数据模板模型。</summary>
	/// <param name="template">指定数据模板。</param>
	/// <param name="argument">指定的获取参数。</param>
	/// <param name="cancellation">指定的异步取消标记。</param>
	/// <returns>如果获取成功则返回对应的数据模板模型对象，否则返回空(<c>null</c>)。</returns>
	ValueTask<IDataTemplateModel> GetModelAsync(IDataTemplate template, object argument, CancellationToken cancellation = default);
}
