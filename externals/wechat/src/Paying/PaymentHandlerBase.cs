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
		#region 私有变量
		private IEnumerable<IServiceProvider<IAuthority>> _providers;
		#endregion

		#region 构造函数
		protected PaymentHandlerBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 保护属性
		protected IServiceProvider ServiceProvider { get; }
		#endregion

		#region 重写方法
		public override ValueTask<OperationResult> HandleAsync(object caller, Message request, CancellationToken cancellation = default)
		{
			if(caller is string text && text != null)
			{
				var (key, format) = Resolve(text);
				var authority = GetAuthority(key);

				if(authority == null)
					return ValueTask.FromResult(OperationResult.Fail("AuthorityNotFound", $"Didn't find the '{key}' authority or it has no certificate."));

				if(string.IsNullOrEmpty(authority.Secret))
					return ValueTask.FromResult(OperationResult.Fail("MissingSecret", $"The specified '{key}' authority has no secret key."));

				var resource = request.Resource;
				var data = CryptographyHelper.Decrypt1(authority.Secret, resource.Nonce, resource.AssociatedData, resource.Ciphertext);
				var payload = JsonSerializer.Deserialize(data, GetModelType(format), Json.Options);
				return this.OnHandleAsync(caller, (PaymentManager.PaymentService.PaymentOrder)payload, cancellation);
			}

			return ValueTask.FromResult(OperationResult.Fail());
		}

		internal ValueTask<OperationResult> HandleAsync(object caller, PaymentManager.PaymentService.PaymentOrder request, CancellationToken cancellation = default) => this.OnHandleAsync(caller, request, cancellation);
		#endregion

		#region 抽象方法
		protected abstract ValueTask<OperationResult> OnHandleAsync(object caller, PaymentManager.PaymentService.PaymentOrder request, CancellationToken cancellation);
		#endregion

		#region 私有方法
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

		private static (string authority, string format) Resolve(ReadOnlySpan<char> key)
		{
			if(key.IsEmpty)
				return default;

			var index = key.LastIndexOf(':');

			switch(index)
			{
				case < 0:
					return (key.ToString(), null);
				case 0:
					return (null, key[1..].ToString());
				case > 0:
					return index == key.Length - 1 ?
						(key.Slice(0, index).ToString(), null) :
						(key.Slice(0, index).ToString(), key[(index + 1)..].ToString());
			}
		}

		private static Type GetModelType(string format)
		{
			return string.Equals(format, PaymentManager.PaymentService.DirectPaymentService.FORMAT, StringComparison.OrdinalIgnoreCase) ?
				typeof(PaymentManager.PaymentService.DirectPaymentService.DirectOrder) :
				typeof(PaymentManager.PaymentService.BrokerPaymentService.BrokerOrder);
		}
		#endregion

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
