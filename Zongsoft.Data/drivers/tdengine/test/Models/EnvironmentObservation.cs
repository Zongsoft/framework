using System;

namespace Zongsoft.Data.TDengine.Tests.Models;

public struct EnvironmentObservation
{
	/// <summary>获取或设置采集时间。</summary>
	public DateTime Timestamp { get; set; }
	/// <summary>获取或设置监测站编号。</summary>
	public string StationCode { get; set; }
	/// <summary>获取或设置监测区域。</summary>
	public string Region { get; set; }
	/// <summary>获取或设置温度。</summary>
	public float? Temperature { get; set; }
	/// <summary>获取或设置湿度。</summary>
	public float? Humidity { get; set; }
	/// <summary>获取或设置风向角度。</summary>
	public short? WindDirection { get; set; }
	/// <summary>获取或设置风速。</summary>
	public float? WindSpeed { get; set; }
	/// <summary>获取或设置是否降雨。</summary>
	public bool Raining { get; set; }
	/// <summary>获取或设置天气描述。</summary>
	public string Description { get; set; }
}
