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
        public static Mock<ILogger<T>> VerifyLogErrorWasCalled<T>(this Mock<ILogger<T>> logger, string expectedMessage)
        {
            Func<object, Type, bool> state = (v, t) => v.ToString().CompareTo(expectedMessage) == 0;

            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
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
    }
}