using System;
using System.Collections.Generic;
using Kosta.DevOpsChallenge.FileProcessor.DtoModel;

namespace Kosta.DevOpsChallenge.FileProcessor
{
    public interface IWarehouseService
    {
        bool IsTransmissionSummaryIdAlreadyProcessed(Guid transmissionSummaryId);
        void UpdateWarehouse(Kosta.DevOpsChallenge.FileProcessor.DtoModel.ProductTransmission productTransmission);
        string GetWarehouseReport(string processedFileName, ValidationResultTypeEnum validationResult);
    }
}