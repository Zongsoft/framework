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

namespace Zongsoft.Data.Common;

/// <summary>
/// 表示数据驱动器的接口。
/// </summary>
public interface IDataDriver
{
	#region 属性定义
	/// <summary>获取数据驱动程序的名称。</summary>
	string Name { get; }

	/// <summary>获取支持的功能特性集。</summary>
	FeatureCollection Features { get; }

	/// <summary>获取数据记录读取器。</summary>
	IDataRecordGetter Getter { get; }

	/// <summary>获取数据参数设置器。</summary>
	IDataParameterSetter Setter { get; }

	/// <summary>获取数据语句构建器。</summary>
	Expressions.IStatementBuilder Builder { get; }
	#endregion

	#region 方法定义
	/// <summary>当发生一个错误的通知方法。</summary>
	/// <param name="context">发生异常的数据访问上下文。</param>
	/// <param name="exception">发生的异常对象。</param>
	/// <returns>返回的新异常，如果为空则忽略该异常。</returns>
	Exception OnError(IDataAccessContext context, Exception exception);

	/// <summary>创建一个数据命令对象。</summary>
	/// <returns>返回创建的数据命令对象。</returns>
	DbCommand CreateCommand();

	/// <summary>创建一个数据命令对象。</summary>
	/// <param name="text">指定的命令文本。</param>
	/// <param name="commandType">指定的命令类型。</param>
	/// <returns>返回创建的数据命令对象。</returns>
	DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text);

	/// <summary>创建一个数据命令对象。</summary>
	/// <param name="context">指定的数据访问上下文。</param>
	/// <param name="statement">指定的数据操作语句。</param>
	/// <returns>返回创建的数据命令对象。</returns>
	DbCommand CreateCommand(IDataAccessContextBase context, Expressions.IStatementBase statement);

	/// <summary>创建一个数据连接对象。</summary>
	/// <param name="connectionString">指定的连接字符串。</param>
	/// <returns>返回创建的数据连接对象，该连接对象的连接字符串为<paramref name="connectionString"/>参数值。</returns>
	DbConnection CreateConnection(string connectionString = null);

	/// <summary>创建一个数据连接构建器。</summary>
	/// <param name="connectionString">指定的连接字符串。</param>
	/// <returns>返回创建的数据连接构建器。</returns>
	DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null);

	/// <summary>创建一个数据导入器对象。</summary>
	/// <returns>返回创建的数据导入器对象。</returns>
	IDataImporter CreateImporter();
	#endregion
}
