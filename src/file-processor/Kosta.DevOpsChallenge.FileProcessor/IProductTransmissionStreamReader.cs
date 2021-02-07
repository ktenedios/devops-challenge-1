using System.IO;
using Kosta.DevOpsChallenge.FileProcessor.DtoModel;
using Microsoft.Extensions.Logging;

public interface IProductTransmissionStreamReader
{
    ProductTransmission ValidateStream(Stream incomingStream, string incomingFileName, ILogger logger);
}
