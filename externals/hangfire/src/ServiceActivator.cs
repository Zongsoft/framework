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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Hangfire library.
 *
 * The Zongsoft.Externals.Hangfire is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Hangfire is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Hangfire library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Hangfire;

namespace Zongsoft.Externals.Hangfire
{
	public class ServiceActivator : JobActivator
	{
		public override object ActivateJob(Type type) => Zongsoft.Services.ApplicationContext.Current.Services.GetService(type);
		public override JobActivatorScope BeginScope(JobActivatorContext context) => new SingletonScope(this);

		private class SingletonScope : JobActivatorScope
		{
			private readonly JobActivator _activator;
			public SingletonScope(JobActivator activator) => _activator = activator ?? throw new ArgumentNullException(nameof(activator));
			public override object Resolve(Type type) => _activator.ActivateJob(type);
		}
	}
}
