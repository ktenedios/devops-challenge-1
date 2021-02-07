using System.IO;
using Kosta.DevOpsChallenge.FileProcessor.Models;
using Microsoft.Extensions.Logging;

public interface IFileValidator
{
    ValidationResultTypeEnum ValidateFile(Stream incomingStream, string incomingFileName, ILogger logger);
}
