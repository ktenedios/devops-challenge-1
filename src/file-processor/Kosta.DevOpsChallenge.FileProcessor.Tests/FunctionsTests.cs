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

            using (var successfulBlobContents = new MemoryStream())
            using (var emptyBlob = new MemoryStream(0))
            {
                // Act
                Functions.ProcessFile(emptyBlob, successfulBlobContents, "EmptyFile.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogErrorWasCalled("File 'EmptyFile.json' is empty");
            }
        }

        [Fact]
        public void ProcessFile_FileNotJsonFormat_LogsInvalidProductTransmissionFileError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

            using (var successfulBlobContents = new MemoryStream())
            using (var notJsonBlob = TestExtensions.GetStreamFromString("Not JSON content"))
            {
                // Act
                Functions.ProcessFile(notJsonBlob, successfulBlobContents, "NotJsonBlob.json", mockLogger.Object);

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

            using (var successfulBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(invalidJsonBlob, successfulBlobContents, "MissingProducts.json", mockLogger.Object);

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

            using (var successfulBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(invalidJsonBlob, successfulBlobContents, "MissingTransmissionSummary.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogErrorWasCalled("File 'MissingTransmissionSummary.json' is not a valid ProductTransmission file");
            }
        }

        [Fact]
        public void ProcessFile_SuppliedFileHasMismatchedNumberOfRecords_LogsInvalidProductTransmissionFileError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

            var product1 = new {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var product2 = new {
                sku = "7200354",
                description = "Bosch Blue 900W Professional Corded Rotary Drill With 8 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 549,
                location = "Oakleigh",
                qty = 15
            };

            var data = new {
                products = new object[] { product1, product2 },
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 6,
                    qtysum = 25
                }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var successfulBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(invalidJsonBlob, successfulBlobContents, "MismatchedNumberOfRecords.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogErrorWasCalled("File 'MismatchedNumberOfRecords.json' is not a valid ProductTransmission file");
            }
        }

        [Fact]
        public void ProcessFile_SuppliedFileHasMismatchedQuantities_LogsInvalidProductTransmissionFileError()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

            var product1 = new {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var product2 = new {
                sku = "7200354",
                description = "Bosch Blue 900W Professional Corded Rotary Drill With 8 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 549,
                location = "Oakleigh",
                qty = 15
            };

            var data = new {
                products = new object[] { product1, product2 },
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 2,
                    qtysum = 20
                }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var successfulBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(invalidJsonBlob, successfulBlobContents, "MismatchedQuantities.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogErrorWasCalled("File 'MismatchedQuantities.json' is not a valid ProductTransmission file");
            }
        }

        [Fact]
        public void ProcessFile_SuppliedFileIsValid_FileMovedToSuccessfulContainer()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();

            var product1 = new {
                sku = "6200354",
                description = "Bosch Blue 800W Professional Corded Rotary Drill With 6 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 349,
                location = "Artarmon",
                qty = 10
            };

            var product2 = new {
                sku = "7200354",
                description = "Bosch Blue 900W Professional Corded Rotary Drill With 8 Piece Accessory Kit",
                category = "Our Range > Tools > Power Tools > Drills > Rotary Hammer Drills",
                price = 549,
                location = "Oakleigh",
                qty = 15
            };

            var data = new {
                products = new object[] { product1, product2 },
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 2,
                    qtysum = 25
                }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var successfulBlobContents = new MemoryStream())
            using (var validJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                Functions.ProcessFile(validJsonBlob, successfulBlobContents, "ValidFile.json", mockLogger.Object);

                // Assert
                var isMatch = serializedData.StreamMatchesStringContent(successfulBlobContents);
                Assert.True(isMatch);
            }
        }
    }
}
