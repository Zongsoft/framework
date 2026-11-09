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
using System.Globalization;
using System.ComponentModel;

using Zongsoft.Services;

namespace Zongsoft.Messaging;

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
			var provider = services.Find<IMessageQueueProvider>(text[(index + 1)..]);
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
