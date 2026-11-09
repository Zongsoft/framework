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
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;

using Zongsoft.Services;

namespace Zongsoft.Security;

partial class Module
{
	#region 成员字段
	private Lazy<Diagnostor> _meter = new(() => new(), true);
	#endregion

	#region 公共属性
	/// <summary>获取当前模块的性能诊断器。</summary>
	public Diagnostor Meter => _meter.Value;
	#endregion

	public class Diagnostor
	{
		#region 私有字段
		private readonly Meter _meter;
		#endregion

		#region 公共字段
		public readonly AuthenticationMeter Authentication;
		#endregion

		#region 构造函数
		internal Diagnostor()
		{
			#if NET8_0_OR_GREATER
			_meter = Current.Services.ResolveRequired<IMeterFactory>().Create(NAME);
			#else
			_meter = new Meter(NAME, Current.Version.ToString());
			#endif

			this.Authentication = new(_meter);
		}
		#endregion
	}

	public sealed class AuthenticationMeter(Meter meter)
	{
		#region 常量定义
		private const string METER = nameof(Diagnostor.Authentication);
		#endregion

		#region 公共字段
		public readonly Counter<long> Authenticated = meter.CreateCounter<long>($"{METER}.{nameof(Authenticated)}");
		public readonly Counter<long> Authenticating = meter.CreateCounter<long>($"{METER}.{nameof(Authenticating)}");
		#endregion
	}
}
