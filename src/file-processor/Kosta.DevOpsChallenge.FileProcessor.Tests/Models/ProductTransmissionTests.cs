using System;
using Kosta.DevOpsChallenge.FileProcessor.DtoModel;
using Xunit;

namespace Kosta.DevOpsChallenge.FileProcessor.Tests.DtoModel
{
    public class ProductTransmissionTests
    {
        [Fact]
        public void ValidateObject_NoProducts_ReturnsFailedMissingProducts()
        {
            // Arrange
            var pt = new ProductTransmission
            {
                transmissionsummary = new TransmissionSummary
                {
                    id = Guid.NewGuid(),
                    recordcount = 12,
                    qtysum = 22
                }
            };

            // Act
            var result = pt.ValidateObject();

            // Assert
            Assert.Equal(ValidationResultTypeEnum.FailedMissingProducts, result);
        }

        [Fact]
        public void ValidateObject_NoTransmissionSummary_ReturnsFailedMissingTransmissionSummary()
        {
            // Arrange
            var pt = new ProductTransmission
            {
                products = new[] {
                    new Product {
                        sku = "sku1",
                        description = "description1",
                        category = "category1",
                        price = 10,
                        location = "location1",
                        qty = 4
                    },
                    new Product {
                        sku = "sku2",
                        description = "description2",
                        category = "category2",
                        price = 20,
                        location = "location2",
                        qty = 2
                    }
                }
            };

            // Act
            var result = pt.ValidateObject();

            // Assert
            Assert.Equal(ValidationResultTypeEnum.FailedMissingTransmissionSummary, result);
        }

        [Fact]
        public void ValidateObject_MismatchedRecordCount_ReturnsFailedIncorrectRecordCount()
        {
            // Arrange
            var pt = new ProductTransmission
            {
                products = new[] {
                    new Product {
                        sku = "sku1",
                        description = "description1",
                        category = "category1",
                        price = 10,
                        location = "location1",
                        qty = 4
                    },
                    new Product {
                        sku = "sku2",
                        description = "description2",
                        category = "category2",
                        price = 20,
                        location = "location2",
                        qty = 2
                    }
                },
                transmissionsummary = new TransmissionSummary
                {
                    id = Guid.NewGuid(),
                    recordcount = 5,
                    qtysum = 6
                }
            };

            // Act
            var result = pt.ValidateObject();

            // Assert
            Assert.Equal(ValidationResultTypeEnum.FailedIncorrectRecordCount, result);
        }

        [Fact]
        public void ValidateObject_MismatchedQtySum_ReturnsFailedIncorrectQtySum()
        {
            // Arrange
            var pt = new ProductTransmission
            {
                products = new[] {
                    new Product {
                        sku = "sku1",
                        description = "description1",
                        category = "category1",
                        price = 10,
                        location = "location1",
                        qty = 4
                    },
                    new Product {
                        sku = "sku2",
                        description = "description2",
                        category = "category2",
                        price = 20,
                        location = "location2",
                        qty = 2
                    }
                },
                transmissionsummary = new TransmissionSummary
                {
                    id = Guid.NewGuid(),
                    recordcount = 2,
                    qtysum = 1
                }
            };

            // Act
            var result = pt.ValidateObject();

            // Assert
            Assert.Equal(ValidationResultTypeEnum.FailedIncorrectQtySum, result);
        }

        [Fact]
        public void ValidateObject_ValidObject_ReturnsSuccess()
        {
            // Arrange
            var pt = new ProductTransmission
            {
                products = new[] {
                    new Product {
                        sku = "sku1",
                        description = "description1",
                        category = "category1",
                        price = 10,
                        location = "location1",
                        qty = 4
                    },
                    new Product {
                        sku = "sku2",
                        description = "description2",
                        category = "category2",
                        price = 20,
                        location = "location2",
                        qty = 2
                    }
                },
                transmissionsummary = new TransmissionSummary
                {
                    id = Guid.NewGuid(),
                    recordcount = 2,
                    qtysum = 6
                }
            };

            // Act
            var result = pt.ValidateObject();

            // Assert
            Assert.Equal(ValidationResultTypeEnum.Success, result);
        }
    }
}
