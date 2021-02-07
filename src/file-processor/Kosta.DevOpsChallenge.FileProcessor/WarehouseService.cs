using System;
using System.Collections.Generic;
using System.Linq;
using Kosta.DevOpsChallenge.FileProcessor.WarehouseDb;

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
            var transmissionSummaryToInsertInDb = new Transmission();

            // Due to the way the database has been deployed, change tracking has not been enabled,
            // so need to manually track which records need to be added
            var categoriesFromDb = _dbContext.Categories.ToList();
            var originalCategoriesFromDb = new List<Category>(categoriesFromDb);

            foreach (var product in productTransmission.products)
            {
                ProcessCategories(product, categoriesFromDb);
            }

            var categoriesToAddToDb = categoriesFromDb.Where(c => !originalCategoriesFromDb.Contains(c));
            _dbContext.Categories.AddRange(categoriesToAddToDb);
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

        public IEnumerable<Product> GetProductInventoryForCategory(string categoryName)
        {
            throw new NotImplementedException();
        }

        public string GetWarehouseReport(string processedFileName, Kosta.DevOpsChallenge.FileProcessor.DtoModel.ValidationResultTypeEnum validationResult)
        {
            throw new NotImplementedException();
        }
    }
}
