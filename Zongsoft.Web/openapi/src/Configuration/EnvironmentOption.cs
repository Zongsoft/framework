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

public class EnvironmentOption
{
	public EnvironmentOption() => this.Variables = [];
	public EnvironmentOption(string name, params IEnumerable<VariableOption> variables)
	{
		this.Name = name;
		this.Variables = new VariableOptionCollection(variables);
	}

	public string Name { get; set; }
	public string Description { get; set; }

	[Zongsoft.Configuration.ConfigurationProperty()]
	public VariableOptionCollection Variables { get; }
}

public class EnvironmentOptionCollection() : KeyedCollection<string, EnvironmentOption>(StringComparer.OrdinalIgnoreCase)
{
	protected override string GetKeyForItem(EnvironmentOption server) => server.Name;
}