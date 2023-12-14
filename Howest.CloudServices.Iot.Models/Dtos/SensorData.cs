using Newtonsoft.Json;

namespace Howest.CloudServices.Iot.Models.Dtos;

public class SensorData
{
    [JsonProperty("id")] public Guid Id { get; set; }

    [JsonProperty("sensorValue")] public ushort SensorValue { get; set; }

    [JsonProperty("device")] public string Device { get; set; }
}