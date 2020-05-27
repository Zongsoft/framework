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
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

using Zongsoft.Messaging;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	public class Topic : Zongsoft.Messaging.ITopic
	{
		#region 常量定义
		private readonly string MESSAGE_SEND_URL;

		private const string MESSAGE_CONTENT_NOTAG_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?><Message xmlns=""http://mns.aliyuncs.com/doc/v1/""><MessageBody>{0}</MessageBody><MessageAttributes>{1}</MessageAttributes></Message>";
		private const string MESSAGE_CONTENT_FULLY_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?><Message xmlns=""http://mns.aliyuncs.com/doc/v1/""><MessageBody>{0}</MessageBody><MessageTag>{1}</MessageTag><MessageAttributes>{2}</MessageAttributes></Message>";
		#endregion

		#region 成员字段
		private string _name;
		private TopicProvider _provider;
		private TopicInfo _info;
		private HttpClient _http;
		private TopicSubscription _subscription;
		#endregion

		#region 构造函数
		public Topic(TopicProvider provider, string name, TopicInfo info, HttpClient http = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
			_info = info;

			_http = http ?? provider.GetHttpClient(_name);

			//初始化相关操作的URL常量
			MESSAGE_SEND_URL = provider.GetRequestUrl(_name, "messages");
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				return _name;
			}
		}
		#endregion

		#region 公共方法
		public TopicInfo GetInfo(bool refresh = false)
		{
			if(refresh || _info == null)
			{
				var response = _http.GetAsync(MESSAGE_SEND_URL);

				if(response.Result.IsSuccessStatusCode)
					_info = MessageUtility.ResolveTopicInfo(response.Result.Content.ReadAsStreamAsync().Result);
				else
					_info = null;
			}

			return _info;
		}

		public ITopicSubscription GetSubscription()
		{
			if(_subscription == null)
			{
				lock(this)
				{
					if(_subscription == null)
						_subscription = new TopicSubscription(this);
				}
			}

			return _subscription;
		}

		public object Send(byte[] data, string tags, object state = null)
		{
			if(data == null || data.Length == 0)
				return null;

			return this.Send(data, 0, data.Length, tags, state);
		}

		public object Send(byte[] data, int offset, int count, string tags, object state = null)
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

			var response = _http.PostAsync(MESSAGE_SEND_URL, this.CreateMessageRequest(data, offset, count, tags, state));

			return this.GetMessageResponseId(response.Result.Content.ReadAsStreamAsync().Result);
		}

		public object Send(Stream stream, string tags, object state = null)
		{
			if(stream == null)
				return null;

			var response = _http.PostAsync(MESSAGE_SEND_URL, this.CreateMessageRequest(stream, tags, state));

			return this.GetMessageResponseId(response.Result.Content.ReadAsStreamAsync().Result);
		}

		public Task<object> SendAsync(byte[] data, string tags, object state = null)
		{
			return this.SendAsync(data, 0, 0, tags, state);
		}

		public async Task<object> SendAsync(byte[] data, int offset, int count, string tags, object state = null)
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

			var response = await _http.PostAsync(MESSAGE_SEND_URL, this.CreateMessageRequest(data, offset, count, tags, state));
			var content = await response.Content.ReadAsStreamAsync();
			return Task.FromResult(this.GetMessageResponseId(content));
		}

		public async Task<object> SendAsync(Stream stream, string tags, object state = null)
		{
			if(stream == null)
				return null;

			var response = await _http.PostAsync(MESSAGE_SEND_URL, this.CreateMessageRequest(stream, tags, state));
			var content = await response.Content.ReadAsStreamAsync();
			return this.GetMessageResponseId(content);
		}
		#endregion

		#region 虚拟方法
		protected virtual string GenerateStateEntity(object state)
		{
			if(state == null)
				return null;

			if(state is TopicEmailState)
			{
				return string.Format("<DirectMail>{0}</DirectMail>", Zongsoft.Runtime.Serialization.Serializer.Json.Serialize(state));
			}
			else if(state is TopicSmsState)
			{
				return string.Format("<DirectSMS>{0}</DirectSMS>", Zongsoft.Runtime.Serialization.Serializer.Json.Serialize(state));
			}

			return null;
		}
		#endregion

		#region 私有方法
		private HttpContent CreateMessageRequest(byte[] data, int offset, int count, string tags, object state)
		{
			var content = System.Convert.ToBase64String(data, offset, count);

			if(string.IsNullOrWhiteSpace(tags))
				content = string.Format(MESSAGE_CONTENT_NOTAG_TEMPLATE, content, this.GenerateStateEntity(state));
			else
				content = string.Format(MESSAGE_CONTENT_FULLY_TEMPLATE, content, tags, this.GenerateStateEntity(state));

			return new StringContent(content, Encoding.UTF8, "application/xml");
		}

		public HttpContent CreateMessageRequest(Stream stream, string tags, object state)
		{
			var buffer = new byte[stream.Length - stream.Position];
			stream.Read(buffer, 0, buffer.Length);
			var content = System.Convert.ToBase64String(buffer);

			if(string.IsNullOrWhiteSpace(tags))
				content = string.Format(MESSAGE_CONTENT_NOTAG_TEMPLATE, content, this.GenerateStateEntity(state));
			else
				content = string.Format(MESSAGE_CONTENT_FULLY_TEMPLATE, content, tags, this.GenerateStateEntity(state));

			return new StringContent(content, Encoding.UTF8, "application/xml");
		}

		private string GetMessageResponseId(Stream stream)
		{
			if(stream == null)
				return null;

			var settings = new XmlReaderSettings()
			{
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true,
			};

			using(var reader = XmlReader.Create(stream, settings))
			{
				if(reader.MoveToContent() != XmlNodeType.Element)
					return null;

				while(reader.Read())
				{
					if(reader.NodeType != XmlNodeType.Element)
						continue;

					switch(reader.LocalName)
					{
						case "MessageId":
							return reader.Value;
					}
				}
			}

			return null;
		}
		#endregion
	}
}
