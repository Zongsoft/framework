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
using System.Text;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Messaging;
using Zongsoft.Components;

namespace Zongsoft.Externals.Aliyun.Messaging;

public class MessageTopic : MessageQueueBase<MessageTopic.Consumer>
{
	#region 常量定义
	private readonly string MESSAGE_SEND_URL;

	private const string MESSAGE_CONTENT_NOTAG_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?><Message xmlns=""http://mns.aliyuncs.com/doc/v1/""><MessageBody>{0}</MessageBody></Message>";
	private const string MESSAGE_CONTENT_FULLY_TEMPLATE = @"<?xml version=""1.0"" encoding=""utf-8""?><Message xmlns=""http://mns.aliyuncs.com/doc/v1/""><MessageBody>{0}</MessageBody><MessageTag>{1}</MessageTag></Message>";
	#endregion

	#region 成员字段
	private HttpClient _http;
	#endregion

	#region 构造函数
	public MessageTopic(string name) : base(name)
	{
		this.Features.Add(MessageQueueFeature.Compression);

		//初始化相关操作的URL常量
		MESSAGE_SEND_URL = MessageTopicUtility.GetRequestUrl(name, "messages");

		var certificate = MessageTopicUtility.GetCertificate(name);
		_http = new HttpClient(new HttpClientHandler(certificate, MessageAuthenticator.Instance));
		_http.DefaultRequestHeaders.Add("x-mns-version", "2015-06-06");
	}
	#endregion

	#region 订阅方法
	protected override ValueTask<bool> OnSubscribeAsync(Consumer subscriber, CancellationToken cancellation = default) => ValueTask.FromResult(true);
	protected override ValueTask<Consumer> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) => ValueTask.FromResult(new Consumer(this, topic, handler, options));
	#endregion

	#region 发送消息
	protected override async ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
	{
		if(data.IsEmpty)
			return null;

		var compression = options?.Compression ?? default;
		var payload = compression.CanCompress(data.Length) ? MessageUtility.Pack(compression, data.Span) : data.ToArray();
		var response = await _http.PostAsync(MESSAGE_SEND_URL, CreateMessageRequest(payload, string.IsNullOrEmpty(tags) ? [] : tags.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)), cancellation);

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
	#endregion

	#region 资源释放
	protected override ValueTask DisposeAsync(bool disposing)
	{
		var http = Interlocked.Exchange(ref _http, null);

		if(http != null)
			http.Dispose();

		return ValueTask.CompletedTask;
	}
	#endregion

	#region 嵌套子类
	public class Consumer(MessageTopic queue, string topic, IHandler<Message> handler, MessageSubscribeOptions options = null) : MessageConsumerBase<MessageTopic>(queue, topic, handler, options)
	{
		protected override ValueTask OnCloseAsync(CancellationToken cancellation) => ValueTask.CompletedTask;
	}
	#endregion
}
