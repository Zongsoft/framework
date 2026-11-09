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

namespace Zongsoft.Services;

public interface IApplicationEnvironment
{
	string Name { get; }
	IDictionary<object, object> Properties { get; }

	public bool IsDevelopment => string.Equals(this.Name, "Development", StringComparison.OrdinalIgnoreCase);
	public bool IsProduction => string.Equals(this.Name, "Production", StringComparison.OrdinalIgnoreCase);
	public bool IsStaging => string.Equals(this.Name, "Staging", StringComparison.OrdinalIgnoreCase);
}