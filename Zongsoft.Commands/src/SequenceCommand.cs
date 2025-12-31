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
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Commands;

[CommandOption(VARIATE_OPTION, 'v', typeof(bool))]
[CommandOption(INITIATE_OPTION, 'i', typeof(int), 0)]
[CommandOption(GROWTH_LOWER_OPTION, 'l', typeof(int), 100)]
[CommandOption(GROWTH_UPPER_OPTION, 'u', typeof(int), 1000)]
public partial class SequenceCommand : CommandBase<CommandContext>
{
	#region 常量定义
	const string VARIATE_OPTION = "variate";
	const string INITIATE_OPTION = "initiate";
	const string GROWTH_LOWER_OPTION = "lower";
	const string GROWTH_UPPER_OPTION = "upper";
	#endregion

	#region 构造函数
	public SequenceCommand() : base("Sequence") { }
	public SequenceCommand(string name) : base(name) { }
	#endregion

	#region 公共属性
	public ISequenceBase Sequence { get; set; }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.Count > 1)
			throw new CommandException("Only one argument is allowed for specifying the sequence instance.");

		var provider = ApplicationContext.Current?.Services.Resolve<IServiceProvider<ISequenceBase>>();

		if(provider == null)
		{
			context.Output.WriteLine(CommandOutletColor.DarkRed, "The sequence service provider is not available.");
			return ValueTask.FromResult<object>(null);
		}

		if(context.Arguments.IsEmpty)
			this.Sequence = provider.GetService();
		else
			this.Sequence = provider.GetService(context.Arguments[0]);

		if(context.GetOptions().Contains(VARIATE_OPTION))
		{
			var initiate = context.GetOptions().GetValue<int>(INITIATE_OPTION);
			var growthLower = context.GetOptions().GetValue(GROWTH_LOWER_OPTION, 100);
			var growthUpper = context.GetOptions().GetValue(GROWTH_UPPER_OPTION, 1000);
			this.Sequence = this.Sequence.Variate(new Sequence.VariatorOptions(initiate, growthLower, growthUpper));
		}

		if(this.Sequence == null)
			context.Output.WriteLine(CommandOutletColor.DarkMagenta, context.Arguments.IsEmpty ? "The Sequence provider cannot obtain the default Sequence." : $"The sequence with the name '{context.Arguments[0]}' specified by the provider cannot be found.");
		else
			context.Output.WriteLine(CommandOutletColor.DarkGreen, $"The '{this.Sequence}' sequence has been set successfully.");

		return ValueTask.FromResult<object>(this.Sequence);
	}
	#endregion
}
