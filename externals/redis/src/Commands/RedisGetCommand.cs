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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Externals.Redis.Commands
{
	[DisplayName("Text.RedisGetCommand.Name")]
	[Description("Text.RedisGetCommand.Description")]
	public class RedisGetCommand : Zongsoft.Services.CommandBase<Zongsoft.Services.CommandContext>
	{
		#region 构造函数
		public RedisGetCommand() : base("Get")
		{
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(Services.CommandContext context)
		{
			if(context.Expression.Arguments.Length < 1)
				throw new Zongsoft.Services.CommandException("Missing arguments.");

			int index = 0;
			var redis = RedisCommand.GetRedis(context.CommandNode);
			var result = new List<object>(context.Expression.Arguments.Length);

			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				var entry = redis.GetValue(context.Expression.Arguments[i], out var expiry);

				if(entry == null)
				{
					context.Output.WriteLine(Services.CommandOutletColor.Red, $"The '{context.Expression.Arguments[i]}' entry is not existed.");
				}
				else
				{
					result.Add(entry);

					var entryType = redis.GetEntryType(context.Expression.Arguments[i]);
					context.Output.Write(Services.CommandOutletColor.DarkGray, $"[{entryType}] ");

					if(expiry.HasValue)
						context.Output.Write(Services.CommandOutletColor.DarkCyan, expiry.Value.ToString() + " ");

					switch(entryType)
					{
						case RedisEntryType.String:
							context.Output.WriteLine(entry);
							break;
						case RedisEntryType.Dictionary:
							context.Output.WriteLine(Services.CommandOutletColor.DarkYellow, $"The '{context.Expression.Arguments[i]}' dictionary have {((IDictionary)entry).Count} entries.");

							foreach(DictionaryEntry item in (IDictionary)entry)
							{
								context.Output.Write(Services.CommandOutletColor.Gray, $"[{(++index).ToString()}] ");
								context.Output.Write(Services.CommandOutletColor.DarkGreen, item.Key.ToString());
								context.Output.Write(Services.CommandOutletColor.Cyan, " : ");
								context.Output.WriteLine(Services.CommandOutletColor.DarkGreen, item.Value.ToString());
							}

							break;
						case RedisEntryType.List:
							context.Output.WriteLine(Services.CommandOutletColor.DarkYellow, $"The '{context.Expression.Arguments[i]}' list(queue) have {((Zongsoft.Collections.IQueue)entry).Count} entries.");

							foreach(object item in (IEnumerable)entry)
							{
								context.Output.Write(Services.CommandOutletColor.Gray, $"[{(++index).ToString()}] ");

								if(item == null)
									context.Output.WriteLine(Services.CommandOutletColor.DarkGray, "NULL");
								else
									context.Output.WriteLine(Services.CommandOutletColor.DarkGreen, item.ToString());
							}

							break;
						case RedisEntryType.Set:
						case RedisEntryType.SortedSet:
							context.Output.WriteLine(Services.CommandOutletColor.DarkYellow, $"The '{context.Expression.Arguments[i]}' hashset have {((ISet<string>)entry).Count} entries.");

							foreach(object item in (IEnumerable)entry)
							{
								context.Output.Write(Services.CommandOutletColor.Gray, $"[{(++index).ToString()}] ");

								if(item == null)
									context.Output.WriteLine(Services.CommandOutletColor.DarkGray, "NULL");
								else
									context.Output.WriteLine(Services.CommandOutletColor.DarkGreen, item.ToString());
							}

							break;
						default:
							context.Output.WriteLine();
							break;
					}
				}
			}

			if(result.Count == 1)
				return result[0];
			else
				return result;
		}
		#endregion
	}
}
