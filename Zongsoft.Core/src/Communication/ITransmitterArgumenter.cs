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

namespace Zongsoft.Communication;

/// <summary>提供发送器模板参数转换功能的接口。</summary>
public interface ITransmitterArgumenter
{
	/// <summary>将指定的数据转换为模板参数。</summary>
	/// <param name="transmitter">指定的发送器对象。</param>
	/// <param name="channel">指定的发送通道。</param>
	/// <param name="template">指定的模板名称。</param>
	/// <param name="data">指定的待转换的参数对象。</param>
	/// <param name="parameters">指定的参数集合。</param>
	/// <returns>返回转换后的对象。</returns>
	object GetArgument(ITransmitter transmitter, string channel, string template, object data, Collections.Parameters parameters);
}