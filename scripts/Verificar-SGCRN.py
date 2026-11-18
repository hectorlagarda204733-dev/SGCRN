"""Pruebas HTTP de acceso, CU01, CU02, CU03 básico, CU05 y CU06.

Requiere aplicación y MySQL configurados. Los registros generados se identifican
con el prefijo VERIFICACION_SGCRN_ para facilitar su limpieza manual.
"""
import html
import http.cookiejar
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid
import os
import json
import concurrent.futures
from datetime import datetime
from pathlib import Path

base = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5181").rstrip("/")
browser = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
suffix = uuid.uuid4().hex[:12]
product_name = "VERIFICACION_SGCRN_PRODUCTO_" + suffix
material_name = "VERIFICACION_SGCRN_MATERIA_" + suffix
checks = 0
results = []
manifest = {"prefijo": suffix, "base": base, "inicio": datetime.now().isoformat(), "registros": {}, "pruebas": results}


def save_results():
    if os.environ.get("SGCRN_RESULTADOS"):
        Path(os.environ["SGCRN_RESULTADOS"]).write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")


def request(path, data=None):
    payload = urllib.parse.urlencode(data).encode() if data is not None else None
    try:
        response = browser.open(base + path, data=payload, timeout=20)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        return response.status, html.unescape(response.read().decode("utf-8")), response.url


def check(condition, description):
    global checks
    results.append({"caso": description, "resultado": "APROBADO" if condition else "FALLIDO"})
    save_results()
    if not condition:
        raise AssertionError(description)
    checks += 1
    print("OK - " + description)


def post(path, fields):
    status, page, _ = request(path)
    assert status == 200, (path, status)
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
    assert token, "Falta el token antifalsificación"
    actions = re.findall(r'<form[^>]*action="([^"]+)"', page)
    assert path in actions, "El formulario MVC debe conservar la ruta y el identificador"
    reference = re.search(r'name="ReferenciaRegistro"[^>]*value="([^"]+)"', page)
    if reference and "ReferenciaRegistro" not in fields:
        fields = {**fields, "ReferenciaRegistro": reference.group(1)}
    return request(path, {**fields, "__RequestVerificationToken": token.group(1)})


def product_fields(**changes):
    return {
        "Nombre": product_name, "Descripcion": "Producto de prueba.",
        "UnidadMedida": "Frasco", "PrecioSugerido": "55.25", "ImagenUrl": "",
        **changes,
    }


def material_fields(**changes):
    return {
        "Nombre": material_name, "Descripcion": "Materia prima de prueba.",
        "UnidadMedida": "kg", **changes,
    }


def entry_fields(**changes):
    return {
        "TipoOrigen": "Compra", "MateriaPrimaId": material_id,
        "ProductoTerminadoId": "", "Cantidad": "2.500", "CostoUnitario": "3.25",
        "Observaciones": "Entrada de prueba.", **changes,
    }


def stock_is(page, value):
    number = re.escape(value).replace(",", "[.,]")
    return re.search(r"Existencias actuales.*?</dt>\s*<dd[^>]*>" + number, page, re.S) is not None


for route in ("/", "/Producto/Create", "/MateriaPrima", "/EntradaInventario", "/SalidaDistribucion", "/ClientePuntoVenta", "/uploads/productos/no-existe.png"):
    status, page, url = request(route)
    check(status == 200 and "/Cuenta/Login" in url and "Entrar al sistema" in page, "Acceso anónimo redirigido: " + route)
status, _, _ = request("/Cuenta/Login", {"Usuario": "admin", "Password": "admin"})
check(status == 400, "Login sin token antifalsificación rechazado")
status, page, _ = post("/Cuenta/Login", {"Usuario": "", "Password": ""})
check(status == 200 and "Escribe tu usuario" in page and "Escribe tu contraseña" in page, "Login valida campos obligatorios")
status, page, _ = post("/Cuenta/Login", {"Usuario": "admin", "Password": "incorrecta"})
check(status == 200 and "Usuario o contraseña incorrectos" in page, "Contraseña incorrecta rechazada")
status, page, _ = post("/Cuenta/Login", {"Usuario": "no-existe", "Password": "admin"})
check(status == 200 and "Usuario o contraseña incorrectos" in page, "Usuario inexistente recibe error genérico")
status, page, url = post("/Cuenta/Login", {"Usuario": os.getenv("SGCRN_USUARIO", "admin"), "Password": os.getenv("SGCRN_PASSWORD", "admin"), "ReturnUrl": "https://example.com/"})
check(status == 200 and urllib.parse.urlparse(url).path in ("/", "/Producto") and "Administrador" in page, "Login admin válido y retorno externo ignorado")
status, page, _ = request("/")
check(status == 200 and "Catálogo de productos" in page, "Inicio MVC: catálogo de productos")
status, page, _ = request("/MateriaPrima")
check(status == 200 and "Flor de manzanilla" in page, "CU01: catálogo de materias primas")
status, page, _ = request("/lib/bootstrap/bootstrap.min.css")
check(status == 200 and "Bootstrap" in page, "CSS Bootstrap local")

# CU02: catálogo, reglas, validación y antisuplantación.
status, page, _ = request("/Producto/Create")
check(status == 200 and 'name="StockActual"' not in page, "CU02: registro no pide stock")
status, _, _ = request("/Producto/Create", product_fields())
check(status == 400, "CU02: POST sin token rechazado")
status, page, _ = post("/Producto/Create", product_fields(Nombre=" ", UnidadMedida="", PrecioSugerido="-1"))
check(status == 200 and "El nombre es obligatorio" in page and "La unidad de medida es obligatoria" in page
      and "El precio debe estar" in page, "CU02: campos y precio validados")
status, page, _ = post("/Producto/Create", product_fields(PrecioSugerido="1.234", ImagenUrl="javascript:alert(1)"))
check(status == 200 and "hasta dos decimales" in page and "comience con http" in page,
      "CU02: decimales y URL inválidos")
status, page, _ = post("/Producto/Create", product_fields(Activo="false", StockActual="999"))
check(status == 200 and "Producto registrado correctamente" in page, "CU02: registro exitoso")
query_product = "/Producto?" + urllib.parse.urlencode({"Buscar": product_name, "Activo": "true"})
status, page, _ = request(query_product)
match = re.search(r'href="/Producto/Details/(\d+)"', page)
check(status == 200 and product_name in page and match is not None and 'data-result-count="1"' in page,
      "CU02: búsqueda y filtro de activos")
product_id = match.group(1)
manifest["registros"]["producto"] = {"id": product_id, "nombre": product_name}
save_results()
status, page, _ = request("/Producto/Details/" + product_id)
check(status == 200 and "Activo" in page and stock_is(page, "0,000"),
      "CU02: ignora StockActual y Activo enviados por el navegador")
status, page, _ = post("/Producto/Create", product_fields())
check(status == 200 and "Ya existe un producto" in page, "CU02: nombre duplicado rechazado")
edit_product = "/Producto/Edit/" + product_id
status, page, _ = request(edit_product)
check(status == 200 and 'name="StockActual"' not in page, "CU02: edición no modifica stock")
status, _, _ = request(edit_product, product_fields())
check(status == 400, "CU02: edición sin token rechazada")
status, page, _ = post(edit_product, product_fields(PrecioSugerido="60.50", StockActual="777"))
check(status == 200 and "Producto actualizado correctamente" in page, "CU02: edición del catálogo")

# CU01: registrar, editar, filtrar y consultar un insumo sin capturar stock.
status, page, _ = request("/MateriaPrima/Create")
check(status == 200 and 'name="StockActual"' not in page, "CU01: formulario no pide existencias")
status, _, _ = request("/MateriaPrima/Create", material_fields())
check(status == 400, "CU01: POST sin token rechazado")
status, page, _ = post("/MateriaPrima/Create", material_fields(Nombre="", UnidadMedida=""))
check(status == 200 and "El nombre es obligatorio" in page and "La unidad de medida es obligatoria" in page,
      "CU01: campos obligatorios")
status, page, _ = post("/MateriaPrima/Create", material_fields(StockActual="900", Activo="false"))
check(status == 200 and "Materia prima registrada correctamente" in page, "CU01: registro exitoso")
query_material = "/MateriaPrima?" + urllib.parse.urlencode({"Buscar": material_name, "Activo": "true"})
status, page, _ = request(query_material)
match = re.search(r'href="/MateriaPrima/Details/(\d+)"', page)
check(status == 200 and material_name in page and match is not None and 'data-result-count="1"' in page,
      "CU01: búsqueda y filtro de activos")
material_id = match.group(1)
manifest["registros"]["materia"] = {"id": material_id, "nombre": material_name}
save_results()
status, page, _ = request("/MateriaPrima/Details/" + material_id)
check(status == 200 and "Activa" in page and stock_is(page, "0,000"),
      "CU01: stock inicial cero y estado activo pese a POST manipulado")
status, page, _ = post("/MateriaPrima/Create", material_fields())
check(status == 200 and "Ya existe una materia prima" in page, "CU01: nombre duplicado rechazado")
edit_material = "/MateriaPrima/Edit/" + material_id
status, page, _ = post(edit_material, material_fields(UnidadMedida="g", StockActual="777"))
check(status == 200 and "Materia prima actualizada correctamente" in page, "CU01: edición no modifica existencias")
status, page, _ = request("/MateriaPrima/Details/" + material_id)
check(status == 200 and "g" in page and stock_is(page, "0,000"), "CU01: edición conservó el stock")

# CU05: destino exclusivo, operaciones válidas y registro histórico.
status, page, _ = request("/EntradaInventario/Create")
check(status == 200 and material_name in page and product_name in page,
      "CU05: formulario ofrece materias y productos activos")
status, _, _ = request("/EntradaInventario/Create", entry_fields())
check(status == 400, "CU05: POST sin token rechazado")
status, page, _ = post("/EntradaInventario/Create", entry_fields(Cantidad="0", CostoUnitario="-1"))
check(status == 200 and "cantidad debe ser mayor" in page and "costo no puede" in page,
      "CU05: cantidad positiva y costo no negativo")
status, page, _ = post("/EntradaInventario/Create", entry_fields(Cantidad="1.2345"))
check(status == 200 and "hasta tres decimales" in page, "CU05: máximo tres decimales")
status, page, _ = post("/EntradaInventario/Create", entry_fields(ProductoTerminadoId=product_id))
check(status == 200 and "compra debe indicar solo una materia prima" in page,
      "CU05: prohíbe dos destinos simultáneos")
status, page, _ = post("/EntradaInventario/Create", entry_fields(TipoOrigen="Produccion"))
check(status == 200 and "producción debe indicar solo un producto terminado" in page,
      "CU05: origen y destino deben coincidir")
status, page, _ = post("/EntradaInventario/Create", entry_fields(MateriaPrimaId="9223372036854775807"))
check(status == 200 and "materia prima no existe" in page,
      "CU05: destino inexistente rechazado")
status, page, _ = request("/MateriaPrima/Details/" + material_id)
check(status == 200 and stock_is(page, "0,000"), "CU05: entradas inválidas no aumentan existencias")
status, page, url = post("/EntradaInventario/Create", entry_fields())
check(status == 200 and "Entrada registrada" in page and re.search(r"2[.,]500", page)
      and "/EntradaInventario/Details/" in url, "CU05: entrada de compra con folio")
entry_material_id = url.rsplit("/", 1)[-1]
status, page, _ = request("/MateriaPrima/Details/" + material_id)
check(status == 200 and stock_is(page, "2,500"), "CU05: compra incrementó stock del insumo")
status, page, _ = request("/Producto/Details/" + product_id)
check(status == 200 and stock_is(page, "0,000"), "CU05: compra no afectó al producto")
status, page, url = post("/EntradaInventario/Create", entry_fields(
    TipoOrigen="Produccion", MateriaPrimaId="", ProductoTerminadoId=product_id,
    Cantidad="3.125", CostoUnitario="0"))
check(status == 200 and "Entrada registrada" in page and re.search(r"3[.,]125", page),
      "CU05: entrada de producción con folio")
entry_product_id = url.rsplit("/", 1)[-1]
status, page, _ = request("/Producto/Details/" + product_id)
check(status == 200 and stock_is(page, "3,125"), "CU05: producción incrementó stock del producto")
status, page, _ = request("/EntradaInventario")
check(status == 200 and material_name in page and product_name in page,
      "CU05: historial incluye ambas entradas")
status, page, _ = post(edit_product, product_fields(PrecioSugerido="60.50", StockActual="999"))
status, page, _ = request("/Producto/Details/" + product_id)
check(status == 200 and stock_is(page, "3,125"), "CU02: editar el catálogo preserva stock de CU05")

# CU03 básico y CU06: destinatarios, atomicidad observable y concurrencia.
client_name = "VERIFICACION_SGCRN_CLIENTE_" + suffix
status, page, url = post("/ClientePuntoVenta/Create", {"NombreTienda": client_name, "Contacto": "Contacto de prueba", "Direccion": "Dirección de prueba"})
client_id = urllib.parse.parse_qs(urllib.parse.urlparse(url).query)["clienteId"][0]
manifest["registros"]["cliente"] = {"id": client_id, "nombre": client_name}
save_results()
check(status == 200 and "/SalidaDistribucion/Create" in url and f'value="{client_id}" selected="selected"' in page, "CU03: alta y preselección del destinatario")
status, page, _ = post("/ClientePuntoVenta/Create", {"NombreTienda": client_name})
check("Ya existe un cliente" in page, "CU03: nombre duplicado rechazado")
status, page, _ = post("/ClientePuntoVenta/Create", {"NombreTienda": " "})
check("obligatorio" in page, "CU03: nombre obligatorio")
status, page, _ = request("/ClientePuntoVenta?buscar=" + client_name)
check(status == 200 and client_name in page, "CU03: búsqueda del destinatario")
status, page, _ = request("/ClientePuntoVenta/Details/" + client_id)
check(status == 200 and "Contacto de prueba" in page, "CU03: consulta de contacto y dirección")
outgoing = {"TipoSalida": "VentaDirecta", "ProductoTerminadoId": product_id, "ClientePuntoVentaId": client_id, "Cantidad": "1", "Observaciones": "Entrega de prueba"}
status, _, _ = request("/SalidaDistribucion/Create", outgoing)
check(status == 400, "CU06: POST sin token rechazado")
for fields, label in [
    ({"Cantidad": "0"}, "cantidad cero"), ({"Cantidad": "-1"}, "cantidad negativa"),
    ({"Cantidad": "1.0001"}, "más de tres decimales"), ({"Cantidad": "4"}, "existencias insuficientes"),
    ({"ClientePuntoVentaId": "9223372036854775807"}, "destinatario inexistente"),
    ({"ProductoTerminadoId": "9223372036854775807"}, "producto inexistente"),
    ({"TipoSalida": "999"}, "motivo inexistente"), ({"ReferenciaRegistro": "no-guid"}, "referencia inválida"),
    ({"ClientePuntoVentaId": ""}, "destinatario obligatorio")
]:
    status, page, url = post("/SalidaDistribucion/Create", {**outgoing, **fields})
    check(status == 200 and "/Details/" not in url and "validation-summary-errors" in page, "CU06: rechaza " + label)
status, page, _ = request("/Producto/Details/" + product_id)
check(stock_is(page, "3,125"), "CU06: rechazos conservan existencias")
reference = str(uuid.uuid4())
status, page, url = post("/SalidaDistribucion/Create", {**outgoing, "ReferenciaRegistro": reference})
outgoing_id = url.rsplit("/", 1)[-1]
check("/SalidaDistribucion/Details/" in url and client_name in page, "CU06: venta directa con folio y destinatario")
status, page, _ = request("/Producto/Details/" + product_id)
check(stock_is(page, "2,125"), "CU06: venta descuenta exactamente una unidad")
status, page, url = post("/SalidaDistribucion/Create", {**outgoing, "ReferenciaRegistro": reference})
check(url.endswith("/" + outgoing_id), "CU06: reenvío devuelve el mismo folio")
status, page, _ = post("/SalidaDistribucion/Create", {**outgoing, "ReferenciaRegistro": reference, "Cantidad": "0.5"})
check("ya registró una salida distinta" in page, "CU06: referencia no admite otra entrega")

def prepare_outgoing(amount, reference=None):
    session = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
    def send(path, fields=None):
        payload = urllib.parse.urlencode(fields).encode() if fields is not None else None
        with session.open(base + path, payload, timeout=30) as response:
            return html.unescape(response.read().decode()), response.url
    page, _ = send("/Cuenta/Login")
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
    send("/Cuenta/Login", {"Usuario": os.getenv("SGCRN_USUARIO", "admin"), "Password": os.getenv("SGCRN_PASSWORD", "admin"), "__RequestVerificationToken": token})
    page, _ = send("/SalidaDistribucion/Create")
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
    ref = reference or re.search(r'name="ReferenciaRegistro"[^>]*value="([^"]+)"', page).group(1)
    return lambda: send("/SalidaDistribucion/Create", {**outgoing, "TipoSalida": "Distribucion", "Cantidad": amount, "ReferenciaRegistro": ref, "__RequestVerificationToken": token})

jobs = [prepare_outgoing("2"), prepare_outgoing("2")]
with concurrent.futures.ThreadPoolExecutor(2) as pool:
    answers = list(pool.map(lambda job: job(), jobs))
check(sum("/Details/" in url for _, url in answers) == 1, "CU06: salidas simultáneas solo confirman la que cabe")
status, page, _ = request("/Producto/Details/" + product_id)
check(stock_is(page, "0,125"), "CU06: concurrencia preserva saldo exacto")
reference = str(uuid.uuid4())
jobs = [prepare_outgoing("0.125", reference), prepare_outgoing("0.125", reference)]
with concurrent.futures.ThreadPoolExecutor(2) as pool:
    answers = list(pool.map(lambda job: job(), jobs))
check(all("/Details/" in url for _, url in answers) and answers[0][1] == answers[1][1], "CU06: duplicados simultáneos devuelven un solo folio")
status, page, _ = request("/Producto/Details/" + product_id)
check(stock_is(page, "0,000"), "CU06: entrega exacta agota stock sin negativos")
status, page, _ = request("/SalidaDistribucion")
check(product_name in page and client_name in page, "CU06: historial muestra producto y destinatario")

delete_product = "/Producto/Delete/" + product_id
status, _, _ = request(delete_product, {})
check(status == 400, "CU02: baja sin token rechazada")
status, page, _ = post(delete_product, {})
check(status == 200 and "Producto dado de baja" in page, "CU02: baja lógica de producto")
status, page, _ = request(query_product)
check(status == 200 and "No se encontraron productos" in page, "CU02: inactivo excluido del filtro activo")
status, page, _ = post("/EntradaInventario/Create", entry_fields(
    TipoOrigen="Produccion", MateriaPrimaId="", ProductoTerminadoId=product_id))
check(status == 200 and "producto no existe o está inactivo" in page,
      "CU05: no ingresa stock a producto inactivo")
status, page, _ = post("/SalidaDistribucion/Create", outgoing)
check("Selecciona un producto activo" in page, "CU06: no entrega producto inactivo")
status, page, _ = request("/SalidaDistribucion/Details/" + outgoing_id)
check(status == 200 and product_name in page, "CU06: baja conserva historial")
status, page, _ = request("/EntradaInventario/Details/" + entry_product_id)
check(status == 200 and product_name in page, "CU05: baja no destruye entradas previas")
delete_material = "/MateriaPrima/Delete/" + material_id
status, page, _ = post(delete_material, {})
check(status == 200 and "Materia prima dada de baja" in page, "CU01: baja lógica de insumo")
status, page, _ = post("/EntradaInventario/Create", entry_fields())
check(status == 200 and "materia prima no existe o está inactiva" in page,
      "CU05: no ingresa stock a insumo inactivo")
status, page, _ = request("/EntradaInventario/Details/" + entry_material_id)
check(status == 200 and material_name in page, "CU05: historial conservado después de baja")
for route in ("/MateriaPrima/Details/9223372036854775807",
              "/Producto/Details/9223372036854775807",
              "/EntradaInventario/Details/9223372036854775807",
              "/SalidaDistribucion/Details/9223372036854775807",
              "/ClientePuntoVenta/Details/9223372036854775807"):
    status, _, _ = request(route)
    check(status == 404, "Identificador inexistente devuelve 404: " + route)

status, _, _ = request("/Cuenta/Logout", {})
check(status == 400, "Logout sin token rechazado")
status, page, _ = request("/")
token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page).group(1)
status, page, url = request("/Cuenta/Logout", {"__RequestVerificationToken": token})
check("/Cuenta/Login" in url and "Entrar al sistema" in page, "Logout elimina sesión")
status, page, url = request("/SalidaDistribucion")
check("/Cuenta/Login" in url, "Módulos vuelven a exigir sesión después de salir")
status, page, url = post("/Cuenta/Login", {"Usuario": os.getenv("SGCRN_USUARIO", "admin"), "Password": os.getenv("SGCRN_PASSWORD", "admin"), "ReturnUrl": "/EntradaInventario"})
check(urllib.parse.urlparse(url).path == "/EntradaInventario", "Login recupera destino local solicitado")
manifest["fin"] = datetime.now().isoformat()
save_results()
print(f"\n{checks} verificaciones aprobadas. Registros: {product_name} (Id {product_id}), "
      f"{material_name} (Id {material_id}); entradas {entry_material_id}, {entry_product_id}.")
