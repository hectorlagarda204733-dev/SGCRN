USE SGCRN_Proyecto;

CREATE TABLE IF NOT EXISTS ClientesPuntosVenta (
    Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NombreTienda VARCHAR(150) NOT NULL,
    Contacto VARCHAR(150) NULL,
    Direccion VARCHAR(300) NULL,
    CreadoEn DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT UK_ClientesPuntosVenta_Nombre UNIQUE (NombreTienda)
) ENGINE=InnoDB;

CREATE TABLE IF NOT EXISTS SalidasDistribucion (
    Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Fecha DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    TipoSalida VARCHAR(20) NOT NULL,
    ProductoTerminadoId BIGINT NOT NULL,
    ClientePuntoVentaId BIGINT NOT NULL,
    Cantidad DECIMAL(18,3) NOT NULL,
    UnidadMedida VARCHAR(30) NOT NULL,
    Observaciones VARCHAR(500) NULL,
    ReferenciaRegistro VARCHAR(36) NOT NULL,
    CONSTRAINT UK_Salidas_Referencia UNIQUE (ReferenciaRegistro),
    CONSTRAINT FK_Salidas_Producto FOREIGN KEY (ProductoTerminadoId) REFERENCES ProductosTerminados(Id),
    CONSTRAINT FK_Salidas_Cliente FOREIGN KEY (ClientePuntoVentaId) REFERENCES ClientesPuntosVenta(Id),
    CONSTRAINT CK_Salidas_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Salidas_Tipo CHECK (TipoSalida IN ('VentaDirecta', 'Distribucion'))
) ENGINE=InnoDB;
