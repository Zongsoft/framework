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
 * This file is part of Zongsoft.Security.Captcha library.
 *
 * The Zongsoft.Security.Captcha is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Security.Captcha is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Security.Captcha library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using SixLabors.ImageSharp;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Collections;

namespace Zongsoft.Security.Captcha
{
	[Service<ICaptcha>]
	public class AuthencodeCaptcha(IServiceProvider serviceProvider) : ICaptcha, IMatchable, IMatchable<string>
	{
		#region 常量定义
		private const string TOKEN_PARAMETER  = "token";
		private const string CACHE_KEY_SUFFIX = "OK";
		private const string CACHE_KEY_PREFIX = "Zongsoft.Security.Captcha:Authencode";
		#endregion

		#region 成员字段
		private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		#endregion

		#region 公共属性
		public string Scheme => "Authencode";
		public IDistributedCache Cache => _serviceProvider.ResolveRequired<IServiceProvider<IDistributedCache>>().GetService();
		#endregion

		#region 公共方法
		public async ValueTask<object> IssueAsync(object argument, Parameters parameters, CancellationToken cancellation = default)
		{
			var cache = this.Cache;
			var token = Randomizer.GenerateString(12);
			var code = Randomizer.GenerateString(6);

			using var image = AuthencodeImager.Generate(code);
			using var stream = new MemoryStream(8 * 1024);

			await image.SaveAsPngAsync(stream, cancellation);
			await cache.SetValueAsync(GetKey(token), code, TimeSpan.FromMinutes(10), CacheRequisite.Always, cancellation);

			return new AuthencodeCaptchaResult(token, stream.ToArray(), "image/png");
		}

		public async ValueTask<string> VerifyAsync(object argument, Parameters parameters, CancellationToken cancellation = default)
		{
			if(argument == null)
				return null;

			var code = argument.ToString();
			if(string.IsNullOrEmpty(code))
				return null;

			var index = code.IndexOfAny([':', '=']);
			if(index < 1 || index >= code.Length - 1)
				return null;

			var token = code[..index];
			code = code[(index + 1)..];

			var cache = this.Cache;
			var cachedValue = await cache.GetValueAsync(GetKey(token), cancellation);

			if(cachedValue != null && string.Equals(code, cachedValue.ToString(), StringComparison.OrdinalIgnoreCase))
			{
				//生成校验成功的确认号
				var confirmation = Randomizer.GenerateString(10);

				//将确认凭证保存到缓存中
				if(await cache.SetValueAsync(GetKey(confirmation, CACHE_KEY_SUFFIX), token, TimeSpan.FromMinutes(5), CacheRequisite.Always, cancellation))
					return confirmation;
			}

			return null;
		}

		public async ValueTask<bool> VerifyAsync(string token, CancellationToken cancellation)
		{
			if(string.IsNullOrEmpty(token))
				return false;

			//将验证确认缓存项删除，并获取其保存的令牌号
			if(this.Cache.Remove(GetKey(token, CACHE_KEY_SUFFIX), out var value) && value != null)
			{
				//将验证原始令牌缓存项删除
				await this.Cache.RemoveAsync(GetKey(value.ToString()), cancellation);

				//返回验证成功
				return true;
			}

			return false;
		}
		#endregion

		#region 私有方法
		private static string GetKey(string token, string suffix = null) =>
			string.IsNullOrEmpty(suffix) ?
			$"{CACHE_KEY_PREFIX}:{token}" :
			$"{CACHE_KEY_PREFIX}:{token}!{suffix}";
		#endregion

		#region 服务匹配
		bool IMatchable<string>.Match(string argument) => this.Scheme.Equals(argument, StringComparison.OrdinalIgnoreCase);
		bool IMatchable.Match(object argument) => argument is string scheme && this.Scheme.Equals(scheme, StringComparison.OrdinalIgnoreCase);
		#endregion

		#region 嵌套结构
		internal sealed class AuthencodeCaptchaResult(string token, byte[] data, string type)
		{
			public readonly string Token = token;
			public readonly byte[] Data = data;
			public readonly string Type = type;
			public bool HasValue => this.Data != null && this.Data.Length > 0;
		}
		#endregion
	}
}