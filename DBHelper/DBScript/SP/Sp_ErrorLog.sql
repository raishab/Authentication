
 CREATE OR ALTER PROCEDURE Sp_ErrorLog(
@ErrorMessage  NVARCHAR(4000),
@ErrorSeverity INT, 
@ErrorState	INT)
AS
BEGIN 
INSERT INTO ErrorLog(ErrorMessage,ErrorSeverity,ErrorState,CreatedDate)
	VALUES(@ErrorMessage,@ErrorSeverity,@ErrorState,GETDATE());
SELECT  @ErrorMessage AS ErrorMessage,@ErrorSeverity AS ErrorSeverity,@ErrorState AS ErrorState; 
END
  