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
using System.Collections.Concurrent;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	public class MessageQueueProvider : Zongsoft.Collections.IQueueProvider
	{
		#region 成员字段
		private ConcurrentDictionary<string, MessageQueue> _queues;
		private Options.IConfiguration _configuration;
		#endregion

		#region 构造函数
		public MessageQueueProvider()
		{
			_queues = new ConcurrentDictionary<string, MessageQueue>(StringComparer.OrdinalIgnoreCase);
		}

		public MessageQueueProvider(Options.IConfiguration configuration)
		{
			_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			_queues = new ConcurrentDictionary<string, MessageQueue>(StringComparer.OrdinalIgnoreCase);
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
				_configuration = value ?? throw new ArgumentNullException();
			}
		}

		public MessageQueue this[string name]
		{
			get
			{
				return (MessageQueue)this.GetQueue(name);
			}
		}
		#endregion

		#region 公共方法
		public Collections.IQueue GetQueue(string name)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException("name");

			return _queues.GetOrAdd(name, key => new MessageQueue(this, name));
		}
		#endregion

		#region	内部方法
		internal ICertificate GetCertificate(string name)
		{
			var configuration = this.EnsureConfiguration();
			var certificate = string.Empty;

			if(configuration.Queues.TryGet(name, out var option))
				certificate = option.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				certificate = configuration.Queues.Certificate;

			if(string.IsNullOrWhiteSpace(certificate))
				return Aliyun.Configuration.Instance.Certificates.Default;

			return Aliyun.Configuration.Instance.Certificates.Get(certificate);
		}

		internal string GetRequestUrl(string queueName, params string[] parts)
		{
			var configuration = this.EnsureConfiguration();
			var region = configuration.Queues.Region ?? Aliyun.Configuration.Instance.Name;

			if(configuration.Queues.TryGet(queueName, out var option) && option.Region.HasValue)
				region = option.Region.Value;

			var center = ServiceCenter.GetInstance(region, Aliyun.Configuration.Instance.IsInternal);

			var path = parts == null ? string.Empty : string.Join("/", parts);

			if(string.IsNullOrEmpty(path))
				return string.Format("http://{0}.{1}/queues/{2}", configuration.Name, center.Path, queueName);
			else
				return string.Format("http://{0}.{1}/queues/{2}/{3}", configuration.Name, center.Path, queueName, path);
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private Options.IConfiguration EnsureConfiguration()
		{
			return this.Configuration ?? throw new InvalidOperationException("Missing required configuration of the message queue provider(aliyun).");
		}
		#endregion
	}
}
