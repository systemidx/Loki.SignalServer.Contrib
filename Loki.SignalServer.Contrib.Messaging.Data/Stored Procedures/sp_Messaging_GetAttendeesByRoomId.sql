CREATE PROCEDURE [dbo].[sp_Messaging_GetAttendeesByRoomId]
	@RoomId UNIQUEIDENTIFIER	
AS
BEGIN
	SELECT 
		[Attendees].EntityId
	FROM
		[Attendees]
	WHERE
		[Attendees].RoomId = @RoomId AND
		[Attendees].IsActive = 1		
END
GO