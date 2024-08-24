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
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

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

		#region 公共方法
		public ValueTask HandleAsync(TRequest request, CancellationToken cancellation = default) =>
			this.OnHandleAsync(request, null, cancellation);

		public ValueTask HandleAsync(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(parameters != null && parameters.Any())
				return this.OnHandleAsync(request, new Dictionary<string, object>(parameters), cancellation);
			else
				return this.OnHandleAsync(request, null, cancellation);
		}
		#endregion

		#region 重写方法
		protected override async ValueTask OnHandleAsync(Stream stream, Parameters parameters, CancellationToken cancellation)
		{
			var request = await this.GetRequestAsync(stream, parameters, cancellation);
			await this.OnHandleAsync(request, parameters, cancellation);
		}
		#endregion

		#region 抽象方法
		internal protected abstract Type GetRequestType(string format);
		protected abstract ValueTask OnHandleAsync(TRequest request, Parameters parameters, CancellationToken cancellation);
		#endregion

		#region 虚拟方法
		internal protected virtual IAuthority GetAuthority(string code, string type)
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
	}

	public abstract class FallbackHandlerBase<TRequest, TResult> : HandlerBase<Stream, TResult>
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

		#region 公共方法
		public ValueTask<TResult> HandleAsync(TRequest request, CancellationToken cancellation = default) => this.OnHandleAsync(request, null, cancellation);
		public ValueTask<TResult> HandleAsync(TRequest request, IEnumerable<KeyValuePair<string, object>> parameters, CancellationToken cancellation = default)
		{
			if(parameters != null && parameters.Any())
				return this.OnHandleAsync(request, new Dictionary<string, object>(parameters), cancellation);
			else
				return this.OnHandleAsync(request, null, cancellation);
		}
		#endregion

		#region 重写方法
		protected override async ValueTask<TResult> OnHandleAsync(Stream stream, Parameters parameters, CancellationToken cancellation)
		{
			var request = await this.GetRequestAsync(stream, parameters, cancellation);
			return await this.OnHandleAsync(request, parameters, cancellation);
		}
		#endregion

		#region 抽象方法
		internal protected abstract Type GetRequestType(string format);
		protected abstract ValueTask<TResult> OnHandleAsync(TRequest request, Parameters parameters, CancellationToken cancellation);
		#endregion

		#region 虚拟方法
		internal protected virtual IAuthority GetAuthority(string code, string type)
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
	}

	internal static class FallbackHandlerUtility
	{
		public static ValueTask<TRequest> GetRequestAsync<TRequest>(this FallbackHandlerBase<TRequest> handler, Stream stream, Parameters parameters, CancellationToken cancellation = default)
		{
			return GetRequestAsync<TRequest>(stream, parameters, handler.GetAuthority, handler.GetRequestType, cancellation);
		}

		public static ValueTask<TRequest> GetRequestAsync<TRequest, TResult>(this FallbackHandlerBase<TRequest, TResult> handler, Stream stream, Parameters parameters, CancellationToken cancellation = default)
		{
			return GetRequestAsync<TRequest>(stream, parameters, handler.GetAuthority, handler.GetRequestType, cancellation);
		}

		private static async ValueTask<TRequest> GetRequestAsync<TRequest>(Stream stream, Parameters parameters, Func<string, string, IAuthority> authorityThunk, Func<string, Type> typeThunk, CancellationToken cancellation = default)
		{
			if(stream == null)
				return default;

			if(parameters != null && parameters.TryGetValue("key", out var value) && value is string text)
			{
				var (key, format) = Resolve(text);
				var authority = authorityThunk(key, format);

				if(authority == null)
					throw OperationException.Unfound($"Didn't find the '{key}' authority or it has no certificate.");

				if(string.IsNullOrEmpty(authority.Secret))
					throw OperationException.Unsatisfied($"The specified '{key}' authority has no secret key.");

				var message = await JsonSerializer.DeserializeAsync<FallbackMessage>(stream, Json.Options, cancellation);
				var resource = message.Resource;
				byte[] data;

				try
				{
					data = CryptographyHelper.Decrypt1(authority.Secret, resource.Nonce, resource.AssociatedData, resource.Ciphertext);
				}
				catch(Exception ex)
				{
					Zongsoft.Diagnostics.Logger.GetLogger(typeof(FallbackHandlerUtility)).Error(ex, $"微信回调解密出错：\nAuthority.name:{authority.Name}\nAuthority.Secret:{authority.Secret}\nResource.Nonce:{resource.Nonce}\nResource.AssociatedData:{resource.AssociatedData}\nResource.Ciphertext:{resource.Ciphertext}");
					throw;
				}

				var payload = JsonSerializer.Deserialize(data, typeThunk(format), Json.Options);
				return (TRequest)payload;
			}

			throw OperationException.Unfound();
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
						(key[..index].ToString(), null) :
						(key[..index].ToString(), key[(index + 1)..].ToString());
			}
		}
	}
}
