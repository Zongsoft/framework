using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data.TDengine.Tests.Models;

namespace Zongsoft.Data.TDengine.Tests;

[Collection("Database")]
[Trait("Category", "Integration")]
public class TimeSeriesScenarioTest(DatabaseFixture database)
{
	private const string TESTING_DISABLED_REASON = "TDengine integration tests require a debugger or ZONGSOFT_DATA_TESTS=1.";
	private readonly DatabaseFixture _database = database;

	[Fact]
	public async Task SmartMetersAtSameTimestampRemainIsolatedByTagsAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 1, 1, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetTimeCriteria(timestamp, timestamp.AddMilliseconds(1));

		var sanFrancisco = new PowerMeterReading
		{
			Timestamp = timestamp,
			MeterId = "d1001",
			Location = "California.SanFrancisco",
			GroupId = 2,
			Current = 10.5F,
			Voltage = 219,
			Phase = 0.25F,
		};

		var losAngeles = new PowerMeterReading
		{
			Timestamp = timestamp,
			MeterId = "d1002",
			Location = "California.SanFrancisco",
			GroupId = 2,
			Current = 12.5F,
			Voltage = 221,
			Phase = 0.5F,
		};

		try
		{
			await accessor.DeleteAsync<PowerMeterReading>(criteria);

			Assert.Equal(1, await accessor.InsertAsync(sanFrancisco));
			Assert.Equal(1, await accessor.InsertAsync(losAngeles));

			var rows = accessor.Select<PowerMeterReading>(criteria).ToArray();
			Assert.Equal(2, rows.Length);
			Assert.Contains(rows, row => row.MeterId == sanFrancisco.MeterId);
			Assert.Contains(rows, row => row.MeterId == losAngeles.MeterId);

			var meterCriteria = criteria & Condition.Equal(nameof(PowerMeterReading.MeterId), sanFrancisco.MeterId);
			var row = Assert.Single(accessor.Select<PowerMeterReading>(meterCriteria));
			Assert.Equal(sanFrancisco.Current, row.Current);
			Assert.Equal(sanFrancisco.Voltage, row.Voltage);
			Assert.Equal(sanFrancisco.Phase, row.Phase);
		}
		finally
		{
			await accessor.DeleteAsync<PowerMeterReading>(criteria);
		}
	}

	[Fact]
	public async Task SmartMeterImportAggregatesAcrossSubtablesAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 1, 2, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetTimeCriteria(timestamp, timestamp.AddMinutes(3));
		var readings = new[]
		{
			CreatePowerMeter(timestamp, "d101", "California.SanFrancisco", 2, 10, 218, 0),
			CreatePowerMeter(timestamp.AddMinutes(1), "d101", "California.SanFrancisco", 2, 20, 219, 60),
			CreatePowerMeter(timestamp.AddMinutes(2), "d101", "California.SanFrancisco", 2, 30, 220, 120),
			CreatePowerMeter(timestamp, "d102", "California.LosAngeles", 3, 40, 221, 180),
			CreatePowerMeter(timestamp.AddMinutes(1), "d102", "California.LosAngeles", 3, 50, 222, 240),
			CreatePowerMeter(timestamp.AddMinutes(2), "d102", "California.LosAngeles", 3, 60, 223, 300),
		};

		try
		{
			await accessor.DeleteAsync<PowerMeterReading>(criteria);
			Assert.Equal(readings.Length, await accessor.ImportAsync(readings));

			var count = await accessor.AggregateAsync<PowerMeterReading, long>(
				DataAggregateFunction.Count,
				nameof(PowerMeterReading.Timestamp),
				criteria);
			var average = await accessor.AggregateAsync<PowerMeterReading, double>(
				DataAggregateFunction.Average,
				nameof(PowerMeterReading.Current),
				criteria);
			var maximum = await accessor.AggregateAsync<PowerMeterReading, int>(
				DataAggregateFunction.Maximum,
				nameof(PowerMeterReading.Voltage),
				criteria);

			Assert.Equal(6L, count);
			Assert.Equal(35D, average);
			Assert.Equal(223, maximum);
		}
		finally
		{
			await accessor.DeleteAsync<PowerMeterReading>(criteria);
		}
	}

	[Fact]
	public async Task SmartMeterImportCrossesNativeBatchBoundaryAsync()
	{
		const int COUNT = 1001;

		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 1, 3, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetTimeCriteria(timestamp, timestamp.AddMilliseconds(COUNT));
		var readings = Enumerable.Range(0, COUNT)
			.Select(index => CreatePowerMeter(
				timestamp.AddMilliseconds(index),
				"d1001",
				"California.SanFrancisco",
				2,
				index,
				220,
				index % 360))
			.ToArray();

		try
		{
			await accessor.DeleteAsync<PowerMeterReading>(criteria);
			Assert.Equal(COUNT, await accessor.ImportAsync(readings));

			var count = await accessor.AggregateAsync<PowerMeterReading, long>(
				DataAggregateFunction.Count,
				nameof(PowerMeterReading.Timestamp),
				criteria);
			Assert.Equal((long)COUNT, count);
		}
		finally
		{
			await accessor.DeleteAsync<PowerMeterReading>(criteria);
		}
	}

	[Fact]
	public async Task VehicleTelemetryRoundTripsNullableAltitudeAndTagsAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 2, 1, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetTimeCriteria(timestamp, timestamp.AddMilliseconds(1));
		var telemetry = new VehicleTelemetry
		{
			Timestamp = timestamp,
			PlateNumber = "沪A-TEST1",
			Model = "Model 3",
			Vin = "VIN209802010001",
			Longitude = 121.5,
			Latitude = 31.25,
			Altitude = null,
			Direction = 90,
			Speed = 72,
		};

		try
		{
			await accessor.DeleteAsync<VehicleTelemetry>(criteria);
			Assert.Equal(1, await accessor.InsertAsync(telemetry));

			var tagCriteria = criteria &
				Condition.Equal(nameof(VehicleTelemetry.Vin), telemetry.Vin) &
				Condition.Equal(nameof(VehicleTelemetry.Model), telemetry.Model);
			var row = Assert.Single(accessor.Select<VehicleTelemetry>(tagCriteria));

			Assert.Equal(telemetry.PlateNumber, row.PlateNumber);
			Assert.Equal(telemetry.Longitude, row.Longitude);
			Assert.Equal(telemetry.Latitude, row.Latitude);
			Assert.Null(row.Altitude);
			Assert.Equal(telemetry.Direction, row.Direction);
			Assert.Equal(telemetry.Speed, row.Speed);
		}
		finally
		{
			await accessor.DeleteAsync<VehicleTelemetry>(criteria);
		}
	}

	[Fact]
	public async Task VehicleTelemetryUpsertReplacesSameVehicleTimestampAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 2, 2, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetTimeCriteria(timestamp, timestamp.AddMilliseconds(1));
		var original = CreateVehicle(timestamp, 30, 10);
		var replacement = CreateVehicle(timestamp, 88, 25);

		try
		{
			await accessor.DeleteAsync<VehicleTelemetry>(criteria);
			Assert.Equal(1, await accessor.InsertAsync(original));
			Assert.Equal(1, await accessor.UpsertAsync(replacement));

			var row = Assert.Single(accessor.Select<VehicleTelemetry>(criteria));
			Assert.Equal(replacement.Vin, row.Vin);
			Assert.Equal(replacement.Speed, row.Speed);
			Assert.Equal(replacement.Altitude, row.Altitude);
		}
		finally
		{
			await accessor.DeleteAsync<VehicleTelemetry>(criteria);
		}
	}

	[Fact]
	public async Task EnvironmentImportSupportsMixedStationsUnicodeAndNullsAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 3, 1, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetTimeCriteria(timestamp, timestamp.AddMilliseconds(1));
		var observations = new[]
		{
			CreateEnvironment(timestamp, "EAST-001", "华东", 26.5F, 80F, true, "小雨"),
			CreateEnvironment(timestamp, "EAST-002", "华东", null, null, false, "传感器维护"),
			CreateEnvironment(timestamp, "NORTH-001", "华北", 18F, 35F, false, null),
		};

		try
		{
			await accessor.DeleteAsync<EnvironmentObservation>(criteria);
			Assert.Equal(observations.Length, await accessor.ImportAsync(observations));

			var eastCriteria = criteria & Condition.Equal(nameof(EnvironmentObservation.Region), "华东");
			var east = accessor.Select<EnvironmentObservation>(eastCriteria).ToArray();

			Assert.Equal(2, east.Length);
			Assert.Contains(east, row => row.StationCode == "EAST-001" && row.Raining && row.Description == "小雨");

			var maintenance = Assert.Single(east, row => row.StationCode == "EAST-002");
			Assert.Null(maintenance.Temperature);
			Assert.Null(maintenance.Humidity);
			Assert.Equal("传感器维护", maintenance.Description);
		}
		finally
		{
			await accessor.DeleteAsync<EnvironmentObservation>(criteria);
		}
	}

	[Fact]
	public async Task EnvironmentThresholdFilterAndMinimumIgnoreNullsAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 3, 2, 0, 0, 0, DateTimeKind.Local);
		var timeCriteria = GetTimeCriteria(timestamp, timestamp.AddMinutes(4));
		var observations = new[]
		{
			CreateEnvironment(timestamp, "SOUTH-001", "华南", 29F, 70F, true, "阵雨"),
			CreateEnvironment(timestamp.AddMinutes(1), "SOUTH-001", "华南", 31F, 60F, false, "多云"),
			CreateEnvironment(timestamp.AddMinutes(2), "SOUTH-001", "华南", 35F, 55F, false, "晴"),
			CreateEnvironment(timestamp.AddMinutes(3), "SOUTH-001", "华南", null, null, false, "维护"),
		};
		var thresholdCriteria = timeCriteria &
			Condition.GreaterThan(nameof(EnvironmentObservation.Temperature), 30F) &
			Condition.Equal(nameof(EnvironmentObservation.Raining), false);

		try
		{
			await accessor.DeleteAsync<EnvironmentObservation>(timeCriteria);
			Assert.Equal(observations.Length, await accessor.ImportAsync(observations));

			var rows = accessor.Select<EnvironmentObservation>(thresholdCriteria).ToArray();
			Assert.Equal(2, rows.Length);
			Assert.All(rows, row => Assert.True(row.Temperature > 30F));
			Assert.All(rows, row => Assert.False(row.Raining));

			var minimum = await accessor.AggregateAsync<EnvironmentObservation, double>(
				DataAggregateFunction.Minimum,
				nameof(EnvironmentObservation.Humidity),
				thresholdCriteria);
			Assert.Equal(55D, minimum);
		}
		finally
		{
			await accessor.DeleteAsync<EnvironmentObservation>(timeCriteria);
		}
	}

	[Fact]
	public async Task MotorWaveformRoundTripsLongSamplePayloadAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 4, 1, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetTimeCriteria(timestamp, timestamp.AddMilliseconds(1));
		var values = string.Join(',', Enumerable.Repeat("0.125,-0.250,0.375", 128));
		var waveform = CreateWaveform(timestamp, "MOTOR-A", values, 2048);

		try
		{
			await accessor.DeleteAsync<MotorWaveform>(criteria);
			Assert.Equal(1, await accessor.InsertAsync(waveform));

			var row = Assert.Single(accessor.Select<MotorWaveform>(criteria));
			Assert.Equal(waveform.LineId, row.LineId);
			Assert.Equal(waveform.SiteId, row.SiteId);
			Assert.Equal(waveform.ElevatorCode, row.ElevatorCode);
			Assert.Equal(waveform.MeasuringId, row.MeasuringId);
			Assert.Equal(values, row.Values);
			Assert.Equal(waveform.SampleRate, row.SampleRate);
			Assert.Equal(waveform.Length, row.Length);
			Assert.Equal(waveform.Ratio, row.Ratio);
		}
		finally
		{
			await accessor.DeleteAsync<MotorWaveform>(criteria);
		}
	}

	[Fact]
	public async Task MotorWaveformImportSplitsRowsByMeasuringPointAsync()
	{
		Assert.SkipUnless(Global.IsTestingEnabled, TESTING_DISABLED_REASON);

		IDataAccess accessor = _database.Accessor;
		var timestamp = new DateTime(2098, 4, 2, 0, 0, 0, DateTimeKind.Local);
		var criteria = GetTimeCriteria(timestamp, timestamp.AddMilliseconds(2));
		var waveforms = new[]
		{
			CreateWaveform(timestamp, "MOTOR-A", "0.1,0.2,0.3", 1024),
			CreateWaveform(timestamp.AddMilliseconds(1), "MOTOR-A", "0.4,0.5,0.6", 1024),
			CreateWaveform(timestamp, "MOTOR-B", "1.1,1.2,1.3", 4096),
		};

		try
		{
			await accessor.DeleteAsync<MotorWaveform>(criteria);
			Assert.Equal(waveforms.Length, await accessor.ImportAsync(waveforms));

			var measuringCriteria = criteria & Condition.Equal(nameof(MotorWaveform.MeasuringId), "MOTOR-A");
			var motorA = accessor.Select<MotorWaveform>(measuringCriteria).ToArray();
			Assert.Equal(2, motorA.Length);
			Assert.All(motorA, row => Assert.Equal(1024, row.SampleRate));

			var all = accessor.Select<MotorWaveform>(criteria).ToArray();
			Assert.Equal(3, all.Length);
			Assert.Contains(all, row => row.MeasuringId == "MOTOR-B" && row.SampleRate == 4096);
		}
		finally
		{
			await accessor.DeleteAsync<MotorWaveform>(criteria);
		}
	}

	private static PowerMeterReading CreatePowerMeter(
		DateTime timestamp,
		string meterId,
		string location,
		int groupId,
		float current,
		int voltage,
		float phase) => new()
	{
		Timestamp = timestamp,
		MeterId = meterId,
		Location = location,
		GroupId = groupId,
		Current = current,
		Voltage = voltage,
		Phase = phase,
	};

	private static VehicleTelemetry CreateVehicle(DateTime timestamp, int speed, int? altitude) => new()
	{
		Timestamp = timestamp,
		PlateNumber = "京A-TEST2",
		Model = "Fleet Sedan",
		Vin = "VIN209802020001",
		Longitude = 116.375,
		Latitude = 39.875,
		Altitude = altitude,
		Direction = 180,
		Speed = speed,
	};

	private static EnvironmentObservation CreateEnvironment(
		DateTime timestamp,
		string stationCode,
		string region,
		float? temperature,
		float? humidity,
		bool raining,
		string description) => new()
	{
		Timestamp = timestamp,
		StationCode = stationCode,
		Region = region,
		Temperature = temperature,
		Humidity = humidity,
		WindDirection = temperature.HasValue ? (short)135 : null,
		WindSpeed = humidity.HasValue ? 3.5F : null,
		Raining = raining,
		Description = description,
	};

	private static MotorWaveform CreateWaveform(
		DateTime timestamp,
		string measuringId,
		string values,
		int sampleRate) => new()
	{
		Timestamp = timestamp,
		LineId = "LINE-01",
		SiteId = "SITE-01",
		ElevatorCode = "ESC-01",
		MeasuringId = measuringId,
		Values = values,
		SampleRate = sampleRate,
		Length = values.Split(',').Length,
		Ratio = 1,
	};

	private static ICondition GetTimeCriteria(DateTime start, DateTime end) =>
		Condition.GreaterThanEqual("Timestamp", start) &
		Condition.LessThan("Timestamp", end);
}
