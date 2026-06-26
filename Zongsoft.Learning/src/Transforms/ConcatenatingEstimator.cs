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
 * Copyright (C) 2025-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Learning library.
 *
 * The Zongsoft.Learning is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Learning is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Learning library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Microsoft.ML;

namespace Zongsoft.Learning.Transforms;

public class ConcatenatingEstimator : ITrainerBuilder
{
	public string Name => "Concatenating";

	public IEstimator<ITransformer> Build(MLContext context, ITrainer trainer)
	{
		ArgumentNullException.ThrowIfNull(trainer);

		var settings = ConcatenatingEstimatorSettingsDriver.Instance.GetSettings(trainer.Settings);

		if(string.IsNullOrEmpty(settings.Output))
			return null;
		if(settings.Inputs == null || settings.Inputs.Length == 0)
			return null;

		return context.Transforms.Concatenate(settings.Output, settings.Inputs);
	}
}
