using System;
using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;
using Microsoft.Azure.WebJobs;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Howest.CloudServices.Iot.Models.Dtos;
using Howest.Mct.Functions.CosmosDb.Helper;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Howest.CloudServices.Iot.IotTrigger;

public static class IotHubTrigger
{
    [FunctionName("IotHubTrigger")]
    public static async Task RunAsync([IoTHubTrigger("messages/events", Connection = "EventHubEndPoint")] EventData message,
        ILogger log)
    {
        var dataString = Encoding.UTF8.GetString(message.Body.ToArray());
        var data = JsonConvert.DeserializeObject<SensorData>(dataString);
        
        var container = CosmosHelper.GetContainer();
        
        data.Id = Guid.NewGuid();
        await container.CreateItemAsync(data, new PartitionKey(data.Device));
    } 
}