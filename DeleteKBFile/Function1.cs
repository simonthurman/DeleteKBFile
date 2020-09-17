using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using System.Collections.Generic;

namespace DeleteKBFile
{
    public static class DeleteKBFile
    {
        [FunctionName("DeleteKBFile")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string sourceFile = req.Query["sourceFile"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            sourceFile = sourceFile ?? data?.sourceFile;

            var authoringKey = "a4827dc9c9f146469010e3457f098495";
            var authoringUrl = $"https://testmeservice.cognitiveservices.azure.com/";
            var kbId = "4adea693-1ef5-4aa5-b3eb-37dc50f7465f";

            var client = new QnAMakerClient(new ApiKeyServiceClientCredentials(authoringKey))
            {
                Endpoint = authoringUrl
            };

            //Delete Source File
            List<string> toDelete = new List<string>();

            toDelete.Add(sourceFile);

            var updateDelete = await client.Knowledgebase.UpdateAsync(kbId, new UpdateKbOperationDTO
            {
                Add = null,
                Update = null,
                Delete = new UpdateKbOperationDTODelete(null, toDelete)
            });

            updateDelete = await MonitorOperation(client, updateDelete);

            string responseMessage = "This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }
        private static async Task<Operation> MonitorOperation(IQnAMakerClient client, Operation operation)
        {
            // Loop while operation is success
            for (int i = 0;
                i < 20 && (operation.OperationState == OperationStateType.NotStarted || operation.OperationState == OperationStateType.Running);
                i++)
            {
                Console.WriteLine("Waiting for operation: {0} to complete.", operation.OperationId);
                await Task.Delay(5000);
                operation = await client.Operations.GetDetailsAsync(operation.OperationId);
            }

            if (operation.OperationState != OperationStateType.Succeeded)
            {
                throw new Exception($"Operation {operation.OperationId} failed to completed.");
            }
            return operation;
        }
    }
}
