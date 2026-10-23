using System;

namespace Zongsoft.Externals.Wechat.Platforms;

public static class ComponentManager
{
	public static Component Find(string appId)
	{
		ArgumentException.ThrowIfNullOrEmpty(appId);
		var options = Utility.GetOptions<Options.ComponentOptionsCollection>("/Externals/Wechat/Components");

		if(options == null || !options.TryGetValue(appId, out var option))
			return null;

		var store = Services.ApplicationContext.Current?.Services.GetService(typeof(IComponentStore)) as IComponentStore;
		return new Component(option, store ?? throw new InvalidOperationException());
	}
}
