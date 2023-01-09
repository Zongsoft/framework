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
using System.Linq;
using System.Globalization;
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Messaging.Options
{
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
				var index = text.IndexOfAny(new[] { ':', '?', '!', '|' });

				if(index > 0 && index < text.Length - 1)
				{
					var tags = text.Substring(index + 1);
					return new QueueSubscriptionFilter(text.Substring(0, index), tags);
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
}
