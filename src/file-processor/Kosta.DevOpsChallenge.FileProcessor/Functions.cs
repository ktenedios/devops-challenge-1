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
            [Blob("successful-files/{name}")] Stream successfulBlobContents,
            string name,
            ILogger logger)
        {
            if (blobContents.Length == 0)
            {
                logger.LogError($"File '{name}' is empty");
                return;
            }

            string blobContentsAsString = null;
            ProductTransmission pt = null;
            var invalidProductTransmissionFileErrorMessage = $"File '{name}' is not a valid ProductTransmission file";

            try
            {
                using (var sr = new StreamReader(blobContents, Encoding.UTF8))
                {
                    // Important to set stream's position to 0 to deserialize entire contents
                    blobContents.Position = 0;
                    blobContentsAsString = sr.ReadToEnd();
                    pt = JsonSerializer.Deserialize<ProductTransmission>(blobContentsAsString);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, invalidProductTransmissionFileErrorMessage);
                return;
            }

            var validationResult = pt.ValidateObject();
            if (validationResult != ValidationResultTypeEnum.Success)
            {
                logger.LogError(invalidProductTransmissionFileErrorMessage);
                return;
            }

            // Copy file to container that stores successfully processed files
            var encoding = new UTF8Encoding();
            var bytes = encoding.GetBytes(blobContentsAsString);
            successfulBlobContents.Write(bytes, 0, bytes.Length);
        }
    }
}