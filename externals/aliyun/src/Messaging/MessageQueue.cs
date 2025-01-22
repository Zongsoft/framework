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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;

using Zongsoft.Messaging;
using Zongsoft.Components;

namespace Zongsoft.Externals.Aliyun.Messaging;

public class MessageQueue : MessageQueueBase<MessageQueue.Consumer>
{
	#region 常量定义
	private static readonly Regex COUNT_REGEX = new Regex(@"\<(?'tag'(ActiveMessages|InactiveMessages|DelayMessages))\>\s*(?<value>[^<>\s]+)\s*\<\/\k'tag'\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
	private static readonly Regex DELAY_REGEX = new Regex(@"\<(?'tag'(ReceiptHandle|NextVisibleTime))\>\s*(?<value>[^<>\s]+)\s*\<\/\k'tag'\>", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);
	#endregion

	#region 成员字段
	private HttpClient _http;
	#endregion

	#region 构造函数
	public MessageQueue(string name) : base(name)
	{
		var certificate = MessageQueueUtility.GetCertificate(name);
		_http = new HttpClient(new HttpClientHandler(certificate, MessageAuthenticator.Instance));
		_http.DefaultRequestHeaders.Add("x-mns-version", "2015-06-06");
	}
	#endregion

	#region 订阅方法
	protected override ValueTask<bool> OnSubscribeAsync(MessageQueue.Consumer subscriber, CancellationToken cancellation = default) => ValueTask.FromResult(true);
	protected override ValueTask<Consumer> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => ValueTask.FromResult(new Consumer(this, handler, options));
	#endregion

	#region 数量获取
	public async ValueTask<long> GetCountAsync(CancellationToken cancellation = default)
	{
		var response = await _http.GetAsync(this.GetRequestUrl(), cancellation);

		if(!response.IsSuccessStatusCode)
			return -1;

		var content = await response.Content.ReadAsStringAsync(cancellation);

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
	#endregion

	#region 出队方法
	public async ValueTask<Message> DequeueAsync(MessageDequeueOptions options, CancellationToken cancellation = default)
	{
		if(options == null)
			options = MessageDequeueOptions.Default;

		var waitSeconds = (long)Math.Ceiling(options.Timeout.TotalSeconds);
		var request = new HttpRequestMessage(HttpMethod.Get, this.GetRequestUrl("messages") + (options != null && options.Timeout >= TimeSpan.Zero ? "?waitseconds=" + waitSeconds.ToString() : string.Empty));
		request.Headers.Add("x-mns-version", "2015-06-06");
		var response = await _http.SendAsync(request, cancellation);

		if(response.IsSuccessStatusCode)
			return MessageUtility.ResolveMessage(this, await response.Content.ReadAsStreamAsync(cancellation));

		var exception = AliyunException.Parse(await response.Content.ReadAsStringAsync(cancellation));

		if(exception != null)
		{
			switch(exception.Code)
			{
				case MessageUtility.MessageNotExist:
					return Message.Empty;
				case MessageUtility.QueueNotExist:
					throw exception;
				default:
					throw exception;
			}
		}

		return Message.Empty;
	}
	#endregion

	#region 入队方法
	public string Enqueue(ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null) => this.EnqueueAsync(data, options, CancellationToken.None).GetAwaiter().GetResult();
	public ValueTask<string> EnqueueAsync(ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default) => this.EnqueueAsync(data.ToArray(), options, cancellation);
	public async ValueTask<string> EnqueueAsync(byte[] data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
	{
		if(options == null)
			options = MessageEnqueueOptions.Default;

		if(options.Priority < 1)
			options.Priority = 1;
		else if(options.Priority > 16)
			options.Priority = 16;

		if(options.Delay.TotalDays > 7)
			options.Delay = TimeSpan.FromDays(7);

		var text = @"<Message xmlns=""http://mqs.aliyuncs.com/doc/v1/""><MessageBody>" +
			System.Convert.ToBase64String(data) +
			"</MessageBody><DelaySeconds>" +
			((int)options.Delay.TotalSeconds).ToString() +
			"</DelaySeconds><Priority>" + options.Priority.ToString() + "</Priority></Message>";

		var request = new HttpRequestMessage(HttpMethod.Post, this.GetRequestUrl("messages"))
		{
			Content = new StringContent(text, Encoding.UTF8, "text/xml")
		};
		request.Headers.Add("x-mns-version", "2015-06-06");

		var response = await _http.SendAsync(request, cancellation);

		if(!response.IsSuccessStatusCode)
		{
			Zongsoft.Diagnostics.Logger.GetLogger(this).Warn("[" + response.StatusCode + "] The message enqueue failed." + Environment.NewLine + await response.Content.ReadAsStringAsync());
			return null;
		}

		return MessageUtility.GetMessageResponseId(await response.Content.ReadAsStreamAsync(cancellation));
	}

	public override ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
	{
		return this.EnqueueAsync(data, options, cancellation);
	}
	#endregion

	#region 应答方法
	public async ValueTask AcknowledgeAsync(string acknowledgementId, TimeSpan delay, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(acknowledgementId))
			return;

		if(delay <= TimeSpan.Zero)
		{
			var request = new HttpRequestMessage(HttpMethod.Delete, this.GetRequestUrl("messages") + "?ReceiptHandle=" + Uri.EscapeDataString(acknowledgementId));
			request.Headers.Add("x-mns-version", "2015-06-06");
			await _http.SendAsync(request, cancellation);
			return;
		}

		await this.DelayAsync(acknowledgementId, delay, cancellation);
	}

	public async ValueTask DelayAsync(string acknowledgementId, TimeSpan duration, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(acknowledgementId))
			return;

		var request = new HttpRequestMessage(HttpMethod.Put, this.GetRequestUrl("messages") + "?ReceiptHandle=" + Uri.EscapeDataString(acknowledgementId) + "&VisibilityTimeout=" + duration.TotalSeconds.ToString());
		request.Headers.Add("x-mns-version", "2015-06-06");
		var response = await _http.SendAsync(request, cancellation);

		if(!response.IsSuccessStatusCode)
		{
			var exception = AliyunException.Parse(await response.Content.ReadAsStringAsync(cancellation));

			if(exception != null)
				throw exception;

			response.EnsureSuccessStatusCode();
		}

		var matches = DELAY_REGEX.Matches(await response.Content.ReadAsStringAsync(cancellation));

		foreach(Match match in matches)
		{
			switch(match.Groups["tag"].Value)
			{
				case "ReceiptHandle":
					acknowledgementId = match.Groups["value"].Value;
					break;
				case "NextVisibleTime":
					var expiration = Utility.GetDateTimeFromEpoch(match.Groups["value"].Value);
					break;
			}
		}
	}
	#endregion

	#region 内部方法
	internal string GetRequestUrl(params string[] parts) => MessageQueueUtility.GetRequestUrl(this.Name, parts);
	#endregion

	#region 资源释放
	protected override void Dispose(bool disposing)
	{
		var http = Interlocked.Exchange(ref _http, null);

		if(http != null)
			http.Dispose();
	}
	#endregion

	#region 嵌套子类
	public class Consumer(MessageQueue queue, IHandler<Message> handler, MessageSubscribeOptions options = null) : MessageConsumerBase<MessageQueue>(queue, handler, options)
	{
		protected override ValueTask OnCloseAsync(CancellationToken cancellation) => ValueTask.CompletedTask;
	}

	private class MessageQueuePoller(MessageQueue queue, IHandler<Message> handler) : MessagePollerBase
	{
		private readonly MessageQueue _queue = queue;
		private readonly IHandler<Message> _handler = handler;

		protected override Message Receive(MessageDequeueOptions options, CancellationToken cancellation)
		{
			return _queue.DequeueAsync(options, cancellation).AsTask().GetAwaiter().GetResult();
		}

		protected override ValueTask OnHandleAsync(Message message, CancellationToken cancellation)
		{
			return _handler.HandleAsync(message, cancellation);
		}
	}
	#endregion
}
