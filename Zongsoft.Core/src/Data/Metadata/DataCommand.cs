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

namespace Zongsoft.Data.Metadata;

public class DataCommand : DataCommandBase<DataCommandScriptor>
{
	public DataCommand(string @namespace, string name, string alias = null) : this(@namespace, name, DataCommandMutability.None, alias) { }
	public DataCommand(string @namespace, string name, DataCommandMutability mutability, string alias = null) : base(@namespace, name, mutability, alias)
	{
		this.Scriptor = new DataCommandScriptor(this);
	}
}
