CREATE PROCEDURE [dbo].[sp_Metadata_GetMetadataByEntityId]
	@EntityId NVARCHAR(50)
AS
BEGIN
	SELECT	
		[Metadata].[EntityMetadata]
		FROM [Metadata]
		WHERE
			[Metadata].[EntityId] = @EntityId AND
			[Metadata].[IsDeleted] = 0
END
GO