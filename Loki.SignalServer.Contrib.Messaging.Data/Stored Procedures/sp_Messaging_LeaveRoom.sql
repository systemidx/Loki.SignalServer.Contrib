CREATE PROCEDURE [dbo].[sp_Messaging_LeaveRoom]
	@RoomId		UNIQUEIDENTIFIER,
	@EntityId	NVARCHAR(50)
AS
BEGIN
	UPDATE [Attendees]
		SET 
			TimestampLeft = SYSUTCDATETIME(),
			IsActive = 0
		WHERE
			RoomId = @RoomId AND
			EntityId = @EntityId
END
GO