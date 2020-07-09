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

using Zongsoft.Services;

namespace Zongsoft.Flowing
{
	public class StateHandlerProvider : IStateHandlerProvider
	{
		#region 单例字段
		public static readonly StateHandlerProvider Default = new StateHandlerProvider();
		#endregion

		#region 私有构造
		private StateHandlerProvider()
		{
		}
		#endregion

		#region 公共方法
		public IEnumerable<IStateHandler<T>> GetHandlers<T>() where T : struct, IEquatable<T>
		{
			var services = ApplicationContext.Current?.Services;

			if(services != null)
			{
				var handlers = services.Resolve<IEnumerable<IStateHandler<T>>>();

				foreach(var handler in handlers)
					yield return handler;
			}

			var modules = ApplicationContext.Current?.Modules;

			if(modules != null)
			{
				foreach(var module in modules)
				{
					var handlers = module.Services.Resolve<IEnumerable<IStateHandler<T>>>();

					foreach(var handler in handlers)
						yield return handler;
				}
			}
		}
		#endregion
	}
}
