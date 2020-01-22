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

namespace Zongsoft.Diagnostics
{
	public class LoggerHandler : Zongsoft.Services.CommandBase
	{
		#region 成员字段
		private ILogger _logger;
		#endregion

		#region 构造函数
		public LoggerHandler(string name, ILogger logger = null, LoggerHandlerPredication predication = null) : base(name)
		{
			_logger = logger;
			this.Predication = predication ?? new LoggerHandlerPredication();
		}
		#endregion

		#region 公共属性
		public ILogger Logger
		{
			get
			{
				return _logger;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_logger = value;
			}
		}
		#endregion

		#region 重写方法
		protected override bool CanExecute(object parameter)
		{
			return parameter is LogEntry && this.Logger != null && base.CanExecute(parameter);
		}

		protected override object OnExecute(object parameter)
		{
			var logger = this.Logger;

			if(logger != null)
				logger.Log(parameter as LogEntry);

			return null;
		}
		#endregion

		#region 公共方法
		public void Handle(LogEntry entry)
		{
			this.Execute(entry);
		}
		#endregion
	}
}
