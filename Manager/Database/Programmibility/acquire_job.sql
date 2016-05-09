USE [$(DBNAME)]

IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[AcquireQueuedJob]') AND type in (N'P', N'PC'))
DROP PROCEDURE [Stardust].[AcquireQueuedJob]

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [Stardust].[AcquireQueuedJob]
AS
BEGIN

DECLARE 
@Idd nvarchar(100)

BEGIN TRAN
	SELECT TOP 1 @Idd = [JobId]
				FROM [Stardust].[JobQueue] 
				where Tagged is null
				ORDER BY Created

	update [Stardust].[JobQueue]
	set Tagged = '0'
	where JobId = @Idd	

COMMIT TRAN

SELECT * FROM [Stardust].[JobQueue] WHERE JobId = @Idd
END 

