using System;

namespace Zongsoft.Data.TDengine.Tests.Models;

public struct VehicleTelemetry
{
	/// <summary>获取或设置采集时间。</summary>
	public DateTime Timestamp { get; set; }
	/// <summary>获取或设置车牌号。</summary>
	public string PlateNumber { get; set; }
	/// <summary>获取或设置车型。</summary>
	public string Model { get; set; }
	/// <summary>获取或设置车辆识别码。</summary>
	public string Vin { get; set; }
	/// <summary>获取或设置经度。</summary>
	public double Longitude { get; set; }
	/// <summary>获取或设置纬度。</summary>
	public double Latitude { get; set; }
	/// <summary>获取或设置海拔高度。</summary>
	public int? Altitude { get; set; }
	/// <summary>获取或设置行驶方向。</summary>
	public int Direction { get; set; }
	/// <summary>获取或设置行驶速度。</summary>
	public int Speed { get; set; }
}
