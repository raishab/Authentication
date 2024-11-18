CREATE OR ALTER PROCEDURE SP_GetAllUser
AS
BEGIN TRY
  Select Id,EmailId,RoleId,FristName+ +LastName AS Name From tblUser
END TRY
BEGIN CATCH
	DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();                
	DECLARE @ErrorSeverity INT = ERROR_SEVERITY();                
	DECLARE @ErrorState    INT = ERROR_STATE();                
	EXEC Sp_ErrorLog @ErrorMessage,@ErrorSeverity,@ErrorState; 
END CATCH