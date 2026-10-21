USE SGCRN_Avance01;

-- Actualiza instalaciones anteriores del avance conservando los productos.
-- En instalaciones nuevas las columnas ya no existen; se puede repetir.
SET @sql = IF(
    EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ProductosTerminados'
              AND COLUMN_NAME = 'StockActual'),
    'ALTER TABLE ProductosTerminados DROP COLUMN StockActual',
    'DO 0');
PREPARE migracion FROM @sql;
EXECUTE migracion;
DEALLOCATE PREPARE migracion;

SET @sql = IF(
    EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ProductosTerminados'
              AND COLUMN_NAME = 'StockMinimo'),
    'ALTER TABLE ProductosTerminados DROP COLUMN StockMinimo',
    'DO 0');
PREPARE migracion FROM @sql;
EXECUTE migracion;
DEALLOCATE PREPARE migracion;
