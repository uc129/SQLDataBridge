-- Trade Payable Pipeline Tables
-- Run this script once (idempotent - all statements are guarded with IF NOT EXISTS)
-- Master tables (TP_MASTER_*) already exist in the DB — not recreated here.
-- appsettings.json MasterTables section maps logical names to the existing DB names.

-- ============================================================
-- PIPELINE RUN TRACKING
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TP_PipelineRun')
BEGIN
    CREATE TABLE TP_PipelineRun (
        RunId           NVARCHAR(50)     NOT NULL PRIMARY KEY,
        QuarterDate     DATE             NOT NULL,
        RevisionNumber  NVARCHAR(10)     NOT NULL,
        CurrentStepIndex INT             NOT NULL DEFAULT 0,
        Status          NVARCHAR(50)     NOT NULL DEFAULT 'Uploaded',
        StartedBy       NVARCHAR(200)    NULL,
        StartedAt       DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        CompletedAt     DATETIME2        NULL
    );
END

-- ============================================================
-- RAW FAGLL03 STAGING
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TP_FAGLL03_Raw')
BEGIN
    CREATE TABLE TP_FAGLL03_Raw (
        Id                  BIGINT IDENTITY(1,1) PRIMARY KEY,
        RunId               NVARCHAR(50)  NOT NULL,
        Invoice_Key         NVARCHAR(50)  NOT NULL,
        Document_Number     NVARCHAR(50)  NULL,
        Purchasing_Document NVARCHAR(50)  NULL,
        Invoice_Reference   NVARCHAR(100) NULL,
        Document_Header     NVARCHAR(500) NULL,
        Document_Type       NVARCHAR(10)  NULL,
        Company_Code        NVARCHAR(10)  NULL,
        Assignment          NVARCHAR(500) NULL,
        Vendor              NVARCHAR(50)  NULL,
        Vendor_Description  NVARCHAR(500) NULL,
        Invoice_Description NVARCHAR(MAX) NULL,
        Industry            NVARCHAR(10)  NULL,
        Amount_Local        DECIMAL(18,4) NULL,
        GL_Account          NVARCHAR(20)  NULL,
        GL_Description      NVARCHAR(500) NULL,
        Profit_Center       NVARCHAR(20)  NULL,
        Payment_Terms       NVARCHAR(50)  NULL,
        Document_Currency   NVARCHAR(10)  NULL,
        Amount_Doc          DECIMAL(18,4) NULL,
        Document_Date       DATE          NULL,
        Posting_Date        DATE          NULL,
        Payment_Date        DATE          NULL,
        User_Name           NVARCHAR(200) NULL,
        SOURCE              NVARCHAR(200) NULL,
        Edited              NVARCHAR(200) NULL,
        RevisionNumber      NVARCHAR(10)  NULL,
        Report_Date         DATE          NULL,
        QuarterEndDate      DATE          NULL,
        UploadedDate        DATETIME2     NULL,
        INDEX IX_TP_FAGLL03_Raw_RunId (RunId)
    );
END

-- ============================================================
-- STEP RESULT TABLES
-- Created dynamically by StepResultRepository on first write.
-- No DDL needed here.
-- ============================================================
