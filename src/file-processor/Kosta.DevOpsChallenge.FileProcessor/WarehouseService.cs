using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kosta.DevOpsChallenge.FileProcessor.DtoModel;
using Kosta.DevOpsChallenge.FileProcessor.WarehouseDb;
using Product = Kosta.DevOpsChallenge.FileProcessor.WarehouseDb.Product;

namespace Kosta.DevOpsChallenge.FileProcessor
{
    public class WarehouseService : IWarehouseService
    {
        private readonly WarehouseContext _dbContext = null;

        public WarehouseService(WarehouseContext dbContext)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            _dbContext = dbContext;
        }

        public bool IsTransmissionSummaryIdAlreadyProcessed(Guid transmissionSummaryId)
        {
            var transmissionFromDb = _dbContext.Transmissions.Find(transmissionSummaryId);
            return transmissionFromDb != null;
        }

        public void UpdateWarehouse(Kosta.DevOpsChallenge.FileProcessor.DtoModel.ProductTransmission productTransmission)
        {
            var productsToInsertInDb = new List<Product>();

            // Due to the way the database has been deployed, change tracking has not been enabled,
            // so need to manually track which records need to be added
            var categoriesFromDb = _dbContext.Categories.ToList();
            var originalCategoriesFromDb = new List<Category>(categoriesFromDb);
            var productsFromDb = _dbContext.Products.ToList();
            var originalProductsFromDb = new List<Product>(productsFromDb);

            foreach (var product in productTransmission.products)
            {
                ProcessCategories(product, categoriesFromDb);
                ProcessProduct(product, productsFromDb, categoriesFromDb);
            }

            _dbContext.Transmissions.Add(new Transmission
            {
                Id = productTransmission.transmissionsummary.id,
                ImportDate = DateTime.UtcNow
            });

            var categoriesToAddToDb = categoriesFromDb.Where(c => !originalCategoriesFromDb.Contains(c)).ToList();
            var productsToAddToDb = productsFromDb.Where(p => !originalProductsFromDb.Contains(p)).ToList();
            var productsToUpdateInDb = productsFromDb.Where(p => originalProductsFromDb.SingleOrDefault(op => op.Id == p.Id) != null).ToList();

            _dbContext.Categories.AddRange(categoriesToAddToDb);
            _dbContext.Products.AddRange(productsToAddToDb);
            _dbContext.Products.UpdateRange(productsToUpdateInDb);
            _dbContext.SaveChanges();
        }

        private void ProcessCategories(Kosta.DevOpsChallenge.FileProcessor.DtoModel.Product productDto, List<Category> categoriesFromDb)
        {
            var categoryArrayFromProductDto = productDto.category.Split(" > ");
            LinkedList<string> categoryTreeStructure = new LinkedList<string>(categoryArrayFromProductDto);
            foreach (var category in categoryArrayFromProductDto)
            {
                var categoryFromTree = categoryTreeStructure.Find(category);

                if (categoryFromTree.Previous == null)
                {
                    var parentCategoryFromDb = categoriesFromDb.SingleOrDefault(c => c.Name == category && c.ParentCategory == null);

                    if (parentCategoryFromDb == null)
                    {
                        categoriesFromDb.Add(new Category
                        {
                            Id = Guid.NewGuid(),
                            Name = category
                        });
                    }
                }
                else
                {
                    // Assuming that the same category can only appear once in the Categories db table (UNIQUE constraint enforced)
                    var parentCategoryFromDb = categoriesFromDb.SingleOrDefault(c => c.Name == categoryFromTree.Previous.Value);
                    var childCategoryFromDb = categoriesFromDb.SingleOrDefault(c => c.Name == category && c.ParentCategory == parentCategoryFromDb);

                    if (childCategoryFromDb == null)
                    {
                        categoriesFromDb.Add(new Category
                        {
                            Id = Guid.NewGuid(),
                            Name = category,
                            ParentCategory = parentCategoryFromDb
                        });
                    }
                }
            }
        }

        private void ProcessProduct(Kosta.DevOpsChallenge.FileProcessor.DtoModel.Product productDto, List<Product> productsFromDb, List<Category> categoriesFromDb)
        {
            var product = productsFromDb.SingleOrDefault(p =>
                productDto.location == p.Location &&
                productDto.sku == p.Sku
            );

            var categoryHierarchy = productDto.category.Split(" > ");
            var targetCategoryName = categoryHierarchy[categoryHierarchy.Length - 1];
            var targetCategory = categoriesFromDb.Single(c => c.Name == targetCategoryName);

            if (product == null)
            {
                productsFromDb.Add(new Product
                {
                    Id = Guid.NewGuid(),
                    Sku = productDto.sku,
                    Description = productDto.description,
                    Category = targetCategory,
                    Price = productDto.price,
                    Location = productDto.location,
                    Qty = productDto.qty
                });
            }
            else
            {
                product.Description = productDto.description;
                product.Category = targetCategory;
                product.Price = productDto.price;
                product.Qty = productDto.qty;
            }
        }

        public string GetWarehouseReport(string processedFileName, ValidationResultTypeEnum validationResult)
        {
            // The warehouse report is for an aggregate of level 3 categories
            var level1Category = _dbContext.Categories.Single(c => c.ParentCategory == null);
            var level2Categories = _dbContext.Categories.Where(c => c.ParentCategory == level1Category).ToList();
            var level3Categories = _dbContext.Categories.Where(c => level2Categories.Contains(c.ParentCategory)).ToList();
            var reportBuilder = new StringBuilder(validationResult.GetReportHeader(processedFileName));

            foreach (var category in level3Categories)
            {
                var bottomMostCategories = GetBottomMostCategoriesForCategory(category);
                var productsFromDb = _dbContext.Products.Where(p => bottomMostCategories.Contains(p.Category)).AsEnumerable().GroupBy(p => p.Location);

                foreach (var grouping in productsFromDb)
                {
                    var location = grouping.Key;
                    var qtySum = grouping.Sum(p => p.Qty);

                    reportBuilder.AppendLine($"{category.Name} - {location} - {qtySum}");
                }
            }

            return reportBuilder.ToString();
        }

        private List<Category> GetBottomMostCategoriesForCategory(Category parentCategory)
        {
            var lowestLevelCategories = new List<Category>();
            var childCategories = _dbContext.Categories.Where(c => c.ParentCategory == parentCategory).ToList();

            if (childCategories == null || childCategories.Count == 0)
            {
                lowestLevelCategories.Add(parentCategory);
            }

            foreach (var category in childCategories)
            {
                var grandChildCategories = GetBottomMostCategoriesForCategory(category);
                lowestLevelCategories.AddRange(grandChildCategories);
            }

            return lowestLevelCategories;
        }
    }
}
