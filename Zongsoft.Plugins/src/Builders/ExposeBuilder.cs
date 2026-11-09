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

namespace Zongsoft.Plugins.Builders;

/// <summary>构件暴露创建器。</summary>
/// <remarks>其目标对象不追加到所有者，但仍通过继承的 <see cref="IAppender"/> 接收子节点。</remarks>
public class ExposeBuilder : ObjectBuilder
{
	public override object Build(BuilderContext context)
	{
		context.AppendToOwner = false;
		return base.Build(context);
	}
}
