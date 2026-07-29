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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Data.Common;

public sealed class DataConnectionException : DataException
{
	internal DataConnectionException(
		IDataSource source,
		DateTimeOffset? retryAt,
		TimeSpan retryAfter,
		Exception innerException) : base(GetMessage(source, retryAt), innerException)
	{
		this.SourceName = source?.Name;
		this.DriverName = source?.Driver?.Name;
		this.RetryAt = retryAt;
		this.RetryAfter = retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.Zero;
	}

	public string SourceName { get; }
	public string DriverName { get; }
	public DateTimeOffset? RetryAt { get; }
	public TimeSpan RetryAfter { get; }

	private static string GetMessage(IDataSource source, DateTimeOffset? retryAt)
	{
		var message = $"The '{source?.Name}' data source is temporarily unavailable.";
		return retryAt.HasValue ? $"{message} Retry after {retryAt.Value:O}." : message;
	}
}
