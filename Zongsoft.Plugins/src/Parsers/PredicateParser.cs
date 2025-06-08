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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Plugins.Parsers
{
	public class PredicateParser : Parser
	{
		private readonly Regex _regex = new Regex(@"[^\s]+", RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

		public override object Parse(ParserContext context)
		{
			if(string.IsNullOrWhiteSpace(context.Text))
				throw new PluginException("Can not parse for the predication because the parser text is empty.");

			var matches = _regex.Matches(context.Text);

			if(matches.Count < 1)
				throw new PluginException("Can not parse for the predication.");

			var parts = matches[0].Value.Split('.');

			if(parts.Length > 2)
				throw new PluginException("Can not parse for the predication because of a syntax error.");

			IPredication predication;

			if(parts.Length == 1)
				predication = ApplicationContext.Current.Services.Resolve<IPredication>(parts[0]);
			else
			{
				if(!ApplicationContext.Current.Modules.TryGetValue(parts[0], out var module))
					throw new PluginException(string.Format("The '{0}' ServiceProvider is not exists on the predication parsing.", parts[0]));

				predication = module.Services.Resolve<IPredication>(parts[1]);
			}

			if(predication != null)
			{
				string text = matches.Count <= 1 ? null : context.Text.Substring(matches[1].Index);
				object argument = text;

				if(TypeExtension.IsAssignableFrom(typeof(IPredication<PluginPredicationContext>), predication.GetType()))
					argument = new PluginPredicationContext(text, context.Builtin, context.Node, context.Plugin);

				return Predicate(predication, argument);
			}

			return false;
		}

		private static bool Predicate(IPredication predication, object argument)
		{
			if(predication == null)
				return false;

			var task = predication.PredicateAsync(argument);
			if(task.IsCompletedSuccessfully)
				return task.Result;

			return task.AsTask().ConfigureAwait(false).GetAwaiter().GetResult();
		}
	}
}
