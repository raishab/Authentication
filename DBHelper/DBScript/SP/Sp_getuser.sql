

Create OR ALTER Procedure Sp_getuser
(
   @EmailId varchar(150),
   @Password varchar(50)
 )
   AS
   BEGIN TRY
   IF EXISTS(Select 1 From tblUser Where EmailId=@EmailId AND UserPassword=@Password AND IsActive=1)
   BEGIN
	Select Id,EmailId,RoleId From tblUser Where EmailId=@EmailId AND UserPassword=@Password AND IsActive=1
   END
   ELSE
   BEGIN
     SELECT 'Invalid credentials. Please try again.' AS OtpErrorMessage;  
   END
   END TRY
   BEGIN CATCH
    DECLARE @ErrorMessage  NVARCHAR(4000) = ERROR_MESSAGE();                
	DECLARE @ErrorSeverity INT = ERROR_SEVERITY();                
	DECLARE @ErrorState    INT = ERROR_STATE();                
    EXEC Sp_ErrorLog @ErrorMessage,@ErrorSeverity,@ErrorState;  
   END CATCH
