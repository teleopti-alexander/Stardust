USE [$(DBNAME)]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [JobDefinitions](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Serialized] [nvarchar](max) NULL,
	[Type] [nvarchar](max) NULL,
	[UserName] nvarchar(500) NOT NULL,
	[AssignedNode] [nvarchar](max) NULL,
	[JobProgress] [nvarchar](max) NULL,
	[Status] [nvarchar](max) NULL,
 CONSTRAINT [PK_JobDefinitions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))
GO

CREATE TABLE [dbo].[WorkerNodes](
	[Id] [uniqueidentifier] NOT NULL,
	[Url] [nvarchar](max) NULL,
 CONSTRAINT [PK_WorkerNodes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
))

GO

CREATE TABLE JobHistory(
	[JobId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](max) NULL,
	[CreatedBy] nvarchar(500) NOT NULL,
	[Created] DateTime NOT NULL,
	[Started] DateTime NULL,
	[Ended] DateTime NULL,
	[Serialized] [nvarchar](max) NULL,
	[Type] [nvarchar](max) NULL,
	[SentTo] nvarchar(max) NULL,
	[Result] [nvarchar](max) NULL
CONSTRAINT [PK_JobHistory] PRIMARY KEY CLUSTERED 
(
	[JobId] ASC
))

GO
ALTER TABLE dbo.JobHistory ADD CONSTRAINT
	DF_JobHistory_Created DEFAULT getutcdate() FOR Created
GO

CREATE TABLE JobHistoryDetail(
	[Id] int NOT NULL IDENTITY (1, 1),
	[JobId] [uniqueidentifier] NOT NULL,
	[Created] DateTime NOT NULL,
	[Detail] [nvarchar](max) NULL
)

GO
ALTER TABLE dbo.JobHistoryDetail ADD CONSTRAINT
	DF_JobHistoryDetail_Created DEFAULT getutcdate() FOR Created
GO