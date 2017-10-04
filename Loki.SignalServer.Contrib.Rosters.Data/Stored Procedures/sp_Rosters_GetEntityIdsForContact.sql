CREATE PROCEDURE [dbo].[sp_Rosters_GetEntityIdsForContact]
	@ContactId NVARCHAR(50)
AS
BEGIN
	SELECT [EntityId]
			FROM [Rosters]
			WHERE 
				ContactId = @ContactId AND 
				IsDeleted = 0
END
GO