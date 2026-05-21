USE [TradeMSEDDetails_UAT]
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_CreateVendorDetailsPipelineView]
    @TableName  NVARCHAR(128),
    @ViewSuffix INT            -- 1, 2, or 3
AS
BEGIN
    SET NOCOUNT ON;

    IF @ViewSuffix < 1 OR @ViewSuffix > 9
    BEGIN
        RAISERROR('ViewSuffix must be between 1 and 9.', 16, 1);
        RETURN;
    END

    DECLARE @ViewName NVARCHAR(256)
        = N'View_FNATool_VendorDetailsPipeline_' + CAST(@ViewSuffix AS NVARCHAR(10));

    DECLARE @Sql NVARCHAR(MAX) = N'
CREATE OR ALTER VIEW [dbo].[' + @ViewName + N'] AS
WITH VendorDetails AS (
    SELECT * FROM OPENQUERY([SQLAPP.ENCNET.COM],
        ''SELECT [pkc_vendor_code]
              ,[pkc_company_code]
              ,MAX([c_region_code])        c_region_code
              ,MAX([c_email])              c_email
              ,MAX([c_vendor_name])        c_vendor_name
              ,MAX([c_vendor_pan])         c_vendor_pan
              ,MAX([CIN_Number])           CIN_Number
              ,MAX([City])                 City
              ,MAX([District])             District
              ,MAX([Houseno_street])       Houseno_street
              ,MAX([industry_type])        industry_type
              ,MAX([Postalcode])           Postalcode
              ,MAX([Street2])              Street2
              ,MAX([Street3])              Street3
              ,MAX([TAN_NO])               TAN_NO
              ,MAX([Telephone])            Telephone
              ,MAX([Type_of_vendor])       Type_of_vendor
          FROM [Lnt_PO_Data].[dbo].[View_VendorDataforBG]
          GROUP BY pkc_vendor_code, pkc_company_code'')
),
PO_Vendor_Data AS (
    SELECT * FROM OPENQUERY([SQLAPP.ENCNET.COM],
        ''SELECT ebeln, MAX(lifnr) AS lifnr, MAX(name1) AS vendor_name
          FROM Lnt_PO_Data.dbo.podata GROUP BY ebeln'')
)
SELECT
    pop_data.*,
    po_vendor.[lifnr],
    COALESCE(vd_direct.[c_vendor_name], vd_po.[c_vendor_name]) AS Joined_VendorName,
    COALESCE(vd_direct.[c_vendor_pan],  vd_po.[c_vendor_pan])  AS Joined_VendorPAN,
    COALESCE(vd_direct.[City],          vd_po.[City])          AS Joined_Vendor_City,
    po_vendor.[vendor_name] AS PO_Reference_VendorName
FROM [TradeMSEDDetails_UAT].[dbo].' + QUOTENAME(@TableName) + N' AS pop_data
LEFT JOIN PO_Vendor_Data AS po_vendor
    ON pop_data.[purchasing_document] = po_vendor.[ebeln]
-- Primary: match vendor code + company code directly; TOP 1 prevents duplicates if source data is non-unique
OUTER APPLY (
    SELECT TOP 1 vd.c_vendor_name, vd.c_vendor_pan, vd.City
    FROM VendorDetails vd
    WHERE vd.[pkc_vendor_code]  = pop_data.[vendor]
      AND vd.[pkc_company_code] = pop_data.company_code
) vd_direct
-- Fallback: PO-derived lifnr; only fires when direct match returned nothing;
-- TOP 1 prevents duplicates when the same lifnr exists across multiple company codes
OUTER APPLY (
    SELECT TOP 1 vd.c_vendor_name, vd.c_vendor_pan, vd.City
    FROM VendorDetails vd
    WHERE vd.[pkc_vendor_code] = po_vendor.[lifnr]
      AND vd_direct.c_vendor_name IS NULL
) vd_po;
';

    EXEC sp_executesql @Sql;
END
GO

-- Usage: run once per view you need to create (or recreate after a table rename).
-- EXEC [dbo].[sp_CreateVendorDetailsPipelineView] @TableName = 'FNATool_VendorDetailsPipeline_1', @ViewSuffix = 1;
-- EXEC [dbo].[sp_CreateVendorDetailsPipelineView] @TableName = 'my_table_2', @ViewSuffix = 2;
-- EXEC [dbo].[sp_CreateVendorDetailsPipelineView] @TableName = 'my_table_3', @ViewSuffix = 3;
