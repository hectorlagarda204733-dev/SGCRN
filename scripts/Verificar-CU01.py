"""Prueba HTTP del CU01 con Python 3 (sin paquetes externos).

Requiere la aplicación iniciada. Crea un producto identificable y lo deja inactivo
para comprobar la baja lógica. No modifica los productos de ejemplo.
"""
import html
import http.cookiejar
import re
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid

base = (sys.argv[1] if len(sys.argv) > 1 else "http://localhost:5181").rstrip("/")
browser = urllib.request.build_opener(
    urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar())
)
name = "VERIFICACION_CU01_" + uuid.uuid4().hex[:12]
checks = 0


def request(path, data=None):
    payload = urllib.parse.urlencode(data).encode() if data is not None else None
    try:
        response = browser.open(base + path, data=payload, timeout=15)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        return response.status, html.unescape(response.read().decode("utf-8")), response.url


def check(condition, description):
    global checks
    if not condition:
        raise AssertionError(description)
    checks += 1
    print("OK - " + description)


def post(path, fields):
    status, page, _ = request(path)
    assert status == 200, (path, status)
    token = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', page)
    assert token, "No se encontró el token antifalsificación"
    return request(path, {**fields, "__RequestVerificationToken": token.group(1)})


def product_fields(**changes):
    return {
        "Input.Nombre": name,
        "Input.Descripcion": "Producto ficticio para verificar registro y persistencia.",
        "Input.UnidadMedida": "Frasco",
        "Input.PrecioSugerido": "55.25",
        "Input.ImagenUrl": "",
        **changes,
    }


status, page, _ = request("/")
check(status == 200 and "Catálogo de productos" in page, "Inicio y consulta general")
status, page, _ = request("/lib/bootstrap/bootstrap.min.css")
check(status == 200 and "Bootstrap" in page, "Estilos disponibles localmente")
status, _, _ = request("/ProductosTerminados/Create", product_fields())
check(status == 400, "Formulario sin token antifalsificación rechazado")

status, page, _ = post("/ProductosTerminados/Create", product_fields(**{
    "Input.Nombre": " ", "Input.UnidadMedida": "", "Input.PrecioSugerido": "-1",
}))
check(status == 200 and "El nombre es obligatorio" in page
      and "La unidad de medida es obligatoria" in page and "El precio debe estar" in page,
      "Nombre, unidad y precio negativo validados en servidor")
status, page, _ = post("/ProductosTerminados/Create", product_fields(**{
    "Input.PrecioSugerido": "1.234", "Input.ImagenUrl": "javascript:alert(1)",
}))
check(status == 200 and "hasta dos decimales" in page and "comience con http" in page,
      "Decimales e imagen inválida rechazados")
status, page, final_url = post("/ProductosTerminados/Create", product_fields())
check(status == 200 and "Producto registrado correctamente" in page
      and not final_url.endswith("Create"), "Registro exitoso y redirección al catálogo")

query = "/ProductosTerminados?" + urllib.parse.urlencode({"Buscar": name, "Activo": "true"})
status, page, _ = request(query)
match = re.search(r'href="/ProductosTerminados/Details/(\d+)"', page)
check(status == 200 and name in page and match is not None and "1 resultado(s)" in page,
      "Búsqueda por nombre y filtro de activos")
product_id = match.group(1)
status, page, _ = request("/ProductosTerminados/Details/" + product_id)
check(status == 200 and name in page and "55.25" in page, "Consulta individual conserva datos registrados")
status, page, _ = post("/ProductosTerminados/Create", product_fields())
check(status == 200 and "Ya existe un producto con ese nombre" in page, "Nombre duplicado rechazado")

edit_path = "/ProductosTerminados/Edit/" + product_id
status, page, _ = post(edit_path, product_fields(**{"Input.PrecioSugerido": "60.50"}))
check(status == 200 and "Producto actualizado correctamente" in page, "Edición de precio")
status, page, _ = request("/ProductosTerminados/Details/" + product_id)
check(status == 200 and "60.50" in page, "Precio actualizado persiste al consultar")
status, page, _ = post(edit_path, product_fields(**{"Input.PrecioSugerido": "60.50"}))
check(status == 200 and "Producto actualizado correctamente" in page, "Edición sin cambios también es válida")

delete_path = "/ProductosTerminados/Delete/" + product_id
status, page, _ = request(delete_path)
check(status == 200 and "Confirmar baja" in page and "Cancelar" in page, "Pantalla de confirmación de baja")
status, page, _ = request(query)
check(status == 200 and name in page, "Consultar confirmación sin aceptarla conserva el estado activo")
status, page, _ = post(delete_path, {})
check(status == 200 and "Producto dado de baja" in page, "Baja lógica confirmada")
status, page, _ = request(query)
check(status == 200 and "No se encontraron productos" in page, "Producto dado de baja excluido de activos")
status, page, _ = request(query.replace("Activo=true", "Activo=false"))
check(status == 200 and name in page and "Inactivo" in page, "Producto conservado en filtro de inactivos")
status, page, _ = post("/ProductosTerminados/Create", product_fields())
check(status == 200 and "incluso si está inactivo" in page, "Nombre inactivo también impide duplicados")
status, _, _ = request("/ProductosTerminados/Details/9223372036854775807")
check(status == 404, "Producto inexistente devuelve 404")
print(f"\n{checks} comprobaciones aprobadas. Producto de prueba: {name} (Id={product_id}).")
