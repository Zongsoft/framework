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

namespace Zongsoft.Data.Common
{
	public abstract class DataDriverBase : IDataDriver
	{
		#region 成员字段
		private readonly FeatureCollection _features;
		private readonly VisitorPool _visitors;
		#endregion

		#region 构造函数
		protected DataDriverBase()
		{
			//创建访问器对象池
			_visitors = new VisitorPool(this.CreateVisitor);

			//创建功能特性集合
			_features = new FeatureCollection();
		}
		#endregion

		#region 公共属性
		public abstract string Name
		{
			get;
		}

		public FeatureCollection Features
		{
			get
			{
				return _features;
			}
		}

		public abstract Expressions.IStatementBuilder Builder
		{
			get;
		}
		#endregion

		#region 公共方法
		public virtual Exception OnError(Exception exception)
		{
			return exception;
		}

		public virtual DbCommand CreateCommand()
		{
			return this.CreateCommand(null, CommandType.Text);
		}

		public virtual DbCommand CreateCommand(Expressions.IStatementBase statement)
		{
			if(statement == null)
				throw new ArgumentNullException(nameof(statement));

			//创建指定语句的数据命令
			var command = this.CreateCommand(this.Script(statement), CommandType.Text);

			//设置数据命令的参数集
			if(statement.HasParameters)
			{
				foreach(var parameter in statement.Parameters)
				{
					//通过命令创建一个新的空参数
					var dbParameter = command.CreateParameter();

					//设置参数对象的各属性的初始值
					//注意：不能设置参数的DbType属性，因为不同数据提供程序可能因为不支持特定类型而导致异常
					dbParameter.ParameterName = parameter.Name;
					dbParameter.Direction = parameter.Direction;
					dbParameter.Value = parameter.Value ?? DBNull.Value;

					//设置命令参数各属性
					this.SetParameter(dbParameter, parameter);

					//将参数加入到命令的参数集中
					command.Parameters.Add(dbParameter);
				}
			}

			return command;
		}

		public abstract DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text);

		public virtual DbConnection CreateConnection()
		{
			return this.CreateConnection(string.Empty);
		}

		public abstract DbConnection CreateConnection(string connectionString);
		#endregion

		#region 保护方法
		protected abstract Expressions.IExpressionVisitor CreateVisitor();

		protected virtual void SetParameter(DbParameter parameter, Expressions.ParameterExpression expression)
		{
			parameter.DbType = expression.DbType;
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string Script(Expressions.IStatementBase statement)
		{
			//从对象池中获取一个访问器
			var visitor = _visitors.GetObject();

			try
			{
				//访问指定的语句
				visitor.Visit(statement);

				//输出访问器的脚本内容
				return visitor.Output.ToString();
			}
			finally
			{
				//将使用完成的访问器释放回对象池
				_visitors.Release(visitor);
			}
		}
		#endregion

		#region 嵌套子类
		private class VisitorPool : Zongsoft.Collections.ObjectPool<Expressions.IExpressionVisitor>
		{
			#region 内部构造
			internal VisitorPool(Func<Expressions.IExpressionVisitor> creator) : base(creator, null)
			{
			}
			#endregion

			#region 重写方法
			protected override void OnTakein(Expressions.IExpressionVisitor visitor)
			{
				//清空访问器的脚本缓冲区内容
				visitor.Output.Clear();

				//调用基类同名方法
				base.OnTakein(visitor);
			}
			#endregion
		}
		#endregion
	}
}
