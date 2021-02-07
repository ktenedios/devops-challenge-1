using System;
using System.IO;
using Xunit;
using Kosta.DevOpsChallenge.FileProcessor;
using Kosta.DevOpsChallenge.FileProcessor.Models;
using Moq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Kosta.DevOpsChallenge.FileProcessor.Tests
{
    public class FileValidatorTests
    {
        [Fact]
        public void ValidateFile_EmptyFile_LogsErrorAndReturnsFailedEmptyFile()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new FileValidator();

            using (var processedBlobContents = new MemoryStream())
            using (var emptyBlob = new MemoryStream(0))
            {
                // Act
                var result = sut.ValidateFile(emptyBlob, "EmptyFile.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'EmptyFile.json' is empty");
                Assert.Equal(ValidationResultTypeEnum.FailedEmptyFile, result);
            }
        }

        [Fact]
        public void ValidateFile_FileNotJsonFormat_LogsErrorAndReturnsFailedJsonDeserialization()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new FileValidator();

            using (var processedBlobContents = new MemoryStream())
            using (var notJsonBlob = TestExtensions.GetStreamFromString("Not JSON content"))
            {
                // Act
                var result = sut.ValidateFile(notJsonBlob, "NotJsonBlob.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'NotJsonBlob.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedJsonDeserialization, result);
            }
        }

        [Fact]
        public void ValidateFile_SuppliedFileIsMissingProducts_LogsErrorAndReturnsFailedMissingProducts()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new FileValidator();

            var data = new {
                transmissionsummary = new {
                    id = Guid.NewGuid(),
                    recordcount = 6,
                    qtysum = 71
                }
            };

            var serializedData = JsonSerializer.Serialize(data);

            using (var processedBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                var result = sut.ValidateFile(invalidJsonBlob, "MissingProducts.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'MissingProducts.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedMissingProducts, result);
            }
        }

        [Fact]
        public void ValidateFile_SuppliedFileIsMissingTransmissionSummary_LogsErrorAndReturnsFailedMissingTransmissionSummary()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new FileValidator();

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

            using (var processedBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                var result = sut.ValidateFile(invalidJsonBlob, "MissingTransmissionSummary.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'MissingTransmissionSummary.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedMissingTransmissionSummary, result);
            }
        }

        [Fact]
        public void ValidateFile_SuppliedFileHasMismatchedNumberOfRecords_LogsErrorAndReturnsFailedIncorrectRecordCount()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new FileValidator();

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

            using (var processedBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                var result = sut.ValidateFile(invalidJsonBlob, "MismatchedNumberOfRecords.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'MismatchedNumberOfRecords.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedIncorrectRecordCount, result);
            }
        }

        [Fact]
        public void ValidateFile_SuppliedFileHasMismatchedQuantities_LogsErrorAndReturnsFailedIncorrectQtySum()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new FileValidator();

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

            using (var processedBlobContents = new MemoryStream())
            using (var invalidJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                var result = sut.ValidateFile(invalidJsonBlob, "MismatchedQuantities.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogWasCalled(LogLevel.Error, "File 'MismatchedQuantities.json' is not a valid ProductTransmission file");
                Assert.Equal(ValidationResultTypeEnum.FailedIncorrectQtySum, result);
            }
        }

        [Fact]
        public void ValidateFile_SuppliedFileIsValid_LogsInformationAndReturnsSuccess()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<Functions>>();
            var sut = new FileValidator();

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

            using (var processedBlobContents = new MemoryStream())
            using (var validJsonBlob = TestExtensions.GetStreamFromString(serializedData))
            {
                // Act
                var result = sut.ValidateFile(validJsonBlob, "ValidFile.json", mockLogger.Object);

                // Assert
                mockLogger.VerifyLogWasCalled(LogLevel.Information, "File 'ValidFile.json' successfully validated");
                Assert.Equal(ValidationResultTypeEnum.Success, result);
            }
        }
    }
}
