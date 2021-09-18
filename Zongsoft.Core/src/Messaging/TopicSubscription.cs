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

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示主题订阅的描述类。
	/// </summary>
	public class TopicSubscription
	{
		#region 成员字段
		private string _name;
		private string _tags;
		private string _contentType;
		private string _fallbackUrl;
		private MessageSubscriptionFallbackBehavior _fallbackBehavior;
		#endregion

		#region 构造函数
		public TopicSubscription()
		{
		}

		public TopicSubscription(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
		}
		#endregion

		/// <summary>
		/// 获取或设置订阅名称。
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if(string.IsNullOrEmpty(value))
					throw new ArgumentNullException();

				_name = value.Trim();
			}
		}

		/// <summary>
		/// 获取或设置订阅过滤标签。
		/// </summary>
		public string Tags
		{
			get
			{
				return _tags;
			}
			set
			{
				_tags = value;
			}
		}

		/// <summary>
		/// 获取或设置订阅对调的内容格式。
		/// </summary>
		public string ContentType
		{
			get
			{
				return _contentType;
			}
			set
			{
				_contentType = value;
			}
		}

		/// <summary>
		/// 获取或设置订阅的回调URL。
		/// </summary>
		public string FallbackUrl
		{
			get
			{
				return _fallbackUrl;
			}
			set
			{
				_fallbackUrl = value;
			}
		}

		/// <summary>
		/// 获取或设置订阅回调失败的重试策略。
		/// </summary>
		public MessageSubscriptionFallbackBehavior FallbackBehavior
		{
			get
			{
				return _fallbackBehavior;
			}
			set
			{
				_fallbackBehavior = value;
			}
		}
	}
}
