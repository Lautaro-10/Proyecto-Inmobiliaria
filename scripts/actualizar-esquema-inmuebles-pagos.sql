USE InmobiliariaDB;

ALTER TABLE Inmuebles
    MODIFY COLUMN ImagenesAdicionales TEXT NULL;

ALTER TABLE Pagos
    ADD COLUMN AnuladoPorUsuarioId INT NULL,
    ADD COLUMN FechaAnulacion DATETIME NULL,
    ADD CONSTRAINT FK_Pagos_AnuladoPorUsuario
        FOREIGN KEY (AnuladoPorUsuarioId) REFERENCES Usuarios(Id);
