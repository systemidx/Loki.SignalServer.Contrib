CREATE PROCEDURE [dbo].[sp_Rosters_GetEntityRostersForContact]
	@ContactId NVARCHAR(50)
AS
BEGIN
	SELECT [EntityId],[ContactId],[RosterGroups].[RosterGroupName] 
			FROM [Rosters] 
			LEFT OUTER JOIN [RosterGroups] 
				ON [Rosters].[RosterGroupId] = [RosterGroups].[RosterGroupId]			
			WHERE 
				ContactId = @ContactId AND 
				IsDeleted = 0
END
GO