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
 * The MIT License (MIT)
 * 
 * Copyright (C) 2020-2026 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Upgrading;

partial class Packager
{
	[CommandOption(ALGORITHM_OPTION, 'a', typeof(ChecksumAlgorithm), ChecksumAlgorithm.Sha1)]
	public sealed class ChecksumCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string ALGORITHM_OPTION = "algorithm";
		#endregion

		#region 重写方法
		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments.IsEmpty)
			{
				Terminal.WriteLine(CommandOutletColor.Red, Properties.Resources.MissingRequiredArgments);
				return null;
			}

			var algorithm = context.Options.GetValue<ChecksumAlgorithm>(ALGORITHM_OPTION);
			var result = new string[context.Arguments.Count];

			for(int i = 0; i < context.Arguments.Count; i++)
			{
				//获取升级包文件的路径
				var path = Path.GetExtension(context.Arguments[i]).ToLowerInvariant() switch
				{
					EXTENSION => context.Arguments[i],
					Manifest.FILE_NAME => Path.ChangeExtension(context.Arguments[i], EXTENSION),
					_ => $"{context.Arguments[i]}{EXTENSION}",
				};

				//检查升级包文件是否存在
				if(!File.Exists(path))
				{
					Terminal.WriteLine(CommandOutletColor.Red, $"The file '{path}' does not exist.");
					continue;
				}

				//获取升级清单文件的路径
				result[i] = Path.ChangeExtension(path, Manifest.FILE_NAME);

				//检查升级清单文件是否存在
				if(!File.Exists(result[i]))
				{
					Terminal.WriteLine(CommandOutletColor.Red, $"The manifest file '{result[i]}' does not exist.");
					result[i] = null;
					continue;
				}

				//以异步方式打开升级清单文件
				using var stream = new FileStream(result[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 1024 * 4, true);

				//读取升级清单文件
				var release = await Release.ReadAsync(stream, cancellation);

				//释放升级清单文件
				stream.Dispose();

				//计算并设置升级包的校验码
				release.Checksum = Common.Checksum.Compute(algorithm.ToString(), File.OpenRead(path));

				//保存升级清单文件
				release.Save(result[i]);

				//输出提示信息
				Terminal.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.ChecksumSuccessfully_Message, path, result[i]));
			}

			return result.Length == 1 ? result[0] : result;
		}
		#endregion
	}

	/// <summary>表示校验码算法的枚举。</summary>
	public enum ChecksumAlgorithm
	{
		Sha1,
		Sha256,
		Sha384,
		Sha512,
	}
}
