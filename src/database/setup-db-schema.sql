USE [INSERT_DATABASE_NAME];
GO

CREATE TABLE dbo.Transmission
(
    Id            UNIQUEIDENTIFIER    NOT NULL,
    ImportDate    DATETIME            NOT NULL,

    CONSTRAINT PK_Transmission_Id PRIMARY KEY CLUSTERED (Id)
);
GO

CREATE TABLE dbo.Category
(
    Id                  UNIQUEIDENTIFIER    NOT NULL,
    Name                NVARCHAR(50)        NOT NULL,
    ParentCategoryId    UNIQUEIDENTIFIER,

    CONSTRAINT PK_Category_Id PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ParentCategory_ChildCategory
        FOREIGN KEY (ParentCategoryId) REFERENCES dbo.Category (Id)
);
GO

CREATE TABLE dbo.Product
(
    Id            UNIQUEIDENTIFIER    NOT NULL,
    Sku           NVARCHAR(10)        NOT NULL,
    Description   NVARCHAR(255)       NOT NULL,
    CategoryId    UNIQUEIDENTIFIER    NOT NULL,
    Price         DECIMAL(6,2)        NOT NULL,
    Location      NVARCHAR(255)       NOT NULL,
    Qty           INT                 NOT NULL,

    CONSTRAINT PK_Product_Id PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Category_Product
        FOREIGN KEY (CategoryId) REFERENCES dbo.Category (Id),
    CONSTRAINT CK_Price CHECK (Price > 0.00),
    CONSTRAINT CK_Qty CHECK (Qty > 0)
);
GO
