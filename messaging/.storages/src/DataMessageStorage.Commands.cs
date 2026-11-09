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
using System.Collections.Generic;

namespace Zongsoft.Messaging.Storages;

partial class DataMessageStorage
{
	private sealed class CommandSet
	{
		#region 静态字段
		private static readonly HashSet<string> _drivers = new(StringComparer.OrdinalIgnoreCase)
		{
			"MsSql",
			"MySql",
			"SQLite",
			"PostgreSql",
		};

		private static readonly CommandSet _instance = new("Messaging.Storages");
		#endregion

		#region 私有构造
		private CommandSet(string @namespace)
		{
			this.Set = $"{@namespace}.Set";
			this.Get = $"{@namespace}.Get";
			this.GetByTopic = $"{@namespace}.GetByTopic";
			this.Remove = $"{@namespace}.Remove";
			this.Clear = $"{@namespace}.Clear";
			this.ClearByTopic = $"{@namespace}.ClearByTopic";
		}
		#endregion

		#region 公共属性
		public string Set { get; }
		public string Get { get; }
		public string GetByTopic { get; }
		public string Remove { get; }
		public string Clear { get; }
		public string ClearByTopic { get; }
		#endregion

		#region 静态方法
		public static CommandSet Resolve(string driver) =>
			driver != null && _drivers.Contains(driver) ? _instance :
			throw new NotSupportedException(string.Format(Properties.Resources.DataMessageStorage_DriverUnsupported_Message, driver));
		#endregion
	}
}
