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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
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
