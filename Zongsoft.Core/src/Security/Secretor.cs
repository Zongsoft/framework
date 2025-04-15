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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Communication;

namespace Zongsoft.Security;

[Service(typeof(ISecretor))]
public class Secretor : ISecretor
{
	#region 常量定义
	private const int DEFAULT_EXPIRY_MINUTES = 30;
	private const int DEFAULT_PERIOD_SECONDS = 60;
	#endregion

	#region 成员字段
	private TimeSpan _expiry;
	private TimeSpan _period;
	private IDistributedCache _cache;
	private ISecretor.SecretTransmitter _transmitter;
	private IServiceProvider _serviceProvider;
	#endregion

	#region 构造函数
	public Secretor(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

		//设置属性的默认值
		_expiry = TimeSpan.FromMinutes(DEFAULT_EXPIRY_MINUTES);
		_period = TimeSpan.FromSeconds(DEFAULT_PERIOD_SECONDS);

		//创建默认发射器
		this.Transmitter = new DefaultTransmitter(this, serviceProvider);
	}

	public Secretor(IDistributedCache cache, IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

		//设置缓存容器
		this.Cache = cache ?? throw new ArgumentNullException(nameof(cache));

		//设置属性的默认值
		_expiry = TimeSpan.FromMinutes(DEFAULT_EXPIRY_MINUTES);
		_period = TimeSpan.FromSeconds(DEFAULT_PERIOD_SECONDS);

		//创建默认发射器
		this.Transmitter = new DefaultTransmitter(this, serviceProvider);
	}
	#endregion

	#region 公共属性
	/// <summary>获取秘密内容的缓存容器。</summary>
	public IDistributedCache Cache
	{
		get => _cache ??= _serviceProvider.Resolve<IDistributedCache>() ?? _serviceProvider.ResolveRequired<IServiceProvider<IDistributedCache>>().GetService();
		set => _cache = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>获取或设置秘密内容的默认过期时长（默认为30分钟），不能设置为零。</summary>
	public TimeSpan Expiry
	{
		get => _expiry;
		set => _expiry = value > TimeSpan.Zero ? value : _expiry;
	}

	/// <summary>获取或设置重新生成秘密(验证码)的最小间隔时长（默认为60秒），如果为零则表示不做限制。</summary>
	public TimeSpan Period
	{
		get => _period;
		set => _period = value;
	}

	/// <summary>获取秘密(验证码)发射器。</summary>
	public ISecretor.SecretTransmitter Transmitter
	{
		get => _transmitter;
		set => _transmitter = value ?? throw new ArgumentNullException();
	}
	#endregion

	#region 公共方法
	public async ValueTask<(bool, TimeSpan)> ExistsAsync(string name, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name))
			return default;

		//修复秘密名（转换成小写并剔除收尾空格）
		name = PruneKey(name);

		//从缓存中异步获取缓存内容
		(var existed, var cacheValue) = await this.Cache.TryGetValueAsync<string>(name, cancellation);

		//从缓存内容解析出对应的缓存值和发出时间
		if(existed && this.Unpack(cacheValue, out var _, out var timestamp, out _))
			return (true, DateTime.UtcNow - timestamp);

		return default;
	}

	public async ValueTask<(bool, string)> RemoveAsync(string name, string secret, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(secret))
			return (false, null);

		//修复秘密名（转换成小写并剔除收尾空格）
		name = PruneKey(name);

		//从缓存中异步获取缓存内容
		(var existed, var cacheValue) = await this.Cache.TryGetValueAsync<string>(name, cancellation);

		//从缓存内容解析出对应的秘密值并且比对秘密内容成功
		if(existed && this.Unpack(cacheValue, out var cachedSecret, out _, out var extra) &&
		   string.Equals(secret, cachedSecret, StringComparison.OrdinalIgnoreCase))
		{
			await this.Cache.RemoveAsync(name, cancellation);

			//返回校验成功
			return (true, extra);
		}

		//返回校验失败
		return (false, null);
	}

	public ValueTask<string> GenerateAsync(string name, string extra = null, CancellationToken cancellation = default) => this.GenerateAsync(name, null, extra, cancellation);
	public async ValueTask<string> GenerateAsync(string name, string pattern, string extra, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//修复秘密名（转换成小写并剔除收尾空格）
		name = PruneKey(name);

		//从缓存容器中异步获取对应的内容
		(var existed, var text) = await this.Cache.TryGetValueAsync<string>(name, cancellation);

		if(existed && !string.IsNullOrWhiteSpace(text))
		{
			//尚未验证：则必须确保在最小时间间隔之后才能重新生成
			if(_period > TimeSpan.Zero &&
			   this.Unpack(text, out _, out var timestamp, out _) &&
			   DateTime.UtcNow - timestamp < _period)
				throw new SecurityException("Secret.TooFrequently", Properties.Resources.SecretGenerateTooFrequently_Message);
		}

		//根据指定的模式生成或获取秘密（验证码）
		var secret = this.GenerateSecret(name, pattern);

		//将秘密内容保存到缓存容器中（如果指定的过期时长为零则采用默认过期时长）
		await this.Cache.SetValueAsync(name, this.Pack(secret, extra), _expiry, CacheRequisite.Always, cancellation);

		return secret;
	}

	public async ValueTask<(bool, string)> VerifyAsync(string name, string secret, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(name) || string.IsNullOrEmpty(secret))
			return (false, null);

		//修复秘密名（转换成小写并剔除收尾空格）
		name = PruneKey(name);

		(var existed, var cacheValue) = await this.Cache.TryGetValueAsync<string>(name, cancellation);

		//从缓存内容解析出对应的秘密值并且比对秘密内容成功
		if(existed && this.Unpack(cacheValue, out var cachedSecret, out _, out var extra) &&
		   string.Equals(secret, cachedSecret, StringComparison.OrdinalIgnoreCase))
		{
			/*
			 * 注意：不删除缓存项！
			 * 在缓存的有效期内，都可以重复进行校验。
			 */

			//返回校验成功
			return (true, extra);
		}

		//返回校验失败
		return (false, null);
	}
	#endregion

	#region 虚拟方法
	protected virtual string GenerateSecret(string name, string pattern = null)
	{
		if(string.IsNullOrWhiteSpace(pattern))
			return Randomizer.GenerateString(6, true);

		if(string.Equals(pattern, "guid", StringComparison.OrdinalIgnoreCase) || string.Equals(pattern, "uuid", StringComparison.OrdinalIgnoreCase))
			return Guid.NewGuid().ToString("N");

		if(pattern.Length > 1 && (pattern[0] == '?' || pattern[0] == '*' || pattern[0] == '#'))
		{
			if(int.TryParse(pattern.AsSpan(1), out var count))
				return Randomizer.GenerateString(count, pattern[0] == '#');

			throw new ArgumentException("Invalid secret pattern.");
		}
		else
		{
			if(pattern.Contains(':') | pattern.Contains('|'))
				throw new ArgumentException("The secret argument contains illegal characters.");
		}

		return pattern.Trim();
	}

	protected virtual string Pack(string secret, string extra)
	{
		var timestamp = Timestamp.Millennium.Now;

		if(string.IsNullOrEmpty(extra))
			return $"{secret}|{timestamp}";
		else
			return $"{secret}|{timestamp}|{extra}";
	}

	protected virtual bool Unpack(string text, out string secret, out DateTime timestamp, out string extra)
	{
		secret = null;
		extra = null;
		timestamp = Timestamp.Millennium.Epoch;

		if(string.IsNullOrEmpty(text))
			return false;

		var index = 0;
		var last = 0;
		ulong number;

		for(int i = 0; i < text.Length; i++)
		{
			if(text[i] == '|')
			{
				switch(index++)
				{
					case 0:
						secret = text[..i];
						break;
					case 1:
						if(ulong.TryParse(text.AsSpan(last, i - last), out number))
							timestamp = Timestamp.Millennium.Epoch.AddSeconds(number);

						if(i < text.Length - 1)
							extra = text[(i + 1)..];

						return true;
				}

				last = i + 1;
			}
		}

		if(last < text.Length && ulong.TryParse(text.AsSpan(last), out number))
			timestamp = Timestamp.Millennium.Epoch.AddSeconds(number);

		return !string.IsNullOrEmpty(secret);
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static string PruneKey(string name) => string.IsNullOrEmpty(name) ? name : $"Zongsoft.Secretor:{name.Trim().ToLowerInvariant()}";
	#endregion

	#region 嵌套子类
	private class DefaultTransmitter : ISecretor.SecretTransmitter
	{
		#region 常量定义
		private const int NONE_STATE = 0;
		private const int INITIALIZING_STATE = 1;
		private const int INITIALIZED_STATE  = 2;
		#endregion

		#region 静态变量
		private static readonly System.Security.Cryptography.HashAlgorithm _hasher = System.Security.Cryptography.MD5.Create();
		#endregion

		#region 私有变量
		private volatile int _state;
		private readonly ISecretor _secretor;
		private readonly IDictionary<string, ICaptcha> _captchas;
		private readonly IDictionary<string, ITransmitter> _transmitters;
		private readonly IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		internal DefaultTransmitter(ISecretor secretor, IServiceProvider serviceProvider)
		{
			_secretor = secretor ?? throw new ArgumentNullException(nameof(secretor));
			_captchas = new Dictionary<string, ICaptcha>(StringComparer.OrdinalIgnoreCase);
			_transmitters = new Dictionary<string, ITransmitter>(StringComparer.OrdinalIgnoreCase);
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}
		#endregion

		#region 公共方法
		public override async ValueTask<string> TransmitAsync(string scheme, string destination, string template, string scenario, string captcha, string channel, string extra, CancellationToken cancellation)
		{
			this.Initialize(_serviceProvider);

			if(!string.IsNullOrEmpty(scheme) && !string.IsNullOrEmpty(destination) && _transmitters.TryGetValue(scheme, out var transmitter) && transmitter != null)
			{
				if(!string.IsNullOrEmpty(captcha))
				{
					var index = captcha.IndexOfAny([':', '=', ' ', '\t']);

					if(index <= 0 || index >= captcha.Length - 1)
						throw new SecurityException("Captcha", "Invalid captch format.");

					if(!_captchas.TryGetValue(captcha[..index], out var verifier))
						throw new SecurityException("Captcha", $"The specified '{captcha[..index]}' CAPTCHA is invalid.");

					if(!await verifier.VerifyAsync(captcha[(index + 1)..], cancellation))
						throw new SecurityException("Captcha", $"The specified '{verifier.Scheme}' CAPTCHA failed to validate.");
				}

				var token = GetKey(scheme, destination, template, scenario, channel);
				var value = await _secretor.GenerateAsync(token, null, extra, cancellation);

				//发送验证码到目的地
				await transmitter.TransmitAsync(destination, channel, template, new SecretTemplateData(value), cancellation);

				return token;
			}

			return null;
		}
		#endregion

		#region 私有方法
		private void Initialize(IServiceProvider serviceProvider)
		{
			if(_state == INITIALIZED_STATE)
				return;

			//尝试将状态由未初始化设置为初始化中
			var state = Interlocked.CompareExchange(ref _state, INITIALIZING_STATE, NONE_STATE);

			//如果之前的状态就是初始化中，则等待其他线程完成初始化
			if(state == INITIALIZING_STATE)
			{
				SpinWait.SpinUntil(() => _state == INITIALIZED_STATE, 1000);
				return;
			}

			try
			{
				var captchas = serviceProvider.ResolveAll<ICaptcha>();
				if(captchas != null)
				{
					foreach(var captcha in captchas)
						_captchas.TryAdd(captcha.Scheme, captcha);
				}

				var transmitters = serviceProvider.ResolveAll<ITransmitter>();
				if(transmitters != null)
				{
					foreach(var transmitter in transmitters)
						_transmitters.TryAdd(transmitter.Name, transmitter);
				}
			}
			finally
			{
				//设置状态为已初始化完成
				_state = INITIALIZED_STATE;
			}
		}

		private static string GetKey(string scheme, string destination, string template, string scenario, string channel)
		{
			if(string.IsNullOrEmpty(destination) && string.IsNullOrEmpty(template))
				return null;

			//组合参数为固定格式
			var argument = string.IsNullOrEmpty(channel) ?
				$"{scheme}:{destination}@{template}?{scenario}" :
				$"{scheme}.{channel}:{destination}@{template}?{scenario}";

			//计算组合键的哈希值
			var hash = _hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(argument.ToLowerInvariant()));

			//返回键哈希值的十六进制字符串作为缓存键
			return System.Convert.ToHexString(hash);
		}
		#endregion

		#region 嵌套结构
		private readonly struct SecretTemplateData(string code)
		{
			public readonly string Code = code;
		}
		#endregion
	}
	#endregion
}
