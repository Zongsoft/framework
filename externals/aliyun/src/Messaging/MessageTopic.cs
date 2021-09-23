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
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Messaging;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	public class MessageTopic : IMessageTopic<MessageTopic>, IDisposable
	{
		#region 常量定义
		private readonly string MESSAGE_SEND_URL;

		private const string MESSAGE_CONTENT_NOTAG_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?><Message xmlns=""http://mns.aliyuncs.com/doc/v1/""><MessageBody>{0}</MessageBody><MessageAttributes>{1}</MessageAttributes></Message>";
		private const string MESSAGE_CONTENT_FULLY_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?><Message xmlns=""http://mns.aliyuncs.com/doc/v1/""><MessageBody>{0}</MessageBody><MessageTag>{1}</MessageTag><MessageAttributes>{2}</MessageAttributes></Message>";
		#endregion

		private HttpClient _http;

		public MessageTopic(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name;

			//初始化相关操作的URL常量
			MESSAGE_SEND_URL = MessageTopicUtility.GetRequestUrl(name, "messages");

			var certificate = MessageTopicUtility.GetCertificate(name);
			_http = new HttpClient(new HttpClientHandler(certificate, MessageAuthenticator.Instance));
			_http.DefaultRequestHeaders.Add("x-mns-version", "2015-06-06");
		}

		public string Name { get; }

		public bool Handle(ref MessageTopic message)
		{
			throw new NotImplementedException();
		}

		public Task<bool> HandleAsync(ref MessageTopic message, CancellationToken cancellation = default)
		{
			throw new NotImplementedException();
		}

		public bool Subscribe(string topic, string tags, MessageTopicSubscriptionOptions options = null)
		{
			throw new NotSupportedException();
		}

		public Task<bool> SubscribeAsync(string topic, string tags, MessageTopicSubscriptionOptions options = null)
		{
			throw new NotSupportedException();
		}

		public string Publish(ReadOnlySpan<byte> data, string topic, string tags, MessageTopicPublishOptions options = null)
		{
			var response = _http.Send(new HttpRequestMessage(HttpMethod.Post, MESSAGE_SEND_URL)
			{
				Content = CreateMessageRequest(data, tags),
			});
			var content = response.Content.ReadAsStream();
			return MessageUtility.GetMessageResponseId(content);
		}

		public Task<string> PublishAsync(ReadOnlySpan<byte> data, string topic, string tags, MessageTopicPublishOptions options = null, CancellationToken cancellation = default)
		{
			return this.PublishAsync(data.ToArray(), 0, data.Length, topic, tags, options, cancellation);
		}

		public async Task<string> PublishAsync(byte[] data, int offset, int count, string topic, string tags, MessageTopicPublishOptions options = null, CancellationToken cancellation = default)
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

			var content = await response.Content.ReadAsStreamAsync(cancellation);
			return MessageUtility.GetMessageResponseId(content);
		}

		#region 私有方法
		private static HttpContent CreateMessageRequest(ReadOnlySpan<byte> data, string tags)
		{
			var content = System.Convert.ToBase64String(data);

			if(string.IsNullOrWhiteSpace(tags))
				content = string.Format(MESSAGE_CONTENT_NOTAG_TEMPLATE, content);
			else
				content = string.Format(MESSAGE_CONTENT_FULLY_TEMPLATE, content, tags);

			return new StringContent(content, Encoding.UTF8, "application/xml");
		}

		private static HttpContent CreateMessageRequest(byte[] data, int offset, int count, string tags)
		{
			var content = System.Convert.ToBase64String(data, offset, count);

			if(string.IsNullOrWhiteSpace(tags))
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
