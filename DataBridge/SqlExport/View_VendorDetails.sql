USE [TradeMSEDDetails_UAT]
GO

/****** Object:  View [dbo].[QTR34022]    Script Date: 20-05-2026 14:26:07 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER VIEW [dbo].[View_FNATool_VendorDetailsPipeline_1] AS 
WITH VendorDetails as (
 SELECT * FROM OPENQUERY ( [SQLAPP.ENCNET.COM],
      'SELECT [pkc_vendor_code]
      ,[pkc_company_code]
      ,MAX([c_region_code]) c_region_code
      ,MAX([c_email]) as c_email
      ,MAX([c_vendor_name]) c_vendor_name
      ,MAX([c_vendor_pan]) c_vendor_pan
      ,MAX([CIN_Number]) CIN_Number
      ,MAX([City]) City
      ,MAX([District])  District
      ,MAX([Houseno_street])  Houseno_street
      ,MAX([industry_type])  industry_type
      ,MAX([Postalcode])  Postalcode
      ,MAX([Street2])  Street2
      ,MAX([Street3])  Street3
      ,MAX([TAN_NO])  TAN_NO
      ,MAX([Telephone])  Telephone
      ,MAX([Type_of_vendor])  Type_of_vendor
  FROM [Lnt_PO_Data].[dbo].[View_VendorDataforBG]
  GROUP BY pkc_vendor_code, pkc_company_code')
  ),
PO_Vendor_Data AS (
    SELECT * FROM OPENQUERY([SQLAPP.ENCNET.COM], 
    'SELECT ebeln, MAX(lifnr) as lifnr, MAX(name1) as vendor_name 
     FROM Lnt_PO_Data.dbo.podata GROUP BY ebeln')
)
SELECT
    pop_data.*,
    po_vendor.[lifnr],
    COALESCE(vd_direct.[c_vendor_name], vd_po.[c_vendor_name]) AS Joined_VendorName,
    COALESCE(vd_direct.[c_vendor_pan],  vd_po.[c_vendor_pan])  AS Joined_VendorPAN,
    COALESCE(vd_direct.[City],          vd_po.[City])          AS Joined_Vendor_City,
    po_vendor.[vendor_name] as PO_Reference_VendorName
FROM [TradeMSEDDetails_UAT].[dbo].[FNATool_VendorDetailsPipeline_1] as pop_data
LEFT JOIN PO_Vendor_Data as po_vendor
    ON pop_data.[purchasing_document] = po_vendor.[ebeln]
OUTER APPLY (
    SELECT TOP 1 vd.c_vendor_name, vd.c_vendor_pan, vd.City
    FROM VendorDetails vd
    WHERE vd.[pkc_vendor_code]  = pop_data.[vendor]
      AND vd.[pkc_company_code] = pop_data.company_code
) vd_direct
OUTER APPLY (
    SELECT TOP 1 vd.c_vendor_name, vd.c_vendor_pan, vd.City
    FROM VendorDetails vd
    WHERE vd.[pkc_vendor_code] = po_vendor.[lifnr]
      AND vd_direct.c_vendor_name IS NULL
) vd_po
GO


