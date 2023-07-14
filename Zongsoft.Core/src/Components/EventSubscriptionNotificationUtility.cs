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

namespace Zongsoft.Components
{
	public static class EventSubscriptionNotificationUtility
	{
		public static string GetNotifier(string notifier, out string channel)
		{
			if(string.IsNullOrEmpty(notifier))
			{
				channel = null;
				return null;
			}

			var index = notifier.IndexOf(':');

			switch(index)
			{
				case 0:
					throw new ArgumentException($"Illegal notifier parameter value format: '{notifier}'.", nameof(notifier));
				case < 0:
					channel = null;
					return notifier;
				case > 0:
					channel = index >= notifier.Length - 1 ? null : notifier[(index + 1)..].ToString();
					return notifier[..index].ToString();
			}
		}

		public static string ToString(this IEventSubscriptionNotification notification) => string.IsNullOrEmpty(notification.Channel) ?
			$"{notification.Notifier}:{notification.Template}({notification.Argument})->{notification.Destination}" :
			$"{notification.Notifier}.{notification.Channel}:{notification.Template}({notification.Argument})->{notification.Destination}";
	}
}