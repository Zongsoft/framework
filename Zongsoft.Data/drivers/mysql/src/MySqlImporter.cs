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
 * Copyright (C) 2015-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.MySql library.
 *
 * The Zongsoft.Data.MySql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MySql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MySql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using MySqlConnector;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.MySql;

public class MySqlImporter : DataImporterBase
{
	#region 公共方法
	protected override void OnImport(DataImportContext context, MemberCollection members)
	{
		var bulker = GetBulker(
			context.Entity.GetTableName(),
			Path.GetTempFileName(),
			(MySqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString),
			context.Options);

		//添加导入的列名（注：待 MySql.Data 修复后可去掉对字段名的反引号`标注）
		bulker.Columns.AddRange(members.Select(member => $"`{member.Name}`"));

		using var file = File.OpenWrite(bulker.FileName);
		using var writer = new StreamWriter(file, System.Text.Encoding.UTF8);

		//写入表头（字段名列表）
		for(int i = 0; i < members.Count; i++)
		{
			if(i > 0)
				writer.Write(" | ");

			writer.Write(members[i].Name);
		}

		//写入表头与表体的分隔行符
		writer.Write(bulker.LineTerminator);

		//写入表体
		foreach(var item in context.Data)
		{
			var target = item;

			for(int i = 0; i < members.Count; i++)
			{
				if(i > 0)
					writer.Write(bulker.FieldTerminator);

				var value = members[i].GetValue(ref target);

				if(bulker.FieldQuotationCharacter != '\0')
					writer.Write(bulker.FieldQuotationCharacter);

				if(value is null)
					writer.Write(@"\N");
				else
				{
					if(value is DateTime timestamp)
						writer.Write(timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
					else if(value is bool boolean)
						writer.Write(boolean ? "1" : "0");
					else if(value is string text)
						writer.Write(text.ReplaceLineEndings(string.Empty));
					else
						writer.Write(value);
				}

				if(bulker.FieldQuotationCharacter != '\0')
					writer.Write(bulker.FieldQuotationCharacter);
			}

			//写入表体的分隔行符
			writer.Write(bulker.LineTerminator);
		}

		writer.Flush();
		writer.Close();
		file.Dispose();

		try
		{
			context.Count = bulker.Load();
		}
		finally
		{
			//删除数据导入的临时文件
			DeleteFile(bulker.FileName);
		}
	}

	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		var bulker = GetBulker(
			context.Entity.GetTableName(),
			Path.GetTempFileName(),
			(MySqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString),
			context.Options);

		//添加导入的列名（注：待 MySql.Data 修复后可去掉对字段名的反引号`标注）
		bulker.Columns.AddRange(members.Select(member => $"`{member.Name}`"));

		using var file = File.OpenWrite(bulker.FileName);
		using var writer = new StreamWriter(file, System.Text.Encoding.UTF8);

		//写入表头（字段名列表）
		for(int i = 0; i < members.Count; i++)
		{
			if(i > 0)
				await writer.WriteAsync(" | ");

			await writer.WriteAsync(members[i].Name);
		}

		//写入表头与表体的分隔行符
		await writer.WriteAsync(bulker.LineTerminator);

		//写入表体
		foreach(var item in context.Data)
		{
			var target = item;

			for(int i = 0; i < members.Count; i++)
			{
				if(i > 0)
					await writer.WriteAsync(bulker.FieldTerminator);

				var value = members[i].GetValue(ref target);

				if(bulker.FieldQuotationCharacter != '\0')
					await writer.WriteAsync(bulker.FieldQuotationCharacter);

				if(value is null)
					writer.Write(@"\N");
				else
				{
					if(value is DateTime timestamp)
						await writer.WriteAsync(timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffK"));
					else if(value is bool boolean)
						await writer.WriteAsync(boolean ? "1" : "0");
					else if(value is string text)
						writer.Write(text.ReplaceLineEndings(string.Empty));
					else
						await writer.WriteAsync(value.ToString());
				}

				if(bulker.FieldQuotationCharacter != '\0')
					await writer.WriteAsync(bulker.FieldQuotationCharacter);
			}

			//写入表体的分隔行符
			await writer.WriteAsync(bulker.LineTerminator);
		}

		await writer.FlushAsync(cancellation);
		writer.Close();
		file.Dispose();

		try
		{
			context.Count = await bulker.LoadAsync(cancellation);
		}
		finally
		{
			//删除数据导入的临时文件
			DeleteFile(bulker.FileName);
		}
	}
	#endregion

	#region 私有方法
	private static MySqlBulkLoader GetBulker(string name, string filePath, MySqlConnection connection, IDataImportOptions options) => new MySqlBulkLoader(connection)
	{
		TableName = name,
		FileName = filePath,
		CharacterSet = "UTF8",
		LineTerminator = "\n",
		FieldTerminator = "</>",
		NumberOfLinesToSkip = 1,
		Local = true,
		ConflictOption = options.ConstraintIgnored ? MySqlBulkLoaderConflictOption.Ignore : MySqlBulkLoaderConflictOption.None,
	};

	private static void DeleteFile(string filePath)
	{
		try
		{
			File.Delete(filePath);
		}
		catch { }
	}
	#endregion
}