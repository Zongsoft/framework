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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.Mqtt library.
 *
 * The Zongsoft.Messaging.Mqtt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Mqtt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Mqtt library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Diagnostics;

using MQTTnet.Client;
using MQTTnet.Diagnostics;

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
