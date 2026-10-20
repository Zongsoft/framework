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
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Terminals;

partial class Terminal
{
	partial class ConsoleExecutor
	{
		#region 运行方法
		public Task<int> RunAsync(CancellationToken cancellation = default) => this.RunAsync((CommandOutletContent)null, cancellation);
		public Task<int> RunAsync(string splash, CancellationToken cancellation = default) => this.RunAsync(CommandOutletContent.Create(splash), cancellation);
		public async Task<int> RunAsync(CommandOutletContent splash, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();

			if(Interlocked.CompareExchange(ref _running, 1, 0) != 0)
				throw new InvalidOperationException(Properties.Resources.Terminal_ExecutorRunning_Message);

			try { return await this.RunCoreAsync(splash, _terminal.Input, cancellation).ConfigureAwait(false); }
			finally { Volatile.Write(ref _running, 0); }
		}

		private async Task<int> RunCoreAsync(CommandOutletContent splash, TextReader input, CancellationToken cancellation)
		{
			if(this.Root.Children.Count < 1)
				return 0;

			//初始化终端文本编码方案
			_terminal.Encoding = System.Text.Encoding.UTF8;

			_terminal.WriteLine(splash == null || splash.IsEmpty ? CreateSplash() : splash);

			while(true)
			{
				//如果已经取消运行则退出
				if(cancellation.IsCancellationRequested)
					return 0;

				//重置控制台，准备执行命令
				_terminal.Reset();

				string commandText;

				try { commandText = await input.ReadLineAsync(cancellation).ConfigureAwait(false); }
				catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { return 0; }

				if(commandText == null)
					return 0;

				if(string.IsNullOrWhiteSpace(commandText))
					continue;

				try { await this.ExecuteAsync(commandText, null, cancellation).ConfigureAwait(false); }
				catch(OperationCanceledException)
				{
					return 0;
				}
				catch(ExitException ex)
				{
					var args = new ExitEventArgs(ex.ExitCode);
					this.OnExit(args);

					if(!args.Cancel)
						return ex.ExitCode;
				}
				catch(Exception ex) { WriteError(ex); }
			}

			static CommandOutletContent CreateSplash()
			{
				var content = CommandOutletContent.Create(SPLASH);
				var application = ApplicationContext.Current;
				if(application == null)
					return content;

				content.Last
					.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
					.AppendLine(CommandOutletColor.DarkCyan, Common.StringExtension.Justify(application.Identifier.ToString(), 50))
					.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

				return content;
			}

			void WriteError(Exception exception)
			{
				var content = CommandOutletContent.Create(CommandOutletColor.DarkRed, Properties.Resources.ErrorOccurred_Label)
					.Append(CommandOutletColor.DarkGray, '[')
					.Append(CommandOutletColor.DarkMagenta, exception.GetType().Name)
					.Append(CommandOutletColor.DarkGray, "] ")
					.Append(CommandOutletColor.DarkRed, exception.Message);

				_terminal.WriteLine(content);
			}
		}
		#endregion
	}
}
