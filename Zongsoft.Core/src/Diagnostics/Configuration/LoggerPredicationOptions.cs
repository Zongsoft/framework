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

namespace Zongsoft.Diagnostics.Configuration;

public class LoggerPredicationOptions
{
	public LogLevel? MinLevel { get; set; }
	public LogLevel? MaxLevel { get; set; }
	public IList<SourceOptions> Sources { get; set; }
	public IList<ExceptionOptions> Exceptions { get; set; }

	public class SourceOptions
	{
		public string Pattern { get; set; }
	}

	public class ExceptionOptions
	{
		public string Type { get; set; }

		public Type GetExceptionType()
		{
			if(string.IsNullOrEmpty(this.Type))
				return null;

			return Zongsoft.Common.TypeAlias.TryParse(this.Type, out var type) ? type : null;
		}
	}
}
