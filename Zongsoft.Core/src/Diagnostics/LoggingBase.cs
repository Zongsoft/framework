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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Diagnostics;

public abstract partial class LoggingBase<TLog> where TLog : ILog
{
	protected virtual string GetSource(Type type)
	{
		if(type == null)
			return null;

		var attribute = ApplicationModuleAttribute.Find(type);

		if(attribute == null || string.IsNullOrEmpty(attribute.Name) || attribute.Name == "_")
			return TypeAlias.GetAlias(type, true);
		else
			return $"{attribute.Name}:{TypeAlias.GetAlias(type, true)}";
	}

	protected virtual TLog CreateLog(LogLevel level, string message, object data, string source, string action) => this.CreateLog(level, message, null, data, source, action);
	protected virtual TLog CreateLog(LogLevel level, Exception exception, object data, string source, string action) => this.CreateLog(level, null, exception, data, source, action);
	protected abstract TLog CreateLog(LogLevel level, string message, Exception exception, object data, string source, string action);
}
