using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using Azure;
using Azure.Core.Pipeline;
using Azure.DigitalTwins.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Samples.AdtIothub
{
    public static class DpsAdtAllocationFunc
    {
        private const string adtAppId = "https://digitaltwins.azure.net";
        private static string adtInstanceUrl = Environment.GetEnvironmentVariable("ADT_SERVICE_URL");
        private static readonly HttpClient singletonHttpClientInstance = new HttpClient();

        [FunctionName("DpsAdtAllocationFunc")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            // Get request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogDebug($"Request.Body: {requestBody}");
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Get registration ID of the device
            string regId = data?.deviceRuntimeContext?.registrationId;

            bool fail = false;
            string message = "Uncaught error";
            var response = new ResponseObj();

            // Must have unique registration ID on DPS request
            if (regId == null)
            {
                message = "Registration ID not provided for the device.";
                log.LogInformation("Registration ID: NULL");
                fail = true;
            }
            else
            {
                string[] hubs = data?.linkedHubs.ToObject<string[]>();

                // Must have hubs selected on the enrollment
                if (hubs == null
                    || hubs.Count < 1)
                {
                    message = "No hub group defined for the enrollment.";
                    log.LogInformation("linkedHubs: NULL");
                    fail = true;
                }
                else
                {
                    // Find or create twin based on the provided registration ID and model ID
                    dynamic payloadContext = data?.deviceRuntimeContext?.payload;
                    string dtmi = payloadContext.modelId;
                    log.LogDebug($"payload.modelId: {dtmi}");
                    string dtId = await FindOrCreateTwinAsync(dtmi, regId, log);

                    // Get first linked hub (TODO: select one of the linked hubs based on policy)
                    response.iotHubHostName = hubs[0];

                    // Specify the initial tags for the device.
                    var tags = new TwinCollection();
                    tags["dtmi"] = dtmi;
                    tags["dtId"] = dtId;

                    // Specify the initial desired properties for the device.
                    var properties = new TwinCollection();

                    // Add the initial twin state to the response.
                    var twinState = new TwinState(tags, properties);
                    response.initialTwin = twinState;
                }
            }

            log.LogDebug("Response: " + ((response.iotHubHostName != null)? JsonConvert.SerializeObject(response) : message));

            return fail
                ? new BadRequestObjectResult(message)
                : (ActionResult)new OkObjectResult(response);
        }

        public static async Task<string> FindOrCreateTwinAsync(string dtmi, string regId, ILogger log)
        {
            // Create Digital Twins client
            var cred = new ManagedIdentityCredential(adtAppId);
            var client = new DigitalTwinsClient(
                new Uri(adtInstanceUrl),
                cred,
                new DigitalTwinsClientOptions
                {
                    Transport = new HttpClientTransport(singletonHttpClientInstance)
                });

            // Find existing twin with registration ID
            string query = $"SELECT * FROM DigitalTwins T WHERE $dtId = '{regId}' AND IS_OF_MODEL('{dtmi}')";
            AsyncPageable<BasicDigitalTwin> twins = client.QueryAsync(query);
            string dtId;

            await foreach (BasicDigitalTwin digitalTwin in twins)
            {
                dtId = digitalTwin.Id;
                log.LogInformation($"Twin '{dtId}' with Registration ID '{regId}' found in DT");
                break;
            }

            if (String.IsNullOrWhiteSpace(dtId))
            {
                // Not found, so create new twin
                log.LogInformation($"Twin ID not found - setting DT ID to regID");
                dtId = regId; // use the Registration ID as the DT ID

                // Initialize the twin properties
                var digitalTwin = new BasicDigitalTwin
                {
                    Metadata = { ModelId = dtmi },
                    Contents =
                {
                    {  "Temperature", 0.0 },
                },
                };

                await client.CreateOrReplaceDigitalTwinAsync<BasicDigitalTwin>(dtId, digitalTwin);
                log.LogInformation($"Twin '{dtId}' created in DT");
            }

            return dtId;
        }
    }

    /// <summary>
    /// Expected function result format
    /// </summary>
    public class ResponseObj
    {
        public string iotHubHostName { get; set; }
        public TwinState initialTwin { get; set; }
    }
}