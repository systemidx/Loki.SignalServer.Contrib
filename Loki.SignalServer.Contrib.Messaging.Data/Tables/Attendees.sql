CREATE TABLE [dbo].[Attendees]
(
    [RoomId] UNIQUEIDENTIFIER NOT NULL,
	[EntityId] NVARCHAR(50) NOT NULL,
	[IsActive] BIT NOT NULL,
	[TimestampJoined] DATETIME2(7) NOT NULL DEFAULT SYSUTCDATETIME(),
	[TimestampLeft] DATETIME2(7) NULL
    
    CONSTRAINT [FK_RoomAttendees_ToRooms] FOREIGN KEY (RoomId) REFERENCES [Rooms]([RoomId]), 
    PRIMARY KEY ([RoomId], [EntityId])
)