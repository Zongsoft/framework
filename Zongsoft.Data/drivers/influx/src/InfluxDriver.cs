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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.Influx library.
 *
 * The Zongsoft.Data.Influx is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.Influx is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.Influx library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

using InfluxDB3.Client;
using InfluxDB3.Client.Config;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Influx
{
	public class InfluxDriver : DataDriverBase
	{
		#region 公共常量
		/// <summary>驱动程序的标识：Influx。</summary>
		public const string NAME = "Influx";
		#endregion

		#region 单例字段
		public static readonly InfluxDriver Instance = new();
		#endregion

		#region 私有构造
		private InfluxDriver() => this.Features.Add(Feature.TransactionSuppressed);
		#endregion

		#region 公共属性
		public override string Name => NAME;
		public override IStatementBuilder Builder => InfluxStatementBuilder.Default;
		#endregion

		#region 公共方法
		public override Exception OnError(Exception exception)
		{
			return exception;
		}

		public override DbCommand CreateCommand() => new Common.InfluxCommand();
		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new Common.InfluxCommand()
		{
			CommandText = text,
			CommandType = commandType,
		};

		public override DbConnection CreateConnection(string connectionString = null) => new Common.InfluxConnection(connectionString ?? string.Empty);
		public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) =>
			Configuration.InfluxConnectionSettingsDriver.Instance.GetSettings(connectionString).GetOptions();

		public override IDataImporter CreateImporter() => new InfluxImporter();
		#endregion

		#region 保护方法
		protected override ExpressionVisitorBase CreateVisitor() => new InfluxExpressionVisitor();
		protected override StatementSlotter CreateSlotter() => new() { Evaluator = InfluxStatementSlotEvaluator.Instance };
		#endregion
	}
}
