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
using System.Collections.Generic;

using Zongsoft.Configuration;

namespace Zongsoft.Diagnostics
{
	public class LoggerInitializer : Services.IApplicationInitializer, IDisposable
	{
		#region 公共方法
		public virtual void Initialize(Services.IApplicationContext context)
		{
			if(context == null)
				return;

			//从当前应用的主配置文件中获取日志器的主配置节
			var options = context.Configuration.GetOption<Configuration.LoggerOptions>(@"/Diagnostics/Logger");

			if(options == null)
				return;

			foreach(var handlerSetting in options.Handlers)
			{
				var type = Type.GetType(handlerSetting.Type, true, true);

				//如果当前处理节配置的日志处理器类型不是一个记录器则抛出异常
				if(!typeof(ILogger).IsAssignableFrom(type))
					throw new InvalidOperationException(string.Format("The '{0}' type isn't a Logger.", type.FullName));

				//获取日志记录器实现类的带参构造函数
				var constructor = type.GetConstructor(new Type[] { typeof(Configuration.LoggerOptions.LoggerHandlerSetting) });
				ILogger instance;

				//试图创建日志记录器实例
				if(constructor == null)
					instance = (ILogger)Activator.CreateInstance(type);
				else
					instance = (ILogger)Activator.CreateInstance(type, handlerSetting);

				//如果日志记录器实例创建失败则抛出异常
				if(instance == null)
					throw new InvalidOperationException(string.Format("Can not create instance of '{0}' type.", type));

				//如果日志记录器配置节含有扩展属性，则设置日志记录器实例的扩展属性
				if(handlerSetting.HasProperties)
				{
					foreach(var property in handlerSetting.Properties)
					{
						Reflection.Reflector.SetValue(instance, property.Key, property.Value);
					}
				}

				LoggerHandlerPredication predication = null;

				if(handlerSetting.Predication != null)
				{
					predication = new LoggerHandlerPredication()
					{
						Source = handlerSetting.Predication.Source,
						MaxLevel = handlerSetting.Predication.MaxLevel,
						MinLevel = handlerSetting.Predication.MinLevel,
					};
				}

				Logger.Handlers.Add(new LoggerHandler(handlerSetting.Name, instance, predication));
			}
		}
		#endregion

		#region 处置方法
		void IDisposable.Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
		}
		#endregion
	}
}
