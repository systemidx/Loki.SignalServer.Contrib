CREATE PROCEDURE [dbo].[sp_Messaging_GetRoomByRoomId]
	@RoomId UNIQUEIDENTIFIER	
AS
BEGIN
	SELECT 
		[Rooms].RoomId, [Rooms].IsActive, [Rooms].TimestampClosed, [Rooms].TimestampCreated
	FROM
		[Rooms]
	WHERE
		[Rooms].RoomId = @RoomId
END
GO