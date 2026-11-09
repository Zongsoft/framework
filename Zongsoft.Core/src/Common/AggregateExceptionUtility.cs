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

namespace Zongsoft.Common;

public static class AggregateExceptionUtility
{
	public static object Handle<TException>(this AggregateException exception, Func<TException, object> handler) where TException : Exception
	{
		if(exception == null)
			return null;

		if(handler == null)
			throw new ArgumentNullException(nameof(handler));

		List<Exception> unhandledExceptions = null;

		foreach(var innerException in exception.Flatten().InnerExceptions)
		{
			if(innerException is TException expected)
				return handler(expected);

			unhandledExceptions ??= [];
			unhandledExceptions.Add(innerException);
		}

		if(unhandledExceptions != null)
			throw new AggregateException(exception.Message, unhandledExceptions);

		return null;
	}
}