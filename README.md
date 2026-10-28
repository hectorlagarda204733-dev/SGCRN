# SGCRN · Gestión de remedios naturales

Aplicación ASP.NET Core 10 **MVC** para los casos de uso priorizados del Sistema de Gestión y Catálogo de Remedios Naturales:

## Alcance

- **CU01, Gestionar Materias Primas:** alta, consulta/búsqueda, edición y baja lógica de insumos.
- **CU02, Gestionar Productos Terminados:** alta, consulta/búsqueda, edición y baja lógica de productos.
- **CU05, Registrar Entrada de Inventario:** compra de una materia prima **o** conclusión de producción de un producto, con cantidad positiva, costo y observaciones; cada entrada incrementa el stock del destino y conserva un folio consultable.
- Los catálogos no permiten capturar ni modificar existencias. Productos e insumos empiezan con stock cero y los movimientos se registran mediante CU05.
- Los nombres son únicos incluso entre registros inactivos. La baja lógica no elimina el historial.

Se usa **EF Core 10 / MySql.EntityFrameworkCore**, MySQL 8 y vistas Razor con Bootstrap local. El incremento condicional de stock emplea SQL parametrizado a través de `DbContext` dentro de una transacción para evitar pérdidas entre entradas concurrentes. El acceso a catálogos, entradas y registros se realiza mediante repositorios y servicios.

## Abrir y ejecutar

1. Instala .NET 10 SDK y MySQL 8 (con el servicio iniciado).
2. Abre `SGCRN.slnx` en Visual Studio compatible con .NET 10, o esta carpeta en tu editor.
3. En PowerShell, desde la raíz del repositorio, configura la base del proyecto:

   ```powershell
   .\scripts\Preparar-SGCRN.ps1
   ```

   El script solicita la contraseña del administrador local. Crea la base independiente `SGCRN_Proyecto`, sus tres tablas, ejemplos de ambos catálogos y un usuario exclusivo. La conexión real se guarda en **Secretos de usuario de .NET**, fuera de Git. No modifica bases anteriores. Si tu instalación tiene otra ruta:

   ```powershell
   .\scripts\Preparar-SGCRN.ps1 -MySqlPath "C:\ruta\a\mysql.exe"
   ```

   Se puede repetir: los ejemplos existentes se conservan y la contraseña del usuario exclusivo se renueva junto con el secreto local.

4. Inicia la aplicación:

   ```powershell
   dotnet run --project SGCRN/SGCRN.csproj
   ```

5. Abre **http://localhost:5181**. También puedes iniciar el perfil `SGCRN` desde Visual Studio.

### Configuración manual

Ejecuta en MySQL Workbench los scripts de `database/migrations` en orden: `001_esquema.sql` y `002_datos_ejemplo.sql`. Se pueden repetir sin borrar datos. Configura un usuario con permisos SELECT, INSERT y UPDATE sobre `SGCRN_Proyecto` y registra su conexión local:

```powershell
dotnet user-secrets set "ConnectionStrings:SGCRN" "Server=localhost;Port=3306;Database=SGCRN_Proyecto;User=TU_USUARIO;Password=TU_CLAVE" --project SGCRN/SGCRN.csproj
```

El valor de `appsettings.json` es ilustrativo; no contiene una contraseña real. El perfil de desarrollo carga el secreto automáticamente.

## Estructura

```text
SGCRN/
  Models/                 Entidades de materias, productos y entradas; modelos de entrada
  Views/                  Vistas Razor de CU01, CU02 y CU05
  Controllers/            Acciones HTTP de los tres casos de uso
  Services/               Reglas de catálogos y entradas de inventario
  Data/                   Repositorios y SGCRNDbContext (EF Core)
  Program.cs              Registro de servicios y configuración de rutas MVC
database/
  migrations/             Tres tablas con restricciones y datos de ejemplo
scripts/
  Preparar-SGCRN.ps1       Preparación de la base y conexión local
  Verificar-SGCRN.py       Pruebas HTTP integradas de los tres casos
```

La aplicación utiliza **Modelo-Vista-Controlador** con lógica de negocio separada del acceso a datos. `InventarioService` valida origen/destino y el repositorio registra la entrada y el incremento condicional de existencias en una sola transacción. El esquema exige cantidad positiva y destino exclusivo (materia prima **XOR** producto terminado).

```text
Vista Razor → Controller → Service → Repository → SGCRNDbContext (EF Core) → MySQL
```

Las rutas principales son `/MateriaPrima`, `/Producto` y `/EntradaInventario`. El registro de movimientos está en `/EntradaInventario/Create`.

**Estado del prototipo:** el desarrollo local aún no implementa inicio de sesión ni control de roles. El diagrama de despliegue Ubuntu/Nginx/TLS representa el objetivo, no una instalación realizada; no exponer esta aplicación sin agregar autorización de administrador.

## Comprobación

```powershell
dotnet build SGCRN.slnx
```

Con la aplicación iniciada y Python 3 instalado, `python scripts/Verificar-SGCRN.py` comprueba los tres casos y deja registros de prueba identificables para inspeccionarlos.

Bootstrap se distribuye bajo licencia MIT; consulta `SGCRN/wwwroot/lib/bootstrap/LICENSE`.
