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
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Security
{
	[Service]
	public class SecretDirector
	{
		#region 私有变量
		private readonly ConcurrentDictionary<string, ISecretIssuer> _issuers;
		#endregion

		#region 构造函数
		public SecretDirector(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider;
			_issuers = new ConcurrentDictionary<string, ISecretIssuer>(StringComparer.OrdinalIgnoreCase);
			this.Initialize();
		}
		#endregion

		#region 公共属性
		[ServiceDependency]
		public ISecretor Secretor { get; set; }

		public IServiceProvider ServiceProvider { get; }

		public IDictionary<string, ISecretIssuer> Issuers { get => _issuers; }
		#endregion

		#region 公共方法
		public bool Exists(string token)
		{
			var secretor = this.Secretor ?? throw new InvalidOperationException("Missing a required secretor.");
			return secretor.Exists(token);
		}

		public bool Verify(string token, string secret) => this.Verify(token, secret, out _);
		public bool Verify(string token, string secret, out string extra)
		{
			var secretor = this.Secretor ?? throw new InvalidOperationException("Missing a required secretor.");
			return secretor.Verify(token, secret, out extra);
		}

		public string Issue(string destination, string template, string channel = null) => this.Issue(destination, template, null, null, channel);
		public string Issue(string destination, string template, string pattern, string extra, string channel = null)
		{
			var secretor = this.Secretor ?? throw new InvalidOperationException("Missing a required secretor.");
			var issuer = this.GetIssuer(destination, channel);

			if(issuer == null)
				return null;

			var token = Randomizer.GenerateString(16);
			var secret = secretor.Generate(token, pattern, extra);
			issuer.Issue(destination, template, secret, channel);
			return token;
		}
		#endregion

		#region 虚拟方法
		protected virtual void Initialize()
		{
			var issuers = this.ServiceProvider.ResolveAll<ISecretIssuer>();

			if(issuers == null)
				return;

			foreach(var issuer in issuers)
			{
				if(issuer.Channels != null && issuer.Channels.Length > 0)
				{
					foreach(var channel in issuer.Channels)
						_issuers.TryAdd(channel, issuer);
				}
			}
		}

		protected virtual ISecretIssuer GetIssuer(string destination, string channel)
		{
			if(string.IsNullOrEmpty(channel))
			{
				var issuers = _issuers.Values.Distinct();

				foreach(var issuer in issuers)
				{
					channel = issuer.GetChannel(destination);

					if(!string.IsNullOrEmpty(channel))
						return issuer;
				}
			}

			return channel != null && _issuers.TryGetValue(channel, out var result) ? result : null;
		}
		#endregion
	}
}
