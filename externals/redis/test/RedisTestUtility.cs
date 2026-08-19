using Zongsoft.Externals.Redis.Messaging;
using Zongsoft.Externals.Redis.Configuration;

namespace Zongsoft.Externals.Redis.Tests;

internal static class RedisTestUtility
{
	public static string GetQueueKey(string name, string topic) => $"Zongsoft.Queue:{name}:{topic}";
	public static RedisQueue CreateQueue(string name, string group = null, string client = null, int deadline = 3, string idleTimeout = "2s")
	{
		var settings = RedisConnectionSettingsDriver.Instance.GetSettings(name,
			$"server={Global.Server};password={Global.Password};group={group};client={client};timeout=5s;deadline={deadline};idleTimeout={idleTimeout};");

		return new RedisQueue(name, settings);
	}
}
