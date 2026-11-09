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

namespace Zongsoft.Data.Common.Expressions;

public class StatementSlot
{
	public StatementSlot(string name, string place, object value)
	{
		this.Name = name;
		this.Place = place;
		this.Value = value;
	}

	public string Name { get; }
	public string Place { get; set; }
	public object Value { get; set; }

	public override string ToString() => string.IsNullOrEmpty(this.Place) ? this.Name : $"{this.Name}@{this.Place}";
}