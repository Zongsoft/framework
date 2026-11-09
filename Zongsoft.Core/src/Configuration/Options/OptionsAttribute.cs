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

namespace Zongsoft.Configuration.Options;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class OptionsAttribute : Attribute
{
	public OptionsAttribute(string path)
	{
		if(string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		this.Name = string.Empty;
		this.Path = path.Trim();
	}

	public OptionsAttribute(string name, string path)
	{
		if(string.IsNullOrEmpty(path))
			throw new ArgumentNullException(nameof(path));

		this.Name = name ?? string.Empty;
		this.Path = path.Trim();
	}

	public string Name { get; }
	public string Path { get; }
}
