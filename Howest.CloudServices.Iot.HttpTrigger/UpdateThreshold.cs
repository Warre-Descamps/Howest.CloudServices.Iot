using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Howest.CloudServices.Iot.HttpTrigger;

public static class UpdateThreshold
{
    [FunctionName("UpdateThreshold")]
    public static async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "threshold/{deviceId}/{threshold:int}")] HttpRequest req, ILogger log,
        string deviceId, int threshold)
    {
        var connectionString = Environment.GetEnvironmentVariable("IotHubAdmin");
        var manager = RegistryManager.CreateFromConnectionString(connectionString);
        var twin = await manager.GetTwinAsync(deviceId);
        
        twin.Properties.Desired["threshold"] = threshold;
        await manager.UpdateTwinAsync(twin.DeviceId, twin, twin.ETag);
        
        return new OkObjectResult($"Threshold updated to {threshold}");
    }
}