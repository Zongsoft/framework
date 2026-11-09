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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data.Common;

[System.Reflection.DefaultMember(nameof(Providers))]
[System.ComponentModel.DefaultProperty(nameof(Providers))]
public sealed class DataPopulatorProviderManager : IEnumerable<IDataPopulatorProvider>
{
	#region 成员字段
	private readonly List<IDataPopulatorProvider> _providers;
	#endregion

	#region 内部构造
	internal DataPopulatorProviderManager()
	{
		_providers = [
			ModelPopulatorProvider.Instance,
			ScalarPopulatorProvider.Instance,
			DictionaryPopulatorProvider.Instance,
		];
	}
	#endregion

	#region 公共属性
	public ICollection<IDataPopulatorProvider> Providers => _providers;
	#endregion

	#region 公共方法
	public IDataPopulatorProvider GetProvider(Type type)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		for(int i = 0; i < _providers.Count; i++)
		{
			var provider = _providers[i];
			if(provider.CanPopulate(type))
				return provider;
		}

		throw new DataException(string.Format(Properties.Resources.DataPopulator_ProviderNotFound_Message, type.FullName));
	}

	public IDataPopulator GetPopulator(IDataDriver driver, Type type, IDataRecord record, Metadata.IDataEntity entity = null)
	{
		var provider = this.GetProvider(type);
		return provider.GetPopulator(driver, type, record, entity);
	}
	#endregion

	#region 枚举遍历
	public IEnumerator<IDataPopulatorProvider> GetEnumerator() => _providers.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _providers.GetEnumerator();
	#endregion
}
