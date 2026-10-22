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
	protected override async ValueTask OnImportAsync(DataImportContext context, MemberCollection members, CancellationToken cancellation = default)
	{
		await using var lease = await context.Session.AcquireLeaseAsync(context.Options.TransactionSuppressed, cancellation);
		var filePath = Path.GetTempFileName();

		try
		{
			var bulker = GetBulker(
				context.Entity.GetTableName(),
				filePath,
				(MySqlConnection)lease.Connection,
				context.Options);

			//添加导入的列名（注：待 MySql.Data 修复后可去掉对字段名的反引号`标注）
			bulker.Columns.AddRange(members.Select(member => $"`{member.Name}`"));

			await using(var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous))
			await using(var writer = new StreamWriter(file, System.Text.Encoding.UTF8))
			{
				//写入表头（字段名列表）
				for(int i = 0; i < members.Count; i++)
				{
					if(i > 0)
						await writer.WriteAsync((" | ").AsMemory(), cancellation).ConfigureAwait(false);

					await writer.WriteAsync((members[i].Name).AsMemory(), cancellation).ConfigureAwait(false);
				}

				//写入表头与表体的分隔行符
				await writer.WriteAsync((bulker.LineTerminator).AsMemory(), cancellation).ConfigureAwait(false);

				//写入表体
				await foreach(var item in GetRecordsAsync(context).WithCancellation(cancellation).ConfigureAwait(false))
				{
					var target = item;

					for(int i = 0; i < members.Count; i++)
					{
						if(i > 0)
							await writer.WriteAsync((bulker.FieldTerminator).AsMemory(), cancellation).ConfigureAwait(false);

						var prepared = await members[i].GetValueAsync(target, cancellation).ConfigureAwait(false);
						target = prepared.Target;
						var value = prepared.Value;

						if(bulker.FieldQuotationCharacter != '\0')
							await writer.WriteAsync((bulker.FieldQuotationCharacter.ToString()).AsMemory(), cancellation).ConfigureAwait(false);

						if(value is null)
							await writer.WriteAsync((@"\N").AsMemory(), cancellation).ConfigureAwait(false);
						else
						{
							if(value is DateTime timestamp)
								await writer.WriteAsync((timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffK")).AsMemory(), cancellation).ConfigureAwait(false);
							else if(value is bool boolean)
								await writer.WriteAsync((boolean ? "1" : "0").AsMemory(), cancellation).ConfigureAwait(false);
							else if(value is string text)
								await writer.WriteAsync((text.ReplaceLineEndings(string.Empty)).AsMemory(), cancellation).ConfigureAwait(false);
							else
								await writer.WriteAsync((value.ToString()).AsMemory(), cancellation).ConfigureAwait(false);
						}

						if(bulker.FieldQuotationCharacter != '\0')
							await writer.WriteAsync((bulker.FieldQuotationCharacter.ToString()).AsMemory(), cancellation).ConfigureAwait(false);
					}

					//写入表体的分隔行符
					await writer.WriteAsync((bulker.LineTerminator).AsMemory(), cancellation).ConfigureAwait(false);
				}

				await writer.FlushAsync(cancellation);
			}

			context.Count = await bulker.LoadAsync(cancellation).ConfigureAwait(false);
		}
		finally
		{
			DeleteFile(filePath);
		}
	}
	#endregion

	#region 私有方法
	private static MySqlBulkLoader GetBulker(string name, string filePath, MySqlConnection connection, IDataImportOptions options) => new MySqlBulkLoader(connection)
	{
		TableName = $"`{name.Replace("`", "``")}`",
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
