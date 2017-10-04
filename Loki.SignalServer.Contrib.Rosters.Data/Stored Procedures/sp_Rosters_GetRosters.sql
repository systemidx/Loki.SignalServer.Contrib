CREATE PROCEDURE [dbo].[sp_Rosters_GetRosters]
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
				[Rosters].[IsDeleted] = 0
END
GO