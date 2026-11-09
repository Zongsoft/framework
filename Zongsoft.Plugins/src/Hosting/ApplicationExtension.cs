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
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Hosting;

public static class ApplicationExtension
{
	public static async ValueTask<TApplication> InitializeAsync<TApplication>(this TApplication app) where TApplication : IHost
	{
		try
		{
			app?.Services.GetRequiredService<PluginApplicationContext>().Initialize();
			return app;
		}
		catch(Exception exception)
		{
			try
			{
				if(app is IAsyncDisposable asynchronous)
					await asynchronous.DisposeAsync().ConfigureAwait(false);
				else
					app?.Dispose();
			}
			catch(Exception cleanup) { throw new AggregateException(exception, cleanup); }

			throw;
		}
	}
}
