using System;
using System.IO;
using Xunit;
using Kosta.DevOpsChallenge.FileProcessor;
using Moq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kosta.DevOpsChallenge.FileProcessor.Tests
{
    public class FunctionsTests
    {
        [Fact]
        public void ProcessFile_EmptyFile_LogsEmptyFileError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

            using (var emptyBlob = new MemoryStream(0))
            {
                // Act
                Functions.ProcessFile(emptyBlob, "EmptyFile.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogErrorWasCalled("File 'EmptyFile.json' is empty");
            }
        }

        [Fact]
        public void ProcessFile_FileNotJsonFormat_LogsInvalidProductTransmissionFileError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            
            using (var notJsonBlob = TestExtensions.GetStreamFromString("Not JSON content"))
            {
                // Act
                Functions.ProcessFile(notJsonBlob, "NotJsonBlob.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogErrorWasCalled("File 'NotJsonBlob.json' is not a valid ProductTransmission file");
            }
        }

        [Fact]
        public void ProcessFile_SuppliedFileIsMissingProducts_LogsInvalidProductTransmissionFileError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

            var data = new {
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 6,
                    qtysum = 71
                }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(invalidJsonBlob, "MissingProducts.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogErrorWasCalled("File 'MissingProducts.json' is not a valid ProductTransmission file");
            }
        }

        [Fact]
        public void ProcessFile_SuppliedFileIsMissingTransmissionSummary_LogsInvalidProductTransmissionFileError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

            var product = new {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var data = new {
                products = new object[] { product }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(invalidJsonBlob, "MissingTransmissionSummary.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogErrorWasCalled("File 'MissingTransmissionSummary.json' is not a valid ProductTransmission file");
            }
        }
    }
}
