/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Common
{
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

				unhandledExceptions ??= new List<Exception>();
				unhandledExceptions.Add(innerException);
			}

			if(unhandledExceptions != null)
				throw new AggregateException(exception.Message, unhandledExceptions);

			return null;
		}
	}
}