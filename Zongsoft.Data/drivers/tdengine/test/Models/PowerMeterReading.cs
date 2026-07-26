using System;

namespace Zongsoft.Data.TDengine.Tests.Models;

public struct PowerMeterReading
{
	/// <summary>获取或设置采集时间。</summary>
	public DateTime Timestamp { get; set; }
	/// <summary>获取或设置电表编号。</summary>
	public string MeterId { get; set; }
	/// <summary>获取或设置电表位置。</summary>
	public string Location { get; set; }
	/// <summary>获取或设置电表分组编号。</summary>
	public int GroupId { get; set; }
	/// <summary>获取或设置电流。</summary>
	public float Current { get; set; }
	/// <summary>获取或设置电压。</summary>
	public int Voltage { get; set; }
	/// <summary>获取或设置相位。</summary>
	public float Phase { get; set; }
}
