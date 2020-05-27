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
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;

using Zongsoft.Messaging;
using Zongsoft.Communication;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	public class TopicProvider : Zongsoft.Messaging.ITopicProvider
	{
		#region 成员字段
		private Options.IConfiguration _configuration;
		private readonly ConcurrentDictionary<string, ITopic> _topics;
		private readonly ConcurrentDictionary<string, HttpClient> _pool;
		#endregion

		#region 构造函数
		public TopicProvider()
		{
			_topics = new ConcurrentDictionary<string, ITopic>();
			_pool = new ConcurrentDictionary<string, HttpClient>();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置配置信息。
		/// </summary>
		public Options.IConfiguration Configuration
		{
			get
			{
				return _configuration;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_configuration = value;
			}
		}

		public ITopic this[string name]
		{
			get
			{
				if(string.IsNullOrWhiteSpace(name))
					throw new ArgumentNullException("name");

				return this.Get(name);
			}
		}
		#endregion

		#region 公共方法
		public ITopic Get(string name)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			ITopic topic;

			if(_topics.TryGetValue(name, out topic))
				return topic;

			var http = this.GetHttpClient(name);
			var response = http.GetAsync(this.GetRequestUrl(name));

			if(response.Result.IsSuccessStatusCode)
			{
				var info = MessageUtility.ResolveTopicInfo(response.Result.Content.ReadAsStreamAsync().Result);

				if(info != null)
					return new Topic(this, name, info, http);
			}

			return null;
		}

		public ITopic Register(string name, object state = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			var http = this.GetHttpClient(name);

			var response = http.PutAsync(this.GetRequestUrl(name), new StringContent(@"<Topic xmlns=""http://mns.aliyuncs.com/doc/v1/""><MaximumMessageSize>10240</MaximumMessageSize><LoggingEnabled>True</LoggingEnabled></Topic>", Encoding.UTF8, "application/xml"));

			if(response.Result.IsSuccessStatusCode)
			{
				var topic = _topics[name] = new Topic(this, name, null);
				return topic;
			}

			return null;
		}

		public bool Unregister(string name)
		{
			if(string.IsNullOrEmpty(name))
				return false;

			var http = this.GetHttpClient(name);
			var response = http.DeleteAsync(this.GetRequestUrl(name));

			return response.Result != null && response.Result.IsSuccessStatusCode;
		}
		#endregion

		#region 内部方法
		internal HttpClient GetHttpClient(string name)
		{
			var certificate = this.GetCertificate(name);

			return _pool.GetOrAdd(certificate.Name, key =>
			{
				var http = new HttpClient(new HttpClientHandler(certificate, MessageQueueAuthenticator.Instance));
				http.DefaultRequestHeaders.Add("x-mns-version", "2015-06-06");
				return http;
			});
		}

		internal ICertificate GetCertificate(string name)
		{
			var configuration = this.EnsureConfiguration();
			var certificate = string.Empty;

			if(configuration.Topics.TryGet(name, out var option))
				certificate = option.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				certificate = configuration.Topics.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				return Aliyun.Configuration.Instance.Certificates.Default;

			return Aliyun.Configuration.Instance.Certificates.Get(certificate);
		}

		internal string GetRequestUrl(string topicName, params string[] parts)
		{
			var configuration = this.EnsureConfiguration();
			var region = configuration.Topics.Region ?? Aliyun.Configuration.Instance.Name;

			if(configuration.Topics.TryGet(topicName, out var option) && option.Region.HasValue)
				region = option.Region.Value;

			var center = ServiceCenter.GetInstance(region, Aliyun.Configuration.Instance.IsInternal);

			var path = parts == null ? string.Empty : string.Join("/", parts);

			if(string.IsNullOrEmpty(path))
				return string.Format("http://{0}.{1}/topics/{2}", configuration.Name, center.Path, topicName);
			else
				return string.Format("http://{0}.{1}/topics/{2}/{3}", configuration.Name, center.Path, topicName, path);
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Options.IConfiguration EnsureConfiguration()
		{
			return this.Configuration ?? throw new InvalidOperationException("Missing required configuration of the topic provider(aliyun).");
		}
		#endregion
	}
}
