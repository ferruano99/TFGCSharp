IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'IIS APPPOOL\TFG')
BEGIN
    CREATE LOGIN [IIS APPPOOL\TFG] 
      FROM WINDOWS WITH DEFAULT_DATABASE=TransportePublico

END
GO
CREATE USER [TFGUser] 
  FOR LOGIN [IIS APPPOOL\DefaultAppPool]
GO
EXEC sp_addrolemember 'db_owner', 'TFGUser'
GO