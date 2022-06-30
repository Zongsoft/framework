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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Messaging;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	public class MessageTopic : IMessageTopic<MessageTopicMessage>, IDisposable
	{
		#region 常量定义
		private readonly string MESSAGE_SEND_URL;

		private const string MESSAGE_CONTENT_NOTAG_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?><Message xmlns=""http://mns.aliyuncs.com/doc/v1/""><MessageBody>{0}</MessageBody><MessageAttributes>{1}</MessageAttributes></Message>";
		private const string MESSAGE_CONTENT_FULLY_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?><Message xmlns=""http://mns.aliyuncs.com/doc/v1/""><MessageBody>{0}</MessageBody><MessageTag>{1}</MessageTag><MessageAttributes>{2}</MessageAttributes></Message>";
		#endregion

		#region 成员字段
		private HttpClient _http;
		#endregion

		#region 构造函数
		public MessageTopic(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name.Trim();

			//初始化相关操作的URL常量
			MESSAGE_SEND_URL = MessageTopicUtility.GetRequestUrl(name, "messages");

			var certificate = MessageTopicUtility.GetCertificate(name);
			_http = new HttpClient(new HttpClientHandler(certificate, MessageAuthenticator.Instance));
			_http.DefaultRequestHeaders.Add("x-mns-version", "2015-06-06");
		}
		#endregion

		#region 公共属性
		public string Name { get; }
		public IHandler<MessageTopicMessage> Handler { get; set; }
		public IConnectionSetting ConnectionSetting { get; set; }
		IEnumerable<IMessageSubscriber> IMessageTopic.Subscribers => Array.Empty<IMessageSubscriber>();
		#endregion

		#region 公共方法
		public ValueTask<OperationResult> HandleAsync(ref MessageTopicMessage message, CancellationToken cancellation = default)
		{
			return this.Handler?.HandleAsync(message, cancellation) ?? ValueTask.FromResult(OperationResult.Fail());
		}
		#endregion

		#region 公共方法
		public ValueTask<bool> SubscribeAsync(string topic, IEnumerable<string> tags, MessageTopicSubscriptionOptions options = null)
		{
			throw new NotSupportedException();
		}

		public ValueTask<string> PublishAsync(ReadOnlyMemory<byte> data, string topic, IEnumerable<string> tags, MessageTopicPublishOptions options = null, CancellationToken cancellation = default)
		{
			return this.PublishAsync(data.ToArray(), 0, data.Length, topic, tags, options, cancellation);
		}

		public async ValueTask<string> PublishAsync(byte[] data, int offset, int count, string topic, IEnumerable<string> tags, MessageTopicPublishOptions options = null, CancellationToken cancellation = default)
		{
			if(data == null || data.Length == 0)
				return null;

			if(offset < 0 || offset > data.Length - 1)
				throw new ArgumentOutOfRangeException(nameof(offset));

			if(count < 1)
				count = data.Length - offset;
			else
			{
				if(offset + count > data.Length)
					throw new ArgumentOutOfRangeException(nameof(count));
			}

			var response = await _http.PostAsync(MESSAGE_SEND_URL, CreateMessageRequest(data, offset, count, tags), cancellation);

			if(cancellation.IsCancellationRequested)
				return null;

			return MessageUtility.GetMessageResponseId(await response.Content.ReadAsStreamAsync(cancellation));
		}
		#endregion

		#region 私有方法
		private static HttpContent CreateMessageRequest(ReadOnlySpan<byte> data, IEnumerable<string> tags)
		{
			var content = System.Convert.ToBase64String(data);

			if(tags == null || !tags.Any())
				content = string.Format(MESSAGE_CONTENT_NOTAG_TEMPLATE, content);
			else
				content = string.Format(MESSAGE_CONTENT_FULLY_TEMPLATE, content, tags);

			return new StringContent(content, Encoding.UTF8, "application/xml");
		}

		private static HttpContent CreateMessageRequest(byte[] data, int offset, int count, IEnumerable<string> tags)
		{
			var content = System.Convert.ToBase64String(data, offset, count);

			if(tags == null || !tags.Any())
				content = string.Format(MESSAGE_CONTENT_NOTAG_TEMPLATE, content);
			else
				content = string.Format(MESSAGE_CONTENT_FULLY_TEMPLATE, content, tags);

			return new StringContent(content, Encoding.UTF8, "application/xml");
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var http = Interlocked.Exchange(ref _http, null);

			if(http != null)
				http.Dispose();
		}
		#endregion
	}
}
