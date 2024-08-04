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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Communication
{
	public class TransmitterHandler(IServiceProvider serviceProvider) : HandlerBase<TransmitterHandler.Argument>, IMatchable, IMatchable<string>
	{
		#region 成员字段
		private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		#endregion

		#region 公共方法
		protected override ValueTask OnHandleAsync(object caller, Argument argument, IDictionary<string, object> parameters, CancellationToken cancellation)
		{
			if(argument == null)
				return ValueTask.CompletedTask;

			//获取指定名称的发送器
			var transmitter = _serviceProvider.ResolveRequired<ITransmitter>(argument.Name);

			//获取指定的发送通道
			if(!transmitter.Descriptor.Channels.TryGetValue(argument.Channel ?? string.Empty, out var channel))
				channel = transmitter.Descriptor.Channels.Count > 0 ? transmitter.Descriptor.Channels[0] : null;

			//如果没有指定参数对象则尝试通过参数转换器将参数集转换成发送模板的参数对象
			if(argument.Parameter == null)
			{
				//获取指定的发送器参数转换器
				var argumenter = _serviceProvider.Resolve<ITransmitterArgumenter>(argument);

				if(argumenter != null)
					argument.Parameter = argumenter.GetArgument(transmitter, channel?.Name ?? argument.Channel, argument.Template, parameters);
				else
					argument.Parameter = parameters;
			}

			//执行发送任务
			return transmitter.TransmitAsync(argument.Destination, argument.Channel, argument.Template, argument.Parameter, cancellation);
		}
		#endregion

		#region 服务匹配
		bool IMatchable.Match(object parameter) => parameter is string name && string.Equals(name, "Transmitter", StringComparison.OrdinalIgnoreCase);
		bool IMatchable<string>.Match(string parameter) => string.Equals(parameter, "Transmitter", StringComparison.OrdinalIgnoreCase);
		#endregion

		#region 嵌套子类
		/// <summary>
		/// 表示发送处理器参数的类。
		/// </summary>
		public class Argument
		{
			#region 构造函数
			public Argument() { }
			public Argument(string name, string template, object parameter, string destination) : this(name, null, template, parameter, destination) { }
			public Argument(string name, string channel, string template, object parameter, string destination)
			{
				this.Name = name;
				this.Channel = channel;
				this.Template = template;
				this.Parameter = parameter;
				this.Destination = destination;
			}
			#endregion

			/// <summary>获取或设置发送器的名称。</summary>
			public string Name { get; set; }
			/// <summary>获取或设置发送通道。</summary>
			public string Channel { get; set; }
			/// <summary>获取或设置发送模板。</summary>
			public string Template { get; set; }
			/// <summary>获取或设置发送模板的参数对象。</summary>
			public object Parameter { get; set; }
			/// <summary>获取或设置发送目的地。</summary>
			public string Destination { get; set; }
		}
		#endregion
	}
}