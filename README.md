# SGCRN · Gestión de remedios naturales

Aplicación ASP.NET Core 10 **MVC** para los casos de uso priorizados del Sistema de Gestión y Catálogo de Remedios Naturales:

## Alcance

- **CU01, Gestionar Materias Primas:** alta, consulta/búsqueda, edición y baja lógica de insumos.
- **CU02, Gestionar Productos Terminados:** alta, consulta/búsqueda, edición y baja lógica de productos.
- **CU05, Registrar Entrada de Inventario:** compra de una materia prima **o** conclusión de producción de un producto, con cantidad positiva, costo y observaciones; cada entrada incrementa el stock del destino y conserva un folio consultable.
- **CU06, Registrar Salida / Distribución:** venta directa o entrega de un producto terminado a un cliente/punto de venta, con cantidad positiva y observaciones; descuenta existencias y conserva un folio consultable.
- **Clientes y puntos de venta (soporte básico de CU03):** registro, búsqueda y consulta de destinatarios para las salidas. No incluye todavía edición ni baja de clientes.
- Los catálogos no permiten capturar ni modificar existencias. Productos e insumos empiezan con stock cero; CU05 suma existencias y CU06 descuenta productos terminados.
- Los nombres son únicos incluso entre registros inactivos. La baja lógica no elimina el historial.

Se usa **EF Core 10 / MySql.EntityFrameworkCore**, MySQL 8 y vistas Razor con Bootstrap local. El incremento condicional de stock emplea SQL parametrizado a través de `DbContext` dentro de una transacción para evitar pérdidas entre entradas concurrentes. El acceso a catálogos, entradas y registros se realiza mediante repositorios y servicios.

### Salidas y destinatarios

Desde **Salidas → Registrar salida**, selecciona venta directa o distribución, producto,
destinatario y cantidad. También puedes comenzar desde la ficha del producto o del cliente.
Se muestran las existencias y un resumen previo; la disponibilidad se valida otra vez al guardar.
Si falta un destinatario, regístralo desde el enlace del formulario: se conserva el producto
seleccionado y se vuelve a una nueva salida con el destinatario preseleccionado.

El descuento condicional (`StockActual >= Cantidad`) y la inserción se confirman en una sola
transacción. Una salida rechazada no modifica existencias; si falla la persistencia se revierte
el descuento. La referencia única de cada formulario evita duplicar una salida al reenviarlo,
incluso de forma simultánea. Un nuevo formulario permite registrar otra entrega real.
Se conserva la unidad usada en el movimiento. Las salidas registradas no se editan ni eliminan
desde la aplicación. CU06 controla cantidades del inventario general; no factura ni implementa
todavía el seguimiento de stock por sucursal (CU07) ni el consumo de materias primas.

## Imágenes y navegación

Los productos admiten una imagen JPG, PNG o WebP mediante selección de archivo,
arrastre o pegado en la zona de carga. También se puede usar un enlace HTTP(S).
Se incluye vista previa, reemplazo y eliminación de la referencia desde edición.
El límite es 2 MB, 6000 px por lado y 16 megapíxeles. El servidor decodifica el
archivo, normaliza su orientación y lo guarda como PNG de hasta 1600 px por lado.

Los archivos se guardan en `SGCRN/wwwroot/uploads/productos/`, excluidos de Git.
En un despliegue, esta carpeta debe ser persistente y respaldarse junto con MySQL.
Al reemplazar o quitar una imagen se conserva el archivo anterior; solo se
descartan automáticamente las cargas nuevas cuyo registro falla.
El procesamiento utiliza SkiaSharp (licencia MIT), con componentes nativos para
Windows y Linux. No requiere modificar el esquema de la base de datos.

La navegación incluye regreso visible, retorno después de guardar y conservación
de filtros por pestaña. Las entradas abiertas desde una ficha preseleccionan el
artículo. Las transiciones respetan la preferencia de movimiento reducido.

## Abrir y ejecutar

1. Instala .NET 10 SDK y MySQL 8 (con el servicio iniciado).
2. Abre `SGCRN.slnx` en Visual Studio compatible con .NET 10, o esta carpeta en tu editor.
3. En PowerShell, desde la raíz del repositorio, configura la base del proyecto:

   ```powershell
   .\scripts\Preparar-SGCRN.ps1
   ```

   El script solicita la contraseña del administrador local. Crea la base independiente `SGCRN_Proyecto`, sus cinco tablas, ejemplos de ambos catálogos y un usuario exclusivo. La conexión real se guarda en **Secretos de usuario de .NET**, fuera de Git. No modifica bases anteriores. Si tu instalación tiene otra ruta:

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

Ejecuta en MySQL Workbench los scripts de `database/migrations` en orden: `001_esquema.sql`, `002_datos_ejemplo.sql` y `003_salidas_distribucion.sql`. Se pueden repetir sin borrar datos. Si ya tienes la versión de entradas instalada, basta ejecutar `003_salidas_distribucion.sql` para agregar las dos tablas nuevas sin cambiar catálogos ni existencias. Configura un usuario con permisos SELECT, INSERT y UPDATE sobre `SGCRN_Proyecto` y registra su conexión local:

```powershell
dotnet user-secrets set "ConnectionStrings:SGCRN" "Server=localhost;Port=3306;Database=SGCRN_Proyecto;User=TU_USUARIO;Password=TU_CLAVE" --project SGCRN/SGCRN.csproj
```

El valor de `appsettings.json` es ilustrativo; no contiene una contraseña real. El perfil de desarrollo carga el secreto automáticamente.

## Estructura

```text
SGCRN/
  Models/                 Entidades, catálogos y modelos de entrada de formularios
  Views/                  Vistas Razor de catálogos, destinatarios, entradas y salidas
  Controllers/            Acciones HTTP de los casos de uso
  Services/               Reglas de catálogos y movimientos de inventario
  Data/                   Repositorios y SGCRNDbContext (EF Core)
  Program.cs              Registro de servicios y configuración de rutas MVC
database/
  migrations/             Cinco tablas con restricciones y datos de ejemplo
scripts/
  Preparar-SGCRN.ps1       Preparación de la base y conexión local
  Verificar-SGCRN.py       Pruebas HTTP integradas de los tres casos
```

La aplicación utiliza **Modelo-Vista-Controlador** con lógica de negocio separada del acceso a datos. `InventarioService` valida origen/destino y el repositorio registra la entrada y el incremento condicional de existencias en una sola transacción. El esquema exige cantidad positiva y destino exclusivo (materia prima **XOR** producto terminado).

```text
Vista Razor → Controller → Service → Repository → SGCRNDbContext (EF Core) → MySQL
```

Las rutas principales son `/MateriaPrima`, `/Producto`, `/EntradaInventario`, `/SalidaDistribucion` y `/ClientePuntoVenta`. Los movimientos se registran en `/EntradaInventario/Create` y `/SalidaDistribucion/Create`.

**Estado del prototipo:** el desarrollo local aún no implementa inicio de sesión ni control de roles. El diagrama de despliegue Ubuntu/Nginx/TLS representa el objetivo, no una instalación realizada; no exponer esta aplicación sin agregar autorización de administrador.

## Comprobación

```powershell
dotnet build SGCRN.slnx
```

Con la aplicación iniciada y Python 3 instalado, `python scripts/Verificar-SGCRN.py` comprueba los tres casos y deja registros de prueba identificables para inspeccionarlos.

Bootstrap se distribuye bajo licencia MIT; consulta `SGCRN/wwwroot/lib/bootstrap/LICENSE`.
