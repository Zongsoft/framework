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
		var message = string.Format(Properties.Resources.DataConnectionException_Unavailable_Message, source?.Name);

		return retryAt.HasValue ?
			string.Format(Properties.Resources.DataConnectionException_RetryAfter_Message, message, retryAt.Value.ToLocalTime()) :
			message;
	}
}
