-- Esquema independiente para CU01, CU02 y CU05. No altera bases anteriores.
CREATE DATABASE IF NOT EXISTS SGCRN_Proyecto
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE SGCRN_Proyecto;

CREATE TABLE IF NOT EXISTS ProductosTerminados (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    ImagenUrl VARCHAR(500) NULL,
    UnidadMedida VARCHAR(30) NOT NULL,
    PrecioSugerido DECIMAL(18,2) NOT NULL DEFAULT 0,
    StockActual DECIMAL(18,3) NOT NULL DEFAULT 0,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreadoEn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ActualizadoEn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_ProductosTerminados_Nombre (Nombre),
    CONSTRAINT chk_ProductoPrecio CHECK (PrecioSugerido >= 0),
    CONSTRAINT chk_ProductoStock CHECK (StockActual >= 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS MateriasPrimas (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    UnidadMedida VARCHAR(30) NOT NULL,
    StockActual DECIMAL(18,3) NOT NULL DEFAULT 0,
    StockMinimo DECIMAL(18,3) NOT NULL DEFAULT 0,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreadoEn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ActualizadoEn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_MateriasPrimas_Nombre (Nombre),
    CONSTRAINT chk_MateriaStock CHECK (StockActual >= 0),
    CONSTRAINT chk_MateriaMinimo CHECK (StockMinimo >= 0)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS EntradasInventario (
    Id BIGINT AUTO_INCREMENT PRIMARY KEY,
    Fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TipoOrigen VARCHAR(20) NOT NULL,
    MateriaPrimaId BIGINT NULL,
    ProductoTerminadoId BIGINT NULL,
    Cantidad DECIMAL(18,3) NOT NULL,
    CostoUnitario DECIMAL(18,2) NOT NULL DEFAULT 0,
    Observaciones VARCHAR(500) NULL,
    CONSTRAINT fk_EntradaMateria FOREIGN KEY (MateriaPrimaId) REFERENCES MateriasPrimas(Id),
    CONSTRAINT fk_EntradaProducto FOREIGN KEY (ProductoTerminadoId) REFERENCES ProductosTerminados(Id),
    CONSTRAINT chk_EntradaCantidad CHECK (Cantidad > 0),
    CONSTRAINT chk_EntradaCosto CHECK (CostoUnitario >= 0),
    CONSTRAINT chk_EntradaDestino CHECK (
        (TipoOrigen = 'Compra' AND MateriaPrimaId IS NOT NULL AND ProductoTerminadoId IS NULL)
        OR (TipoOrigen = 'Produccion' AND ProductoTerminadoId IS NOT NULL AND MateriaPrimaId IS NULL)
    )
) ENGINE=InnoDB;
