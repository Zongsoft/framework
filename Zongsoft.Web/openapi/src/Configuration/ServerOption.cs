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

public class ServerOption
{
	public ServerOption() => this.Variables = [];
	public ServerOption(string name, string url, params IEnumerable<VariableOption> variables)
	{
		this.Name = name;
		this.Url = url;
		this.Variables = new VariableOptionCollection(variables);
	}

	public string Name { get; set; }
	public string Url { get; set; }

	[Zongsoft.Configuration.ConfigurationProperty()]
	public VariableOptionCollection Variables { get; }
}

public class ServerOptionCollection() : KeyedCollection<string, ServerOption>(StringComparer.OrdinalIgnoreCase)
{
	protected override string GetKeyForItem(ServerOption server) => server.Name;
}