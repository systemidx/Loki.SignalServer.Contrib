CREATE PROCEDURE [dbo].[sp_Messaging_CreateRoom]
	@RoomId UNIQUEIDENTIFIER	
AS
BEGIN
	INSERT INTO [Rooms] (RoomId, IsActive, TimestampCreated, TimestampClosed)
	VALUES (@RoomId, 1, SYSUTCDATETIME(), null)
END
GO