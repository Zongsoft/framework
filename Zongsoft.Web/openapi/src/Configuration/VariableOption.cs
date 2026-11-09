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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Web.OpenApi.Configuration;

public class VariableOption
{
	public string Name { get; set; }
	public string Default { get; set; }
	public string Description { get; set; }

	[Zongsoft.Configuration.ConfigurationProperty("Value")]
	public IList<string> Values { get; set; }
}

public class VariableOptionCollection : KeyedCollection<string, VariableOption>
{
	public VariableOptionCollection(params IEnumerable<VariableOption> variables) : base(StringComparer.OrdinalIgnoreCase)
	{
		if(variables != null)
		{
			foreach(var variable in variables)
				this.Add(variable);
		}
	}

	protected override string GetKeyForItem(VariableOption variable) => variable.Name;
}