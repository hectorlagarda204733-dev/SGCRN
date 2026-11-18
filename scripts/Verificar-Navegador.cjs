// Requiere Playwright y Microsoft Edge. Evidencias locales, nunca credenciales/cookies.
const { chromium } = require('playwright');
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const base = process.env.SGCRN_URL || 'http://localhost:5181';
const out = path.resolve(process.env.SGCRN_EVIDENCIAS || 'artifacts/navegador');
fs.mkdirSync(path.join(out, 'capturas'), { recursive: true });
const results = { inicio: new Date().toISOString(), base, registros: {}, imagenes: [], pruebas: [], capturas: [] };
const persist = () => fs.writeFileSync(path.join(out, 'resultados-navegador.json'), JSON.stringify(results, null, 2));
const check = (condition, caso) => {
    results.pruebas.push({ caso, resultado: condition ? 'APROBADO' : 'FALLIDO' }); persist();
    assert.ok(condition, caso); console.log('OK - ' + caso);
};
const suffix = Date.now().toString(36);
const productName = 'Bálsamo de eucalipto · Prueba ' + suffix;
const materialName = 'Hojas de eucalipto · Prueba ' + suffix;
const clientName = 'Abarrotes La Esperanza · Prueba ' + suffix;

(async () => {
    const browser = await chromium.launch({ channel: 'msedge', headless: true });
    try {
        const context = await browser.newContext({ viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce' });
        const page = await context.newPage();
        const errors = [];
        page.on('pageerror', error => errors.push(error.message));
        const go = async route => {
            const response = await page.goto(base + route, { waitUntil: 'networkidle' });
            assert.equal(response.status(), 200, route);
        };
        const capture = async (name, caption) => {
            await page.screenshot({ path: path.join(out, 'capturas', name + '.png'), fullPage: true, animations: 'disabled' });
            results.capturas.push({ archivo: 'capturas/' + name + '.png', descripcion: caption }); persist();
        };
        const submit = async name => {
            await Promise.all([page.waitForNavigation({ waitUntil: 'networkidle' }), page.getByRole('button', { name, exact: true }).click()]);
        };
        await go('/SalidaDistribucion');
        check(page.url().includes('/Cuenta/Login'), 'Navegador: ruta privada solicita login');
        await capture('01-login', 'Acceso de administrador; ruta protegida.');
        await page.locator('#Usuario').fill('admin');
        await page.keyboard.press('Tab');
        check(await page.locator('#Password').evaluate(el => el === document.activeElement), 'Login utilizable con teclado');
        await page.locator('#Password').fill('incorrecta');
        await submit('Entrar al sistema');
        check(await page.getByText('Usuario o contraseña incorrectos.').isVisible(), 'Login muestra error de credenciales');
        check(await page.locator('#Password').inputValue() === '', 'Contraseña no se vuelve a mostrar tras error');
        await capture('02-login-error', 'Credenciales incorrectas rechazadas.');
        await page.locator('#Password').fill(process.env.SGCRN_PASSWORD || 'admin');
        await submit('Entrar al sistema');
        check(new URL(page.url()).pathname === '/SalidaDistribucion', 'Login recupera la página solicitada');
        const cookie = (await context.cookies()).find(c => c.name === 'SGCRN.Sesion');
        check(cookie?.httpOnly && cookie.sameSite === 'Lax' && cookie.expires === -1, 'Cookie HttpOnly, SameSite Lax y sesión no persistente');
        const response = await context.request.get(base + '/Producto');
        check(response.headers()['cache-control']?.includes('no-store'), 'Páginas privadas impiden almacenamiento en caché');
        await go('/Producto');
        await capture('03-catalogo-productos', 'Catálogo de productos con imágenes y sesión de administrador.');
        await go('/MateriaPrima');
        await capture('04-catalogo-materias', 'Catálogo de materias primas.');

        await go('/Producto/Create');
        await page.locator('#Nombre').fill(productName);
        await page.locator('#UnidadMedida').fill('frasco');
        await page.locator('#PrecioSugerido').fill('85.50');
        await page.locator('#Descripcion').fill('Producto temporal para comprobar el recorrido completo del sistema.');
        const imagePath = process.env.SGCRN_IMAGEN_PRUEBA;
        assert.ok(imagePath, 'Define SGCRN_IMAGEN_PRUEBA con una imagen JPG, PNG o WebP.');
        const image = fs.readFileSync(imagePath);
        await page.locator('#ImagenArchivo').setInputFiles({ name: 'producto-prueba.png', mimeType: 'image/png', buffer: image });
        check(await page.locator('[data-image-preview]').isVisible(), 'Imagen: selección de archivo y vista previa');
        await capture('05-producto-imagen', 'Alta de producto y vista previa de archivo.');
        await submit('Crear producto');
        await go('/Producto?buscar=' + encodeURIComponent(productName));
        const productHref = await page.locator('a.record-name').getAttribute('href');
        const pid = productHref.split('/').pop();
        results.registros.producto = { id: pid, nombre: productName }; persist();
        check(await page.locator('[data-product-image]').first().evaluate(img => img.complete && img.naturalWidth > 0), 'Imagen guardada visible en miniatura');
        await page.locator('a.record-name').click();
        await page.waitForLoadState('networkidle');
        const firstImage = await page.locator('[data-product-image]').first().getAttribute('src');
        results.imagenes.push(firstImage); persist();
        check(firstImage.startsWith('/uploads/productos/'), 'Imagen normalizada guardada en uploads');
        check((await page.locator('[data-return-link]').getAttribute('href')).includes('buscar='), 'Regreso conserva filtro de catálogo');
        await capture('06-producto-detalle', 'Producto registrado con imagen y existencias cero.');

        await go('/Producto/Edit/' + pid);
        for (const type of ['drop', 'paste']) {
            await page.locator('[data-image-drop]').evaluate((element, { data, type }) => {
                const bytes = Uint8Array.from(atob(data), c => c.charCodeAt(0));
                const transfer = new DataTransfer();
                transfer.items.add(new File([bytes], type + '.png', { type: 'image/png' }));
                element.dispatchEvent(type === 'drop'
                    ? new DragEvent('drop', { bubbles: true, cancelable: true, dataTransfer: transfer })
                    : new ClipboardEvent('paste', { bubbles: true, cancelable: true, clipboardData: transfer }));
            }, { data: image.toString('base64'), type });
            check(await page.locator('#ImagenArchivo').evaluate((input, type) => input.files[0]?.name === type + '.png', type), 'Imagen: evento de ' + type + ' selecciona archivo');
        }
        await submit('Guardar cambios');
        await go(productHref);
        const secondImage = await page.locator('[data-product-image]').first().getAttribute('src');
        results.imagenes.push(secondImage); persist();
        check(firstImage !== secondImage, 'Imagen: reemplazo persistido');
        await go('/Producto/Edit/' + pid);
        await page.locator('#QuitarImagen').check();
        await submit('Guardar cambios');
        await go(productHref);
        check(await page.locator('[data-product-image]').count() === 0, 'Imagen: quitar referencia del producto');

        // La API usa la misma sesión para omitir validación JS y probar el servidor.
        await go('/Producto/Edit/' + pid);
        const token = await page.locator('form.form-sheet input[name="__RequestVerificationToken"]').inputValue();
        for (const [buffer, label, expected] of [
            [Buffer.from('<svg onload="alert(1)"></svg>'), 'archivo disfrazado de PNG', 'no es una imagen'],
            [Buffer.alloc(2097153), 'archivo superior a 2 MB', '2 MB']
        ]) {
            const invalid = await context.request.post(base + '/Producto/Edit/' + pid, { multipart: {
                __RequestVerificationToken: token, Nombre: productName, UnidadMedida: 'frasco', PrecioSugerido: '85.50',
                ImagenArchivo: { name: 'invalida.png', mimeType: 'image/png', buffer }
            } });
            const html = await invalid.text();
            check(invalid.status() === 200 && html.includes(expected), 'Servidor rechaza ' + label);
        }
        // Restaurar foto para las siguientes evidencias.
        await page.locator('#ImagenArchivo').setInputFiles({ name: 'producto-prueba.png', mimeType: 'image/png', buffer: image });
        await submit('Guardar cambios');
        await go(productHref);
        results.imagenes.push(await page.locator('[data-product-image]').first().getAttribute('src')); persist();

        await go('/MateriaPrima/Create');
        await page.locator('#Nombre').fill(materialName);
        await page.locator('#UnidadMedida').selectOption('kg');
        check(await page.locator('#UnidadMedida optgroup').count() > 1, 'Materias: selector de unidades agrupadas');
        await capture('07-materia-formulario', 'Registro de materia prima con unidad de medida del catálogo.');
        await submit('Crear materia prima');
        await go('/MateriaPrima?buscar=' + encodeURIComponent(materialName));
        const materialHref = await page.locator('a.record-name').getAttribute('href');
        const mid = materialHref.split('/').pop();
        results.registros.materia = { id: mid, nombre: materialName }; persist();
        await go('/EntradaInventario/Create?materiaId=' + mid);
        check(await page.locator('#MateriaPrimaId').inputValue() === mid && await page.locator('#origen-compra').isChecked(), 'Compra preselecciona materia y origen');
        await page.locator('#Cantidad').fill('5');
        await page.locator('#CostoUnitario').fill('20');
        await capture('08-entrada-compra', 'Compra de 5 kg con resumen previo.');
        await submit('Registrar entrada');
        const entryMaterial = new URL(page.url()).pathname;
        check(entryMaterial.includes('/Details/'), 'Compra registrada desde navegador');
        await capture('09-compra-confirmada', 'Folio de compra registrado.');
        await go('/EntradaInventario/Create?productoId=' + pid);
        check(await page.locator('#ProductoTerminadoId').inputValue() === pid && await page.locator('#origen-produccion').isChecked(), 'Producción preselecciona producto y origen');
        await page.locator('#Cantidad').fill('10');
        await page.locator('#CostoUnitario').fill('30');
        await submit('Registrar entrada');
        const entryProduct = new URL(page.url()).pathname;
        await capture('10-produccion-confirmada', 'Producción de 10 frascos registrada.');
        await go('/SalidaDistribucion/Create?productoId=' + pid);
        await page.locator('[data-new-recipient]').click();
        await page.waitForLoadState('networkidle');
        check(page.url().includes('productoId=' + pid), 'Nuevo destinatario conserva contexto del producto');
        await page.locator('#NombreTienda').fill(clientName);
        await page.locator('#Contacto').fill('Contacto de demostración');
        await page.locator('#Direccion').fill('Domicilio de prueba');
        await capture('11-cliente-formulario', 'Registro básico de cliente o punto de venta.');
        await submit('Guardar y registrar salida');
        const cid = await page.locator('#ClientePuntoVentaId').inputValue();
        results.registros.cliente = { id: cid, nombre: clientName }; persist();
        check(cid !== '' && await page.locator('#ProductoTerminadoId').inputValue() === pid, 'Regreso a salida con producto y nuevo destinatario');
        await page.locator('#tipo-distribucion').check();
        await page.locator('#Cantidad').fill('11');
        await submit('Confirmar y registrar salida');
        check(await page.locator('[data-form-errors]').innerText().then(t => t.includes('No hay existencias suficientes')), 'Navegador muestra rechazo por existencias insuficientes');
        await capture('12-salida-rechazada', 'Intento de entregar 11 frascos con solo 10 disponibles.');
        await page.locator('#Cantidad').fill('3');
        await capture('13-salida-resumen', 'Resumen de distribución: 3 entregados, 7 restantes.');
        await submit('Confirmar y registrar salida');
        const outgoing = new URL(page.url()).pathname;
        check(outgoing.includes('/Details/'), 'Distribución registrada desde navegador');
        await capture('14-salida-confirmada', 'Folio de distribución y destinatario.');
        await go(productHref);
        check(await page.locator('body').innerText().then(t => /7[.,]000/.test(t)), 'Ficha muestra 7 frascos después de la entrega');
        await capture('15-stock-actualizado', 'Existencias actualizadas de 10 a 7 frascos.');
        await go('/EntradaInventario');
        await capture('16-historial-entradas', 'Historial de compras y producción.');
        await go('/SalidaDistribucion');
        await capture('17-historial-salidas', 'Historial de ventas y distribución.');
        await go('/ClientePuntoVenta/Details/' + cid);
        await capture('18-cliente-detalle', 'Datos de contacto del destinatario.');

        const routes = ['/Producto', '/Producto/Create', productHref, '/Producto/Edit/' + pid, '/Producto/Delete/' + pid,
            '/MateriaPrima', '/MateriaPrima/Create', materialHref, '/MateriaPrima/Edit/' + mid, '/MateriaPrima/Delete/' + mid,
            '/EntradaInventario', '/EntradaInventario/Create', entryMaterial, entryProduct,
            '/SalidaDistribucion', '/SalidaDistribucion/Create', outgoing,
            '/ClientePuntoVenta', '/ClientePuntoVenta/Create', '/ClientePuntoVenta/Details/' + cid];
        for (const width of [1440, 900, 390, 320]) {
            await page.setViewportSize({ width, height: 900 });
            for (const route of routes) {
                await go(route);
                const fits = await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth);
                check(fits && await page.locator('h1').count() === 1, `Vista ${width}px sin desbordamiento: ${route}`);
            }
            if (width === 390) {
                await go('/Producto');
                await capture('19-productos-movil', 'Catálogo y navegación en ancho móvil de 390 px.');
                await go('/SalidaDistribucion/Create?productoId=' + pid + '&clienteId=' + cid);
                await capture('20-salida-movil', 'Formulario de salida en móvil.');
            }
        }
        check(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches), 'Interfaz revisada con movimiento reducido');
        await page.setViewportSize({ width: 1440, height: 1000 });
        await go('/');
        await submit('Salir');
        check(page.url().includes('/Cuenta/Login'), 'Botón Salir cierra sesión en navegador');
        await go(productHref);
        check(page.url().includes('/Cuenta/Login'), 'Tras salir, el producto exige iniciar sesión');
        for (const width of [1440, 900, 390, 320]) {
            await page.setViewportSize({ width, height: 900 });
            check(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), 'Login adaptable a ' + width + 'px');
            if (width === 390) await capture('21-login-movil', 'Inicio de sesión en móvil.');
        }
        const privateImage = await context.request.get(base + results.imagenes[0], { maxRedirects: 0 });
        check(privateImage.status() === 302, 'Imagen cargada requiere sesión después de salir');
        check(errors.length === 0, 'Recorrido completo sin errores JavaScript');
        results.fin = new Date().toISOString(); persist();
        console.log(`${results.pruebas.length} verificaciones; ${results.capturas.length} capturas.`);
    } finally { await browser.close(); }
})().catch(error => { console.error(error); process.exitCode = 1; });
