


Create Database dbUser
GO
Use dbUser
GO

Create table tblUser
(
  Id int Primary key identity(1,1) not Null,
  FristName varchar(250) not null,
  LastName varchar(250) not null,
  EmailId  Varchar(150) Not NULL,
  MobileNumber varchar(10) Not NULL,
  RoleId int Not NULL,
  UserPassword Varchar(150),
  IsActive bit Not NULL,
  CreatedBy int Not NULL,
  CreatedDate Datetime Not NULL,
  UpdatedBy int Null,
  UpdatedDate Datetime Null
)
GO
CREATE TABLE ErrorLog(
	Id INT IDENTITY(1,1) NOT NULL,
	ErrorMessage  NVARCHAR(4000),
    ErrorSeverity INT, 
    ErrorState	INT,
	CreatedDate DATETIME NULL
);
GO
Insert into tblUser (FristName,LastName,EmailId,MobileNumber,RoleId,UserPassword,IsActive,CreatedBy,CreatedDate) values('Anand','Rai','raishab.rai36@gamil.com','9999745665',1,'AnandRai@123',1,1,GETDATE())