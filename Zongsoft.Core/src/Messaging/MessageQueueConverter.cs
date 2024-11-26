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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.ComponentModel;
using System.Globalization;

using Zongsoft.Services;

namespace Zongsoft.Messaging
{
	public class MessageQueueConverter : TypeConverter
	{
		#region 重写方法
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value is string text ? Resolve(text) : base.ConvertFrom(context, culture, value);
		#endregion

		#region 静态方法
		public static IMessageQueue Resolve(string text) => Resolve(ApplicationContext.Current?.Services, text);
		public static IMessageQueue Resolve(IServiceProvider services, string text)
		{
			if(services == null || string.IsNullOrEmpty(text))
				return null;

			var index = text.IndexOf('@');

			if(index > 0 && index < text.Length - 1)
			{
				var provider = services.Resolve<IMessageQueueProvider>(text[(index + 1)..]);
				if(provider == null)
					return null;

				var name = text[..index];
				return provider.Exists(name) ? provider.Queue(name) : null;
			}

			foreach(var provider in services.ResolveAll<IMessageQueueProvider>())
			{
				if(provider.Exists(text))
					return provider.Queue(text);
			}

			return services.Resolve(text) as IMessageQueue;
		}
		#endregion
	}
}
