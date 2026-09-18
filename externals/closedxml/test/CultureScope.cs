using System.Globalization;

namespace Zongsoft.Externals.ClosedXml.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class CultureSensitiveCollection
{
	public const string Name = "Culture-sensitive";
}

internal sealed class CultureScope : IDisposable
{
	private readonly CultureInfo _culture;

	public CultureScope(string name)
	{
		_culture = CultureInfo.CurrentCulture;
		var culture = CultureInfo.GetCultureInfo(name);
		CultureInfo.CurrentCulture = culture;
	}

	public void Dispose()
	{
		CultureInfo.CurrentCulture = _culture;
	}
}
