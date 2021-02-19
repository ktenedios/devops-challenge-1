namespace Kosta.DevOpsChallenge.FileProcessor.DtoModel
{
    public enum ValidationResultTypeEnum
    {
        Success = 0,
        FailedEmptyFile = 1,
        FailedJsonDeserialization = 2,
        FailedMissingProducts = 3,
        FailedMissingTransmissionSummary = 4,
        FailedIncorrectRecordCount = 5,
        FailedIncorrectQtySum = 6,
        FailedAlreadyProcessedTransmissionSummaryId = 7
    }

    public static class ValidationResultTypeEnumExtensions
    {
        public static string GetReportHeader(this ValidationResultTypeEnum validationResult, string processedFileName)
        {
            var headerFirstLine = $"Processing {processedFileName}";
            var headerErrorLine = $"Discarding {processedFileName}, {validationResult}";
            var headerSuccessLine = $"Completed {processedFileName}";

            if (validationResult == ValidationResultTypeEnum.Success)
            {
                return $"{headerFirstLine}\r\n{headerSuccessLine}\r\n";
            }

            return $"{headerFirstLine}\r\n{headerErrorLine}\r\n";
        }
    }
}
