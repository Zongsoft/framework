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

namespace Zongsoft.Terminals;

partial class Terminal
{
	public class ExitException : ApplicationException
	{
		#region 构造函数
		public ExitException() { }
		public ExitException(int exitCode, string message = null) : base(message)
		{
			this.ExitCode = exitCode;
		}
		#endregion

		#region 公共属性
		public int ExitCode { get; }
		#endregion
	}

	public class ExitEventArgs : EventArgs
	{
		#region 构造函数
		public ExitEventArgs(int exitCode) : this(exitCode, false) { }
		public ExitEventArgs(int exitCode, bool cancel)
		{
			this.Cancel = cancel;
			this.ExitCode = exitCode;
		}
		#endregion

		#region 公共属性
		public bool Cancel { get; }
		public int ExitCode { get; }
		#endregion
	}
}
