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

namespace Zongsoft.Components;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class HandlerAttribute(string template, int order = 0) : Attribute
{
	public string Template { get; set; } = template;
	public int Order { get; set; } = order;

	public override string ToString() => this.Order == 0 ? this.Template : $"[{this.Order}]{this.Template}";
}
