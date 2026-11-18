USE SGCRN_Proyecto;

CREATE TABLE IF NOT EXISTS Usuarios (
    Id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    NombreUsuario VARCHAR(50) NOT NULL,
    PasswordHash VARCHAR(500) NOT NULL,
    Rol VARCHAR(30) NOT NULL DEFAULT 'Administrador',
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT UK_Usuarios_Nombre UNIQUE (NombreUsuario)
) ENGINE=InnoDB;

-- La aplicación crea admin en el primer arranque con PasswordHasher (PBKDF2).
-- Reiniciar no modifica una contraseña existente.
