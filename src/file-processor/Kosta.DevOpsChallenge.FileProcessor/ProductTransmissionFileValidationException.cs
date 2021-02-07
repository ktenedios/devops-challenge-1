using System;
using Kosta.DevOpsChallenge.FileProcessor.Models;

public class ProductTransmissionFileValidationException : Exception
{
    public ProductTransmissionFileValidationException(string fileName, ValidationResultTypeEnum validationResult)
        : base($"File validation on file '{fileName}' failed. Result: {validationResult}")
    {
        ValidationResult = validationResult;
        FileName = fileName;
    }

    public ProductTransmissionFileValidationException(string fileName, ValidationResultTypeEnum validationResult, Exception inner)
        : base($"File validation on file '{fileName}' failed. Result: {validationResult}", inner)
    {
        ValidationResult = validationResult;
        FileName = fileName;
    }

    public ValidationResultTypeEnum ValidationResult { get; private set; }

    public string FileName { get; private set; }
}
