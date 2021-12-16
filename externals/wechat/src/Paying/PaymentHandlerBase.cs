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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Externals.Wechat.Paying
{
	public abstract class PaymentHandlerBase : HandlerBase<PaymentHandlerBase.Message>
	{
		private IEnumerable<IServiceProvider<IAuthority>> _providers;

		protected PaymentHandlerBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		protected IServiceProvider ServiceProvider { get; }

		public override ValueTask<OperationResult> HandleAsync(object caller, Message request, CancellationToken cancellation = default)
		{
			if(caller is string key && key != null)
			{
				var authority = GetAuthority(key);

				if(authority != null && authority.Secret != null)
				{
					var resource = request.Resource;
					var data = CryptographyHelper.Decrypt1(authority.Secret, resource.Nonce, resource.AssociatedData, resource.Ciphertext);
					var payload = JsonSerializer.Deserialize<PaymentManager.PaymentService.PaymentOrder>(data);
					return this.OnHandleAsync(caller, payload, cancellation);
				}
			}

			return ValueTask.FromResult(OperationResult.Fail());
		}

		protected abstract ValueTask<OperationResult> OnHandleAsync(object caller, PaymentManager.PaymentService.PaymentOrder request, CancellationToken cancellation);

		private IAuthority GetAuthority(string key)
		{
			var authority = AuthorityFactory.GetAuthority(key);

			if(authority != null)
				return authority;

			foreach(var provider in _providers ??= this.ServiceProvider.ResolveAll<IServiceProvider<IAuthority>>())
			{
				authority = provider.GetService(key);

				if(authority != null)
					return authority;
			}

			return null;
		}

		#region 嵌套子类
		public class Message
		{
			[JsonPropertyName("id")]
			public string Identifier { get; set; }

			[JsonPropertyName("create_time")]
			public DateTime Timestamp { get; set; }

			[JsonPropertyName("event_type")]
			public string Status { get; set; }

			[JsonPropertyName("summary")]
			public string Description { get; set; }

			[JsonPropertyName("resource_type")]
			public string ResourceType { get; set; }

			[JsonPropertyName("resource")]
			public ResourceInfo Resource { get; set; }

			public struct ResourceInfo
			{
				[JsonPropertyName("original_type")]
				public string Source { get; set; }
				[JsonPropertyName("algorithm")]
				public string Algorithm { get; set; }
				[JsonPropertyName("nonce")]
				public string Nonce { get; set; }
				[JsonPropertyName("associated_data")]
				public string AssociatedData { get; set; }
				[JsonPropertyName("ciphertext")]
				public string Ciphertext { get; set; }
			}
		}
		#endregion
	}
}
