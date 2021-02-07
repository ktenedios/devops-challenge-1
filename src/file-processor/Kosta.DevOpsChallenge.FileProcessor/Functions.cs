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
        public static void ProcessFile(
            [BlobTrigger("file-drop/{name}")] Stream blobContents,
            [Blob("processed-files/{name}", FileAccess.Write)] Stream processedBlobContents,
            string name,
            ILogger logger)
        {
            

            // Copy file to container that stores successfully processed files
            /*var encoding = new UTF8Encoding();
            var bytes = encoding.GetBytes(blobContentsAsString);
            processedBlobContents.Write(bytes, 0, bytes.Length);*/
        }
    }
}