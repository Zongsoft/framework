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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	public class MessageQueue : Zongsoft.Messaging.MessageQueueBase
	{
		#region 常量定义
		private static readonly Regex COUNT_REGEX = new Regex(@"\<(?'tag'(ActiveMessages|InactiveMessages|DelayMessages))\>\s*(?<value>[^<>\s]+)\s*\<\/\k'tag'\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
		#endregion

		#region 成员字段
		private readonly HttpClient _http;
		private readonly MessageQueueProvider _provider;
		#endregion

		#region 构造函数
		internal MessageQueue(MessageQueueProvider provider, string name) : base(name)
		{
			_provider = provider ?? throw new ArgumentNullException(nameof(provider));
			_http = new HttpClient(new HttpClientHandler(_provider.GetCertificate(name), MessageQueueAuthenticator.Instance));
		}
		#endregion

		#region 内部属性
		internal HttpClient Http
		{
			get => _http;
		}
		#endregion

		#region 重写方法
		public override long GetCount()
		{
			return this.GetCountAsync().GetAwaiter().GetResult();
		}

		public override async Task<long> GetCountAsync()
		{
			var response = await _http.GetAsync(this.GetRequestUrl());

			if(!response.IsSuccessStatusCode)
				return -1;

			var content = await response.Content.ReadAsStringAsync();

			if(string.IsNullOrWhiteSpace(content))
				return -1;

			var matches = COUNT_REGEX.Matches(content);

			if(matches == null || matches.Count < 1)
				return -1;

			long total = 0;

			foreach(Match match in matches)
			{
				if(match.Success)
					total += Zongsoft.Common.Convert.ConvertValue<long>(match.Groups["value"].Value, 0);
			}

			return total;
		}

		public override async Task EnqueueManyAsync<T>(IEnumerable<T> items, Zongsoft.Messaging.MessageEnqueueSettings settings = null, CancellationToken cancellation = default)
		{
			if(items == null)
				return;

			var count = 0;

			foreach(var item in items)
			{
				var content = this.SerializeContent(item);

				if(content == null)
					continue;

				byte priority = 8;
				TimeSpan? duration = null;

				if(settings != null)
				{
					priority = settings.Priority;
					duration = settings.DelayTimeout;
				}

				if(duration.HasValue && duration.Value.TotalDays > 7)
					throw new ArgumentOutOfRangeException("settings", "The duration must less than 7 days.");

				var text = @"<Message xmlns=""http://mqs.aliyuncs.com/doc/v1/""><MessageBody>" +
					System.Convert.ToBase64String(content) +
					"</MessageBody><DelaySeconds>" +
					(duration.HasValue ? duration.Value.TotalSeconds.ToString() : "0") +
					"</DelaySeconds><Priority>" + priority.ToString() + "</Priority></Message>";

				var request = new HttpRequestMessage(HttpMethod.Post, this.GetRequestUrl("messages"));
				request.Content = new StringContent(text, Encoding.UTF8, "text/xml");
				request.Headers.Add("x-mns-version", "2015-06-06");

				var response = await _http.SendAsync(request);

				if(!response.IsSuccessStatusCode)
				{
					Zongsoft.Diagnostics.Logger.Warn("[" + response.StatusCode + "] The message enqueue failed." + Environment.NewLine + await response.Content.ReadAsStringAsync());
					return;
				}

				count++;
			}

			return;
		}

		protected override void OnEnqueue(object item, Zongsoft.Messaging.MessageEnqueueSettings settings)
		{
			this.OnEnqueueAsync(item, settings).GetAwaiter().GetResult();
		}

		protected override async Task OnEnqueueAsync(object item, Zongsoft.Messaging.MessageEnqueueSettings settings, CancellationToken cancellation = default)
		{
			var content = this.SerializeContent(item);

			if(content == null)
				return;

			byte priority = 8;
			TimeSpan? duration = null;

			if(settings != null)
			{
				priority = settings.Priority;
				duration = settings.DelayTimeout;
			}

			if(duration.HasValue && duration.Value.TotalDays > 7)
				throw new ArgumentOutOfRangeException("settings", "The duration must less than 7 days.");

			var text = @"<Message xmlns=""http://mqs.aliyuncs.com/doc/v1/""><MessageBody>" +
				System.Convert.ToBase64String(content) +
				"</MessageBody><DelaySeconds>" +
				(duration.HasValue ? duration.Value.TotalSeconds.ToString() : "0") +
				"</DelaySeconds><Priority>" + priority.ToString() + "</Priority></Message>";

			var request = new HttpRequestMessage(HttpMethod.Post, this.GetRequestUrl("messages"))
			{
				Content = new StringContent(text, Encoding.UTF8, "text/xml")
			};
			request.Headers.Add("x-mns-version", "2015-06-06");

			var response = await _http.SendAsync(request);

			if(!response.IsSuccessStatusCode)
			{
				Zongsoft.Diagnostics.Logger.Warn("[" + response.StatusCode + "] The message enqueue failed." + Environment.NewLine + await response.Content.ReadAsStringAsync());
				return;
			}
		}

		protected override Zongsoft.Messaging.MessageBase OnDequeue(Zongsoft.Messaging.MessageDequeueSettings settings)
		{
			if(settings == null)
				return Utility.ExecuteTask(() => this.DequeueOrPeekAsync(0));
			else
				return Utility.ExecuteTask(() => this.DequeueOrPeekAsync((int)settings.PollingTimeout.TotalSeconds));
		}

		protected override Task<Zongsoft.Messaging.MessageBase> OnDequeueAsync(Zongsoft.Messaging.MessageDequeueSettings settings, CancellationToken cancellation)
		{
			if(settings == null)
				return this.DequeueOrPeekAsync(0);
			else
				return this.DequeueOrPeekAsync((int)settings.PollingTimeout.TotalSeconds);
		}

		public override Zongsoft.Messaging.MessageBase Peek()
		{
			return this.DequeueOrPeekAsync(-1).GetAwaiter().GetResult();
		}

		public override Task<Zongsoft.Messaging.MessageBase> PeekAsync(CancellationToken cancellation = default)
		{
			return this.DequeueOrPeekAsync(-1);
		}

		private async Task<Zongsoft.Messaging.MessageBase> DequeueOrPeekAsync(int waitSeconds)
		{
			var request = new HttpRequestMessage(HttpMethod.Get, this.GetRequestUrl("messages") + (waitSeconds >= 0 ? "?waitseconds=" + waitSeconds.ToString() : "?peekonly=true"));
			request.Headers.Add("x-mns-version", "2015-06-06");
			var response = await _http.SendAsync(request);

			if(response.IsSuccessStatusCode)
				return MessageUtility.ResolveMessage(this, await response.Content.ReadAsStreamAsync());

			var exception = AliyunException.Parse(await response.Content.ReadAsStringAsync());

			if(exception != null)
			{
				switch(exception.Code)
				{
					case MessageUtility.MessageNotExist:
						return null;
					case MessageUtility.QueueNotExist:
						throw exception;
					default:
						throw exception;
				}
			}

			return null;
		}
		#endregion

		#region 虚拟方法
		protected virtual byte[] SerializeContent(object item)
		{
			if(item == null)
				return null;

			if(item.GetType() == typeof(byte[]))
				return (byte[])item;

			if(item is string)
				return Encoding.UTF8.GetBytes((string)item);

			if(Common.TypeExtension.IsScalarType(item.GetType()))
				return Encoding.UTF8.GetBytes(item.ToString());

			if(item is Stream stream)
			{
				var buffer = new byte[stream.Length - stream.Position];
				stream.Read(buffer, 0, buffer.Length);
				return buffer;
			}

			using(var memory = new MemoryStream())
			{
				Zongsoft.Serialization.Serializer.Json.Serialize(memory, item);
				return memory.ToArray();
			}

			throw new NotSupportedException(string.Format("The '{0}' content of message can not serialize.", item.GetType()));
		}
		#endregion

		#region 内部方法
		internal string GetRequestUrl(params string[] parts)
		{
			return _provider.GetRequestUrl(this.Name, parts);
		}
		#endregion
	}
}
