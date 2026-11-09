/*
  Apply once to the existing RaceDayNovaApiDB database after deploying the Part 3 API changes.
  This is a safe, nullable schema upgrade for the two new Azure Blob URL fields.
  Run against the same database configured by RaceDayNovaDb in API appsettings.json.
*/
IF COL_LENGTH('dbo.Events', 'BannerImageUrl') IS NULL
    ALTER TABLE dbo.Events ADD BannerImageUrl NVARCHAR(2048) NULL;
GO
IF COL_LENGTH('dbo.Users', 'ProfileImageUrl') IS NULL
    ALTER TABLE dbo.Users ADD ProfileImageUrl NVARCHAR(2048) NULL;
GO
