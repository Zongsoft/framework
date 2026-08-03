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
	private readonly CultureInfo _uiCulture;

	public CultureScope(string name)
	{
		_culture = CultureInfo.CurrentCulture;
		_uiCulture = CultureInfo.CurrentUICulture;
		var culture = CultureInfo.GetCultureInfo(name);
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
	}

	public void Dispose()
	{
		CultureInfo.CurrentCulture = _culture;
		CultureInfo.CurrentUICulture = _uiCulture;
	}
}
