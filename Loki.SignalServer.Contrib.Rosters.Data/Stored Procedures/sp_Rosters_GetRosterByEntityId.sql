CREATE PROCEDURE [dbo].[sp_Rosters_GetRosterByEntityId]
	@EntityId NVARCHAR(50)
AS
BEGIN
	SELECT	
		[Rosters].[EntityId],
		[Rosters].[ContactId],
		[RosterGroups].[RosterGroupName] 
			FROM [Rosters] 
			LEFT OUTER JOIN [RosterGroups] 
				ON [Rosters].[RosterGroupId] = [RosterGroups].[RosterGroupId]
			WHERE 
				[Rosters].[EntityId] = @EntityId AND
				[Rosters].[IsDeleted] = 0
END
GO