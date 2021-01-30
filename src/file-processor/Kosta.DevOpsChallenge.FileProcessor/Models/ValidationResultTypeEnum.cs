namespace Kosta.DevOpsChallenge.FileProcessor.Models
{
    public enum ValidationResultTypeEnum
    {
        Success = 0,
        FailedMissingProducts = 1,
        FailedMissingTransmissionSummary = 2,
        FailedIncorrectRecordCount = 3,
        FailedIncorrectQtySum = 4
    }
}
