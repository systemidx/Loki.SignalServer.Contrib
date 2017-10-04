CREATE PROCEDURE [dbo].[sp_Messaging_CloseRoom]
	@RoomId UNIQUEIDENTIFIER	
AS
BEGIN
	UPDATE [Rooms]
		SET 
			IsActive = 0,
			TimestampClosed = SYSUTCDATETIME()
		WHERE
			RoomId = @RoomId
END
GO