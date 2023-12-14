using System.Text;
using Howest.CloudServices.Iot.Device.Security;
using Howest.CloudServices.Iot.Models.Dtos;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Howest.CloudServices.Iot.Device;

internal static class Program
{
    private const string Device = "pcwarre";
    private static ushort _threshhold = 0;
    private static float _batteryPercentage = 100;
    
    public static async Task Main(string[] args)
    {
        var (appSettings, random) = await InitAsync();
        
        await using var deviceClient = DeviceClient.CreateFromConnectionString(appSettings.ConnectionString);

        // open connection explicitly
        await deviceClient.OpenAsync();
        
        var reportedProperties = new TwinCollection
        {
            ["bootTime"] = DateTime.Now,
            ["battery"] = _batteryPercentage
        };
        await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        await ForceDeviceTwinRetrievalAsync(deviceClient);
        
        await deviceClient.SetReceiveMessageHandlerAsync((message, context) => ReceiveMessageAsync(deviceClient, message, context), null);
        await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, null);
        
        while (true)
        {
            reportedProperties = new TwinCollection
            {
                ["battery"] = _batteryPercentage -= (float)(random.NextDouble() * .25)
            };
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);

            if (_batteryPercentage < 1)
                break;
            
            await SendMessageAsync(deviceClient, random);
            Thread.Sleep(1000);
        }
    }

    private static async Task ForceDeviceTwinRetrievalAsync(DeviceClient deviceClient)
    {
        var twin = await deviceClient.GetTwinAsync();

        await OnDesiredPropertyChanged(twin.Properties.Desired, deviceClient);
    }

    private static Task OnDesiredPropertyChanged(TwinCollection desiredproperties, object usercontext)
    {
        var data = desiredproperties["threshold"];
        _threshhold = (ushort)data;
        Console.WriteLine($"One or more device twin properties changed: {JsonConvert.SerializeObject(desiredproperties)}");
        return Task.CompletedTask;
    }

    private static async Task<(AppSettings, Random)> InitAsync()
    {
        var file = JsonConvert.DeserializeObject<JToken>(await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"))) ?? throw new Exception();
        var appsettings = JsonConvert.DeserializeObject<AppSettings>(file[nameof(AppSettings)].ToString()) ?? throw new Exception();
        var random = new Random();

        return (appsettings, random);
    }

    private static async Task SendMessageAsync(DeviceClient deviceClient, Random random)
    {
        var sensorData = new SensorData
        {
            SensorValue = (ushort)(random.NextDouble() * 100),
            Device = Device
        };

        if (sensorData.SensorValue > _threshhold)
        {
            var jsonData = JsonConvert.SerializeObject(sensorData);
            var message = new Message(Encoding.UTF8.GetBytes(jsonData));

            await deviceClient.SendEventAsync(message);

            Console.WriteLine($"Data sent from {sensorData.Device}: {jsonData}");
        }
        else
        {
            Console.WriteLine($"Data not sent from {sensorData.Device}: {sensorData.SensorValue}");
        }
    }
    
    private static async Task ReceiveMessageAsync(DeviceClient deviceClient, Message message, object userContext)
    {
        var messageData = Encoding.ASCII.GetString(message.GetBytes());
        Console.WriteLine("Received message: {0}", messageData);
        await deviceClient.CompleteAsync(message);
    }
}