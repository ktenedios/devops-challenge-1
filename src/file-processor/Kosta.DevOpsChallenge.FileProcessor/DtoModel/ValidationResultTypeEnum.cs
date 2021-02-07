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
        FailedIncorrectQtySum = 6
    }
}
