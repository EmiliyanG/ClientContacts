
CREATE TABLE [dbo].[Organisation](
	[id] [int] NOT NULL,
	[name] [nvarchar](250) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Location](
	[Id] [int] NOT NULL,
	[Name] [nvarchar](250) NOT NULL,
	[organisationId] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Location]  WITH CHECK ADD FOREIGN KEY([organisationId])
REFERENCES [dbo].[Organisation] ([id])
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Contact](
	[ContactName] [nvarchar](250) NOT NULL,
	[IsDisabled] [bit] NOT NULL,
	[IsAdmin] [bit] NOT NULL,
	[email] [nvarchar](250) NULL,
	[telephone] [nvarchar](250) NULL,
	[organisationId] [int] NOT NULL,
	[id] [int] NOT NULL,
	[comments] [nvarchar](2000) NULL,
	[locationid] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Contact]  WITH CHECK ADD FOREIGN KEY([locationid])
REFERENCES [dbo].[Location] ([Id])
GO

ALTER TABLE [dbo].[Contact]  WITH CHECK ADD FOREIGN KEY([organisationId])
REFERENCES [dbo].[Organisation] ([id])
GO


