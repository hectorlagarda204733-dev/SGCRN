# SGCRN · Avance 01

**CU01: Registrar / Consultar Producto del Catálogo.** Sistema de Gestión y Catálogo de Remedios Naturales.

## Alcance

- Registrar productos con nombre único, descripción, unidad, precio e imagen por URL opcional.
- Consultar listado, buscar por nombre, filtrar por estado y consultar una ficha individual.
- Editar información y dar de baja lógicamente (`Activo = 0`), con confirmación y cancelación.
- Validar campos obligatorios, longitudes, precio no negativo con hasta dos decimales y URL HTTP/HTTPS.
- Conservar productos inactivos y evitar nombres duplicados, incluso entre inactivos.

Aplicación local desarrollada con **ASP.NET Core 10 Razor Pages, C#, MySqlConnector y MySQL 8**. Bootstrap se incluye localmente.

## Abrir y ejecutar

1. Instala .NET 10 SDK y MySQL 8 (con el servicio iniciado).
2. Abre `SGCRN-Avance01.slnx` en Visual Studio compatible con .NET 10, o esta carpeta en tu editor.
3. En PowerShell, desde la raíz del repositorio, configura la base independiente:

   ```powershell
   .\scripts\Preparar-Avance.ps1
   ```

   El script solicita la contraseña del administrador local. Crea `SGCRN_Avance01`, su única tabla, tres productos de ejemplo y un usuario exclusivo. La conexión real se guarda en **Secretos de usuario de .NET**, fuera de Git. Si tu instalación tiene otra ruta:

   ```powershell
   .\scripts\Preparar-Avance.ps1 -MySqlPath "C:\ruta\a\mysql.exe"
   ```

   Se puede repetir: los ejemplos existentes se conservan y la contraseña del usuario exclusivo del avance se renueva junto con el secreto local.

4. Inicia la aplicación:

   ```powershell
   dotnet run --project SGCRN/SGCRN.csproj
   ```

5. Abre **http://localhost:5181**. También puedes iniciar el perfil `SGCRN-Avance01` desde Visual Studio.

### Configuración manual

Ejecuta en MySQL Workbench `database/migrations/001_catalogo.sql` y después `002_productos_ejemplo.sql`. Configura un usuario con permisos SELECT, INSERT y UPDATE sobre `SGCRN_Avance01` y registra su conexión local:

```powershell
dotnet user-secrets set "ConnectionStrings:SGCRN" "Server=localhost;Port=3306;Database=SGCRN_Avance01;User=TU_USUARIO;Password=TU_CLAVE" --project SGCRN/SGCRN.csproj
```

El valor de `appsettings.json` es ilustrativo; no contiene una contraseña real. El perfil de desarrollo carga el secreto automáticamente.

## Estructura

```text
SGCRN/
  Models/                 Entidad del producto y validaciones de entrada
  Data/                   Creación y apertura de conexión MySQL
  Services/               Operaciones de productos y SQL parametrizado
  Pages/ProductosTerminados/
                          Listado, registro, consulta, edición y baja lógica
  Program.cs              Registro de servicios y configuración Razor Pages
database/
  migrations/             Esquema de una sola tabla y ejemplos
scripts/
  Preparar-Avance.ps1      Preparación de la base y conexión local
  Verificar-CU01.py        Prueba integrada HTTP contra la aplicación iniciada
```

Los `PageModel` (`*.cshtml.cs`) coordinan las solicitudes de Razor Pages. Las validaciones están en `ProductoTerminadoInput`; `ProductoTerminadoService` implementa las operaciones del catálogo y la persistencia mediante SQL parametrizado.

## Comprobación

```powershell
dotnet build SGCRN-Avance01.slnx
```

Con la aplicación iniciada y Python 3 instalado, `python scripts/Verificar-CU01.py` comprueba registro, duplicados, validaciones, consulta filtrada, edición y baja lógica. Genera un producto de prueba que queda inactivo.

Bootstrap se distribuye bajo licencia MIT; consulta `SGCRN/wwwroot/lib/bootstrap/LICENSE`.
