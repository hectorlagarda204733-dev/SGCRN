-- Entrega parcial CU01: una base independiente y una sola tabla.
CREATE DATABASE IF NOT EXISTS SGCRN_Avance01
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE SGCRN_Avance01;

CREATE TABLE IF NOT EXISTS ProductosTerminados (
    Id BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(150) NOT NULL,
    Descripcion VARCHAR(500) NULL,
    ImagenUrl VARCHAR(500) NULL,
    UnidadMedida VARCHAR(30) NOT NULL,
    PrecioSugerido DECIMAL(18,2) NOT NULL DEFAULT 0,
    StockActual DECIMAL(18,3) NOT NULL DEFAULT 0,
    StockMinimo DECIMAL(18,3) NOT NULL DEFAULT 0,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CreadoEn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ActualizadoEn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_ProductosTerminados_Nombre (Nombre),
    CONSTRAINT chk_PrecioNoNegativo CHECK (PrecioSugerido >= 0)
) ENGINE=InnoDB;
