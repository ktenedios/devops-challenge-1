using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Kosta.DevOpsChallenge.FileProcessor.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Kosta.DevOpsChallenge.FileProcessor
{
    public class Functions
    {
        public static void ProcessQueueMessage(
            [QueueTrigger("message-queue")] string message,
            ILogger logger)
        {
            logger.LogInformation(message);
        }

        public static void ProcessFile(
            [BlobTrigger("file-drop/{name}")] Stream blobContents,
            string name,
            ILogger logger)
        {
            if (blobContents.Length == 0)
            {
                logger.LogError($"File '{name}' is empty");
                return;
            }

            ProductTransmission pt = null;
            var invalidProductTransmissionFileErrorMessage = $"File '{name}' is not a valid ProductTransmission file";

            try
            {
                using (var sr = new StreamReader(blobContents, Encoding.UTF8))
                {
                    // Important to set stream's position to 0 to deserialize entire contents
                    blobContents.Position = 0;
                    var blobContentsAsString = sr.ReadToEnd();
                    pt = JsonSerializer.Deserialize<ProductTransmission>(blobContentsAsString);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, invalidProductTransmissionFileErrorMessage);
                return;
            }

            if (!pt.IsValid())
            {
                logger.LogError(invalidProductTransmissionFileErrorMessage);
                return;
            }
        }
    }
}