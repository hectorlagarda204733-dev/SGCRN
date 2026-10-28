USE SGCRN_Proyecto;

-- Datos ficticios; ejecutar de nuevo no reemplaza productos existentes.
INSERT INTO ProductosTerminados (Nombre, Descripcion, UnidadMedida, PrecioSugerido)
SELECT 'Infusión de manzanilla', 'Mezcla de flores secas para preparar infusión.', 'Caja de 20 sobres', 65.00
WHERE NOT EXISTS (SELECT 1 FROM ProductosTerminados WHERE Nombre = 'Infusión de manzanilla');

INSERT INTO ProductosTerminados (Nombre, Descripcion, UnidadMedida, PrecioSugerido)
SELECT 'Miel natural', 'Miel en presentación de frasco de 250 gramos.', 'Frasco de 250 g', 120.00
WHERE NOT EXISTS (SELECT 1 FROM ProductosTerminados WHERE Nombre = 'Miel natural');

INSERT INTO ProductosTerminados (Nombre, Descripcion, UnidadMedida, PrecioSugerido)
SELECT 'Infusión de hierbabuena', 'Hojas secas de hierbabuena para infusión.', 'Bolsa de 50 g', 45.00
WHERE NOT EXISTS (SELECT 1 FROM ProductosTerminados WHERE Nombre = 'Infusión de hierbabuena');

INSERT INTO MateriasPrimas (Nombre, Descripcion, UnidadMedida)
SELECT 'Flor de manzanilla', 'Insumo para infusiones.', 'kg'
WHERE NOT EXISTS (SELECT 1 FROM MateriasPrimas WHERE Nombre = 'Flor de manzanilla');

INSERT INTO MateriasPrimas (Nombre, Descripcion, UnidadMedida)
SELECT 'Botella de vidrio 250 ml', 'Envase para productos naturales.', 'pieza'
WHERE NOT EXISTS (SELECT 1 FROM MateriasPrimas WHERE Nombre = 'Botella de vidrio 250 ml');

INSERT INTO MateriasPrimas (Nombre, Descripcion, UnidadMedida)
SELECT 'Etiquetas autoadheribles', 'Etiquetas para identificar los productos.', 'pieza'
WHERE NOT EXISTS (SELECT 1 FROM MateriasPrimas WHERE Nombre = 'Etiquetas autoadheribles');
