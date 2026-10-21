param(
    [string]$MySqlPath = "C:\Program Files\MySQL\MySQL Server 8.3\bin\mysql.exe",
    [string]$AdminUser = "root",
    [Security.SecureString]$AdminPassword
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path $MySqlPath)) { throw "Indica la ruta de mysql.exe con -MySqlPath." }
if (-not $AdminPassword) { $AdminPassword = Read-Host 'Contraseña del administrador local de MySQL' -AsSecureString }
$oldPassword = $env:MYSQL_PWD
$oldEncoding = $OutputEncoding

try {
    $env:MYSQL_PWD = [Net.NetworkCredential]::new('', $AdminPassword).Password
    $OutputEncoding = [Text.UTF8Encoding]::new($false)

    foreach ($file in @('001_catalogo.sql', '002_productos_ejemplo.sql', '003_separar_catalogo_inventario.sql')) {
        $path = (Join-Path $root "database/migrations/$file").Replace('\', '/')
        & $MySqlPath --host=localhost --user=$AdminUser --default-character-set=utf8mb4 --execute="source $path"
        if ($LASTEXITCODE -ne 0) { throw "Falló la migración $file." }
    }

    # Usuario exclusivo de esta entrega. Cada ejecución renueva su contraseña local.
    $password = [Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N')
    $sql = @"
CREATE USER IF NOT EXISTS 'sgcrn_avance01'@'localhost' IDENTIFIED BY '$password';
ALTER USER 'sgcrn_avance01'@'localhost' IDENTIFIED BY '$password';
GRANT SELECT, INSERT, UPDATE ON SGCRN_Avance01.* TO 'sgcrn_avance01'@'localhost';
"@
    $sql | & $MySqlPath --host=localhost --user=$AdminUser --default-character-set=utf8mb4
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo configurar el usuario del avance.' }

    $connection = "Server=localhost;Port=3306;Database=SGCRN_Avance01;User=sgcrn_avance01;Password=$password;DefaultCommandTimeout=30"
    @{ 'ConnectionStrings:SGCRN' = $connection } | ConvertTo-Json -Compress |
        dotnet user-secrets set --project (Join-Path $root 'SGCRN/SGCRN.csproj')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo guardar la conexión en secretos locales.' }

    Write-Host 'Avance configurado: SGCRN_Avance01, una tabla y tres productos de ejemplo.'
    Write-Host 'Ejecuta: dotnet run --project SGCRN/SGCRN.csproj'
    Write-Host 'Abre: http://localhost:5181'
}
finally {
    $env:MYSQL_PWD = $oldPassword
    $OutputEncoding = $oldEncoding
}
