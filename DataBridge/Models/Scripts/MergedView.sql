USE [TradeMSEDDetails_UAT]
GO

/****** Object:  View [dbo].[QTR34022]    Script Date: 13-05-2026 14:40:21 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER VIEW [dbo].[QTR34022] AS 
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
    vd.[c_vendor_name] as Joined_VendorName,
    vd.[c_vendor_pan] as Joined_VendorPAN,
    vd.[City] as Joined_Vendor_City,
    po_vendor.[vendor_name] as PO_Reference_VendorName -- Added this for visibility
FROM [TradeMSEDDetails_UAT].[dbo].[my_table] as pop_data
-- Use COALESCE or prioritized Joins to avoid duplicates from OR logic
LEFT JOIN PO_Vendor_Data as po_vendor 
    ON pop_data.[purchasing_document] = po_vendor.[ebeln]
LEFT JOIN VendorDetails vd 
    ON ((pop_data.[vendor] = vd.[pkc_vendor_code] AND pop_data.company_code = vd.[pkc_company_code]) 
    OR (pop_data.[purchasing_document] = po_vendor.[ebeln] and  po_vendor.[lifnr] = vd.[pkc_vendor_code]))
GO


