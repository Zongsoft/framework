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

using MySql.Data.MySqlClient;

using Zongsoft.Reflection;
using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.MySql
{
	public class MySqlImporter : DataImporterBase
	{
		#region 构造函数
		public MySqlImporter(DataImportContextBase context) : base(context) { }
		#endregion

		#region 公共方法
		public override void Import(DataImportContext context)
		{
			var bulker = GetBulker(
				context.Entity.GetTableName(),
				Path.GetTempFileName(),
				(MySqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString),
				context.Options);

			//添加导入的列名
			bulker.Columns.AddRange(this.Members.Select(member => member.Name));

			using var file = File.OpenWrite(bulker.FileName);
			using var writer = new StreamWriter(file, System.Text.Encoding.UTF8);

			//写入表头（字段名列表）
			for(int i = 0; i < this.Members.Length; i++)
			{
				if(i > 0)
					writer.Write(" | ");

				writer.Write(this.Members[i].Name);
			}

			//写入表头与表体的分隔行符
			writer.Write(bulker.LineTerminator);

			//写入表体
			foreach(var item in context.Data)
			{
				var target = item;

				for(int i = 0; i < this.Members.Length; i++)
				{
					if(i > 0)
						writer.Write(bulker.FieldTerminator);

					var value = Reflector.GetValue(this.Members[i], ref target);

					if(bulker.FieldQuotationCharacter != '\0')
						writer.Write(bulker.FieldQuotationCharacter);

					if(value != null)
					{
						if(value is DateTime timestamp)
							writer.Write(timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
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

			context.Count = bulker.Load();

			//删除数据导入的临时文件
			DeleteFile(bulker.FileName);
		}

		public override async ValueTask ImportAsync(DataImportContext context, CancellationToken cancellation = default)
		{
			var bulker = GetBulker(
				context.Entity.GetTableName(),
				Path.GetTempFileName(),
				(MySqlConnection)context.Source.Driver.CreateConnection(context.Source.ConnectionString),
				context.Options);

			//添加导入的列名
			bulker.Columns.AddRange(this.Members.Select(member => member.Name));

			using var file = File.OpenWrite(bulker.FileName);
			using var writer = new StreamWriter(file, System.Text.Encoding.UTF8);

			//写入表头（字段名列表）
			for(int i = 0; i < this.Members.Length; i++)
			{
				if(i > 0)
					await writer.WriteAsync(" | ");

				await writer.WriteAsync(this.Members[i].Name);
			}

			//写入表头与表体的分隔行符
			await writer.WriteAsync(bulker.LineTerminator);

			//写入表体
			foreach(var item in context.Data)
			{
				var target = item;

				for(int i = 0; i < this.Members.Length; i++)
				{
					if(i > 0)
						await writer.WriteAsync(bulker.FieldTerminator);

					var value = Reflector.GetValue(this.Members[i], ref target);

					if(bulker.FieldQuotationCharacter != '\0')
						await writer.WriteAsync(bulker.FieldQuotationCharacter);

					if(value != null)
					{
						if(value is DateTime timestamp)
							await writer.WriteAsync(timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
						else
							await writer.WriteAsync(value.ToString());
					}

					if(bulker.FieldQuotationCharacter != '\0')
						await writer.WriteAsync(bulker.FieldQuotationCharacter);
				}

				//写入表体的分隔行符
				await writer.WriteAsync(bulker.LineTerminator);
			}

			await writer.FlushAsync();
			writer.Close();
			file.Dispose();

			context.Count = await bulker.LoadAsync(cancellation);

			//删除数据导入的临时文件
			DeleteFile(bulker.FileName);
		}
		#endregion

		#region 私有方法
		private static MySqlBulkLoader GetBulker(string name, string filePath, MySqlConnection connection, IDataImportOptions options) => new MySqlBulkLoader(connection)
		{
			TableName = name,
			FileName = filePath,
			CharacterSet = "UTF8",
			LineTerminator = "\n",
			FieldTerminator = ",",
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
}
