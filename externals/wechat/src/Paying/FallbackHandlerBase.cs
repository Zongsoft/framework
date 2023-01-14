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
using System.IO;
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
	public abstract class FallbackHandlerBase<TRequest> : HandlerBase<Stream>
	{
		#region 私有变量
		private IEnumerable<IServiceProvider<IAuthority>> _providers;
		#endregion

		#region 构造函数
		protected FallbackHandlerBase(IServiceProvider serviceProvider)
		{
			this.ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 保护属性
		protected IServiceProvider ServiceProvider { get; }
		#endregion

		#region 重写方法
		public override async ValueTask<object> HandleAsync(object caller, Stream input, CancellationToken cancellation = default)
		{
			var request = await this.GetRequestAsync(caller, input, cancellation);
			return await this.OnHandleAsync(caller, request, cancellation);
		}
		#endregion

		#region 抽象方法
		protected abstract Type GetRequestType(string format);
		protected abstract ValueTask<object> OnHandleAsync(object caller, TRequest request, CancellationToken cancellation = default);
		#endregion

		#region 虚拟方法
		protected virtual IAuthority GetAuthority(string code, string type)
		{
			var authority = AuthorityUtility.GetAuthority(code);

			if(authority != null)
				return authority;

			foreach(var provider in _providers ??= this.ServiceProvider.ResolveAll<IServiceProvider<IAuthority>>())
			{
				authority = provider.GetService(code);

				if(authority != null)
					return authority;
			}

			return null;
		}
		#endregion

		#region 内部方法
		internal async ValueTask<TRequest> GetRequestAsync(object caller, Stream input, CancellationToken cancellation = default)
		{
			if(caller == null || input == null)
				return default;

			if(caller is string text)
			{
				var (key, format) = Resolve(text);
				var authority = GetAuthority(key, format);

				if(authority == null)
					throw new OperationException("NotFound", $"Didn't find the '{key}' authority or it has no certificate.");

				if(string.IsNullOrEmpty(authority.Secret))
					throw new OperationException("InvalidKey", $"The specified '{key}' authority has no secret key.");

				var message = await JsonSerializer.DeserializeAsync<FallbackMessage>(input, Json.Options, cancellation);
				var resource = message.Resource;
				byte[] data;

				try
				{
					data = CryptographyHelper.Decrypt1(authority.Secret, resource.Nonce, resource.AssociatedData, resource.Ciphertext);
				}
				catch(Exception ex)
				{
					Zongsoft.Diagnostics.Logger.Error(ex, $"微信回调解密出错：\nAuthority.name:{authority.Name}\nAuthority.Secret:{authority.Secret}\nResource.Nonce:{resource.Nonce}\nResource.AssociatedData:{resource.AssociatedData}\nResource.Ciphertext:{resource.Ciphertext}");
					throw;
				}

				var payload = JsonSerializer.Deserialize(data, GetRequestType(format), Json.Options);
				return (TRequest)payload;
			}

			throw new OperationException("NotFound");
		}
		#endregion

		#region 私有方法
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
		#endregion
	}
}
