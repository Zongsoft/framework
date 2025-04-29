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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Components.Commands;

internal static class Utility
{
	public static CommandOutletColor GetStateColor(WorkerState state) => state switch
	{
		WorkerState.Pausing or WorkerState.Paused => CommandOutletColor.Yellow,
		WorkerState.Resuming or WorkerState.Starting => CommandOutletColor.DarkCyan,
		WorkerState.Stopped or WorkerState.Stopping => CommandOutletColor.DarkGray,
		_ => CommandOutletColor.Green,
	};

	public static CommandOutletContent GetWorkerActionContent(IWorker worker, string message, CommandOutletColor? color = null)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.DarkCyan, worker.Name)
			.Append(CommandOutletColor.DarkGray, "(")
			.Append(GetStateColor(worker.State), worker.State.ToString())
			.Append(CommandOutletColor.DarkGray, ") ");

		if(color == null)
			return content.Append(message);
		else
			return content.Append(color.Value, message);
	}
}
