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
using System.Linq;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Messaging.Options;

public class QueueSubscriptionOptions
{
	#region 公共属性
	public MessageReliability Reliability { get; set; }
	public MessageFallbackBehavior Fallback { get; set; }

	[Zongsoft.Configuration.ConfigurationProperty("")]
	public ICollection<QueueSubscriptionFilter> Filters { get; set; }
	#endregion
}

public class QueueSubscriptionFilter
{
	#region 构造函数
	public QueueSubscriptionFilter() { }
	public QueueSubscriptionFilter(string topic, string tags = null)
	{
		this.Topic = topic;
		this.Tags = tags;
	}
	#endregion

	#region 公共属性
	public string Topic { get; set; }

	[TypeConverter(typeof(TagsConverter))]
	public string Tags { get; set; }
	#endregion

	#region 重写方法
	public override string ToString()
	{
		var tags = this.Tags;
		return tags == null || tags.Length == 0 ? this.Topic : this.Topic + '?' + string.Join(',', tags);
	}
	#endregion

	#region 解析方法
	public static QueueSubscriptionFilter Parse(string text)
	{
		if(text != null && text.Length > 0)
		{
			var index = text.IndexOfAny([':', '?', '!', '|']);

			if(index > 0 && index < text.Length - 1)
			{
				var tags = text[(index + 1)..];
				return new QueueSubscriptionFilter(text[..index], tags);
			}
		}

		return new QueueSubscriptionFilter(text);
	}
	#endregion

	#region 嵌套子类
	private class TagsConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(string);
		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) => value is string text ? text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) : base.ConvertFrom(context, culture, value);
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) => value is string[] array ? string.Join(',', array) : base.ConvertTo(context, culture, value, destinationType);
	}
	#endregion
}
