using System;
using System.Collections.Generic;

namespace Kosta.DevOpsChallenge.FileProcessor
{
    public interface IWarehouseService
    {
        bool IsTransmissionSummaryIdAlreadyProcessed(Guid transmissionSummaryId);
        void UpdateWarehouse(Kosta.DevOpsChallenge.FileProcessor.DtoModel.ProductTransmission productTransmission);
        IEnumerable<Kosta.DevOpsChallenge.FileProcessor.WarehouseDb.Product> GetProductInventoryForCategory(string categoryName);
    }
}