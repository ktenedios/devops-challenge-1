using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Kosta.DevOpsChallenge.FileProcessor.Models;
using Microsoft.Extensions.Logging;

public class FileValidator : IFileValidator
{
    private static string CreateMessageWithValidationResult(string message, ValidationResultTypeEnum validationResult)
    {
        return $"ValidationResult: {validationResult}, ErrorMessage: {message}";
    }

    public ValidationResultTypeEnum ValidateFile(Stream incomingStream, string incomingFileName, ILogger logger)
    {
        var validationResult = ValidationResultTypeEnum.FailedEmptyFile;
        if (incomingStream.Length == 0)
        {
            logger.LogError(CreateMessageWithValidationResult($"File '{incomingFileName}' is empty", validationResult));
            return validationResult;
        }

        string incomingStreamAsString = null;
        ProductTransmission pt = null;
        var invalidProductTransmissionFileErrorMessage = $"File '{incomingFileName}' is not a valid ProductTransmission file";

        validationResult = ValidationResultTypeEnum.FailedJsonDeserialization;
        try
        {
            using (var sr = new StreamReader(incomingStream, Encoding.UTF8))
            {
                // Important to set stream's position to 0 to deserialize entire contents
                incomingStream.Position = 0;
                incomingStreamAsString = sr.ReadToEnd();
                pt = JsonSerializer.Deserialize<ProductTransmission>(incomingStreamAsString);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, CreateMessageWithValidationResult(invalidProductTransmissionFileErrorMessage, validationResult));
            return validationResult;
        }

        validationResult = pt.ValidateObject();
        if (validationResult != ValidationResultTypeEnum.Success)
        {
            logger.LogError(CreateMessageWithValidationResult(invalidProductTransmissionFileErrorMessage, validationResult));
        }

        logger.LogInformation(CreateMessageWithValidationResult($"File '{incomingFileName}' successfully validated", validationResult));
        return validationResult;
    }
}
