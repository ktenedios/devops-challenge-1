using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Kosta.DevOpsChallenge.FileProcessor.DtoModel;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Kosta.DevOpsChallenge.FileProcessor
{
    public class Functions
    {
        private readonly IProductTransmissionStreamReader _productTransmissionStreamReader = null;
        private readonly IWarehouseService _warehouseService = null;

        public Functions(IProductTransmissionStreamReader productTransmissionStreamReader, IWarehouseService warehouseService)
        {
            if (productTransmissionStreamReader == null)
            {
                throw new ArgumentNullException(nameof(productTransmissionStreamReader));
            }

            if (warehouseService == null)
            {
                throw new ArgumentNullException(nameof(warehouseService));
            }

            _productTransmissionStreamReader = productTransmissionStreamReader;
            _warehouseService = warehouseService;
        }

        public void ProcessFile(
            [BlobTrigger("file-drop/{name}")] Stream blobContents,
            [Blob("processed-files/{name}", FileAccess.Write)] Stream processedBlobContents,
            string name,
            ILogger logger)
        {
            string warehouseReport = null;
            var validationResult = ValidationResultTypeEnum.Success;

            try
            {
                var productTransmission = _productTransmissionStreamReader.ValidateStream(blobContents, name, logger);
                var blobContentsAsString = JsonSerializer.Serialize<ProductTransmission>(productTransmission);

                if (_warehouseService.IsTransmissionSummaryIdAlreadyProcessed(productTransmission.transmissionsummary.id))
                {
                    validationResult = ValidationResultTypeEnum.FailedAlreadyProcessedTransmissionSummaryId;
                    warehouseReport = _warehouseService.GetWarehouseReport(name, validationResult);
                    logger.LogError(warehouseReport);
                    return;
                }

                _warehouseService.UpdateWarehouse(productTransmission);
                warehouseReport = _warehouseService.GetWarehouseReport(name, validationResult);
                logger.LogInformation(warehouseReport);

                // All files that arrive in the file-drop container will exist in the processed-files container,
                // but only successfully processed files will have a file size greater than 0 in the processed-filed container
                var encoding = new UTF8Encoding();
                var bytes = encoding.GetBytes(blobContentsAsString);
                processedBlobContents.Write(bytes, 0, bytes.Length);
            }
            catch (ProductTransmissionFileValidationException validationException)
            {
                warehouseReport = _warehouseService.GetWarehouseReport(name, validationException.ValidationResult);
                logger.LogError(validationException, warehouseReport);
            }
        }
    }
}