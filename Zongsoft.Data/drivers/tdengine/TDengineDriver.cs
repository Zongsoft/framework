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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;

using TDengine.Driver;
using TDengine.Data.Client;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine
{
	public class TDengineDriver : DataDriverBase
	{
		#region 公共常量
		/// <summary>驱动程序的标识：TDengine。</summary>
		public const string NAME = "TDengine";
		#endregion

		#region 构造函数
		public TDengineDriver() => this.Features.Add(Feature.TransactionSuppressed);
		#endregion

		#region 公共属性
		public override string Name => NAME;
		public override IStatementBuilder Builder => TDengineStatementBuilder.Default;
		#endregion

		#region 公共方法
		public override Exception OnError(Exception exception)
		{
			if(exception is TDengineError error)
			{
				switch(error.Code)
				{
					case 0:
						break;
				}
			}

			return exception;
		}

		public override DbCommand CreateCommand() => new TDengineCommand();
		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => new TDengineCommand()
		{
			CommandText = text,
			CommandType = commandType,
		};

		public override DbConnection CreateConnection() => new TDengineConnection(string.Empty);
		public override DbConnection CreateConnection(string connectionString) => new TDengineConnection(connectionString)
		{
			ConnectionStringBuilder = Configuration.TDengineConnectionSettingsDriver.Instance.Create(connectionString).Model<TDengineConnectionStringBuilder>()
		};

		public override IDataImporter CreateImporter() => new TDengineImporter();
		#endregion

		#region 保护方法
		protected override ExpressionVisitorBase CreateVisitor() => new TDengineExpressionVisitor();
		protected override StatementSlotter CreateSlotter() => new() { Evaluator = TDengineStatementSlotEvaluator.Instance };
		#endregion
	}
}
