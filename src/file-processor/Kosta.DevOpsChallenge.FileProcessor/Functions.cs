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
        private readonly IProductTransmissionStreamReader _productTransmissionStreamReader = null;

        public Functions(IProductTransmissionStreamReader productTransmissionStreamReader)
        {
            if (productTransmissionStreamReader == null)
            {
                throw new ArgumentNullException(nameof(productTransmissionStreamReader));
            }

            _productTransmissionStreamReader = productTransmissionStreamReader;
        }

        public void ProcessFile(
            [BlobTrigger("file-drop/{name}")] Stream blobContents,
            [Blob("processed-files/{name}", FileAccess.Write)] Stream processedBlobContents,
            string name,
            ILogger logger)
        {
            var productTransmission = _productTransmissionStreamReader.ValidateStream(blobContents, name, logger);
            var blobContentsAsString = JsonSerializer.Serialize<ProductTransmission>(productTransmission);

            // All files that arrive in the file-drop container will exist in the processed-files container,
            // but only successfully processed files will have a file size greater than 0 in the processed-filed container
            var encoding = new UTF8Encoding();
            var bytes = encoding.GetBytes(blobContentsAsString);
            processedBlobContents.Write(bytes, 0, bytes.Length);
        }
    }
}