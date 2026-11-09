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

using Zongsoft.Diagnostics;

using MQTTnet;
using MQTTnet.Diagnostics.Logger;

namespace Zongsoft.Messaging.Mqtt;

internal sealed class MqttLogger : IMqttNetLogger
{
	#region 单例字段
	public static readonly MqttLogger Instance = new();
	#endregion

	#region 私有变量
	private readonly Logging _logging = Logging.GetLogging<IMqttClient>();
	#endregion

	#region 接口实现
	public bool IsEnabled => true;
	public void Publish(MqttNetLogLevel level, string source, string message, object[] parameters, Exception exception)
	{
		var data = parameters == null ? null : (parameters.Length == 1 ? parameters[0] : parameters);

		if(!string.IsNullOrEmpty(source))
			message = $"[{source}] {message}";

		_logging.Log(level switch
		{
			MqttNetLogLevel.Verbose => LogLevel.Trace,
			MqttNetLogLevel.Info => LogLevel.Info,
			MqttNetLogLevel.Warning => LogLevel.Warn,
			MqttNetLogLevel.Error => LogLevel.Error,
			_ => LogLevel.Debug,
		}, message, exception, data);
	}
	#endregion
}
