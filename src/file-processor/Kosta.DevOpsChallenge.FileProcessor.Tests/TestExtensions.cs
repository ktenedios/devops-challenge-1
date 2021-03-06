using System;
using System.IO;
using System.Text;
using Moq;
using Microsoft.Extensions.Logging;

namespace Kosta.DevOpsChallenge.FileProcessor.Tests
{
    public static class TestExtensions
    {
        // Designed to make mocking of ILogger easier by minimising the amount of boilerplate code required.
        // Refer to Refer to https://adamstorr.azurewebsites.net/blog/mocking-ilogger-with-moq.
        public static Mock<ILogger<T>> VerifyLogWasCalled<T>(this Mock<ILogger<T>> logger, LogLevel expectedLogLevel, string expectedMessageToBeContained)
        {
            Func<object, Type, bool> state = (v, t) => v.ToString().Contains(expectedMessageToBeContained);

            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == expectedLogLevel),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => state(v, t)),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)
                )
            );

            return logger;
        }

        public static Stream GetStreamFromString(string stringToPutInStream)
        {
            var encoding = new UTF8Encoding();
            var stringAsBytes = encoding.GetBytes(stringToPutInStream);
            var stream = new MemoryStream(stringAsBytes.Length);
            stream.Write(stringAsBytes, 0, stringAsBytes.Length);
            stream.Flush();
            return stream;
        }

        public static bool StreamMatchesStringContent(this string originalString, Stream compareToStream)
        {
            var encoding = new UTF8Encoding();
            var originalBytes = encoding.GetBytes(originalString);
            var originalContentLength = originalBytes.Length;
            var compareToStreamLength = Convert.ToInt32(compareToStream.Length);

            if (originalContentLength != compareToStreamLength)
            {
                return false;
            }

            var compareToContent = new byte[compareToStreamLength];

            compareToStream.Position = 0;
            compareToStream.Read(compareToContent, 0, compareToStreamLength);

            for (var byteCounter = 0; byteCounter < originalContentLength; byteCounter++)
            {
                if (originalBytes[byteCounter] != compareToContent[byteCounter])
                {
                    return false;
                }
            }

            return true;
        }
    }
}