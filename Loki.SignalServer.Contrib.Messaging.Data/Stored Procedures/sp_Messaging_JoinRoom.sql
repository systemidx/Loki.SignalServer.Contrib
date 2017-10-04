CREATE PROCEDURE [dbo].[sp_Messaging_JoinRoom]
	@RoomId		UNIQUEIDENTIFIER,
	@EntityId	NVARCHAR(50)
AS
BEGIN
	MERGE INTO Attendees 
		AS _target
	USING 
		(VALUES (@EntityId, @RoomId)) AS _source (EntityId, RoomId)
	ON 
		_target.EntityId = _source.EntityId AND 
		_target.RoomId = _source.RoomId
	WHEN MATCHED THEN 
		UPDATE SET _target.IsActive = 1
	WHEN NOT MATCHED THEN 
		INSERT (RoomId, EntityId, IsActive) VALUES (@RoomId, @EntityId, 1)
	;
END
GO