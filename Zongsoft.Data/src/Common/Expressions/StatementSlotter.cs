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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Data.Common;
using System.Text.RegularExpressions;

namespace Zongsoft.Data.Common.Expressions
{
	public class StatementSlotter : IStatementSlotEvaluator
	{
		#region 静态字段
		private static readonly Regex _regex = new(@"\$\{(?<placeholder>[a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline);
		#endregion

		#region 公共属性
		public IStatementSlotEvaluator Evaluator { get; set; }
		#endregion

		#region 公共方法
		public void Evaluate(IDataAccessContext context, IStatementBase statement, DbCommand command)
		{
			_regex.Replace(command.CommandText, match => this.Evaluate(context, statement, this.GetSlot(context, statement, match.Groups["placeholder"].Value)));
		}
		#endregion

		#region 虚拟方法
		protected virtual StatementSlot GetSlot(IDataAccessContext context, IStatementBase statement, string placeholder) => statement.Slots.TryGetValue(placeholder, out var slot) ? slot : null;
		protected virtual string Evaluate(IDataAccessContext context, IStatementBase statement, StatementSlot slot) => this.Evaluator?.Evaluate(context, statement, slot);
		#endregion

		#region 显式实现
		string IStatementSlotEvaluator.Evaluate(IDataAccessContext context, IStatementBase statement, StatementSlot slot) => this.Evaluate(context, statement, slot);
		#endregion
	}
}