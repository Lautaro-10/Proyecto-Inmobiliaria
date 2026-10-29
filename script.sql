CREATE DATABASE IF NOT EXISTS InmobiliariaDB
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE InmobiliariaDB;

CREATE TABLE IF NOT EXISTS Propietarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Apellido NVARCHAR(50) NOT NULL,
    Dni NVARCHAR(15) NOT NULL UNIQUE,
    Email NVARCHAR(100),
    Telefono NVARCHAR(50),
    FechaRegistro DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Inquilinos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Apellido NVARCHAR(50) NOT NULL,
    Dni NVARCHAR(15) NOT NULL UNIQUE,
    Email NVARCHAR(100),
    Telefono NVARCHAR(50),
    FechaRegistro DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS TiposInmueble (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Descripcion VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS Inmuebles (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre VARCHAR(200) NOT NULL,
    Descripcion VARCHAR(255) NOT NULL,
    Direccion VARCHAR(200) NOT NULL,
    Cupo INT NOT NULL,
    Latitud DECIMAL(10, 8),
    Longitud DECIMAL(11, 8),
    PrecioPorDia DECIMAL(10,2) NOT NULL,
    PorcentajeReserva DECIMAL(5,2) NOT NULL DEFAULT 10.00,
    ImagenPortada VARCHAR(255) NULL,
    ImagenesAdicionales TEXT NULL,
    Disponible TINYINT(1) NOT NULL DEFAULT 1,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PropietarioId INT NOT NULL,
    TipoInmuebleId INT NOT NULL,
    INDEX IX_Inmuebles_PropietarioId (PropietarioId),
    INDEX IX_Inmuebles_TipoInmuebleId (TipoInmuebleId),
    FOREIGN KEY (PropietarioId) REFERENCES Propietarios(Id),
    FOREIGN KEY (TipoInmuebleId) REFERENCES TiposInmueble(Id)
);

CREATE TABLE IF NOT EXISTS Usuarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Email VARCHAR(150) NOT NULL UNIQUE,
    PasswordHash VARCHAR(500) NOT NULL,
    Nombre VARCHAR(100) NOT NULL DEFAULT '',
    Apellido VARCHAR(100) NOT NULL DEFAULT '',
    AvatarUrl VARCHAR(255) NULL,
    Rol VARCHAR(30) NOT NULL DEFAULT 'Empleado',
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UltimoAcceso DATETIME NULL
);

CREATE TABLE IF NOT EXISTS Reservas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    MontoDiario DECIMAL(10,2) NOT NULL,
    InmuebleId INT NOT NULL,
    InquilinoId INT NOT NULL,
    Estado VARCHAR(30) NOT NULL DEFAULT 'Pendiente',
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FechaFinOriginal DATE NULL,
    FechaTerminacion DATETIME NULL,
    CreadoPorUsuarioId INT NULL,
    TerminadoPorUsuarioId INT NULL,
    INDEX IX_Reservas_Disponibilidad (InmuebleId, Estado, FechaInicio, FechaFin),
    INDEX IX_Reservas_InquilinoId (InquilinoId),
    FOREIGN KEY (InmuebleId) REFERENCES Inmuebles(Id),
    FOREIGN KEY (InquilinoId) REFERENCES Inquilinos(Id),
    FOREIGN KEY (CreadoPorUsuarioId) REFERENCES Usuarios(Id),
    FOREIGN KEY (TerminadoPorUsuarioId) REFERENCES Usuarios(Id)
);

CREATE TABLE IF NOT EXISTS Pagos (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    ReservaId INT NOT NULL,
    Monto DECIMAL(10,2) NOT NULL,
    Concepto VARCHAR(100) NOT NULL,
    Estado VARCHAR(30) NOT NULL DEFAULT 'Pendiente',
    FechaPago DATETIME NULL,
    FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CreadoPorUsuarioId INT NULL,
    ModificadoPorUsuarioId INT NULL,
    FechaModificacion DATETIME NULL,
    AnuladoPorUsuarioId INT NULL,
    FechaAnulacion DATETIME NULL,
    INDEX IX_Pagos_ReservaId_FechaPago (ReservaId, FechaPago),
    FOREIGN KEY (ReservaId) REFERENCES Reservas(Id),
    FOREIGN KEY (CreadoPorUsuarioId) REFERENCES Usuarios(Id),
    FOREIGN KEY (ModificadoPorUsuarioId) REFERENCES Usuarios(Id),
    FOREIGN KEY (AnuladoPorUsuarioId) REFERENCES Usuarios(Id)
);

INSERT INTO Usuarios (Email, PasswordHash, Rol)
SELECT 'admin@demo.local', 'AQAAAAIAAYagAAAAEAsxUvQjK5JgASTZ+JhBueGcFmPkY8qz1X3NxAzsUuSBNMSfM2SUj14Au2F6TmTAlQ==', 'Administrador'
WHERE NOT EXISTS (SELECT 1 FROM Usuarios WHERE Email = 'admin@demo.local');


INSERT INTO Propietarios (Nombre, Apellido, Dni, Email, Telefono)
SELECT 'Lautaro', 'Cadelago', '44993667', 'laucadelago123@gmail.com', '2664112233'
WHERE NOT EXISTS (SELECT 1 FROM Propietarios WHERE Dni = '44993667')
UNION ALL
SELECT 'Maria', 'Laura', '12345678', 'mlaura@mail.com', '2664998877'
WHERE NOT EXISTS (SELECT 1 FROM Propietarios WHERE Dni = '12345678');

INSERT INTO Inquilinos (Nombre, Apellido, Dni, Email, Telefono)
SELECT 'Juan', 'Perez', '11111111', 'jperez@mail.com', '2664445566'
WHERE NOT EXISTS (SELECT 1 FROM Inquilinos WHERE Dni = '11111111')
UNION ALL
SELECT 'Ana', 'Martinez', '22222222', 'amartinez@mail.com', '2664778899'
WHERE NOT EXISTS (SELECT 1 FROM Inquilinos WHERE Dni = '22222222');

INSERT INTO TiposInmueble (Descripcion)
SELECT 'Casa' WHERE NOT EXISTS (SELECT 1 FROM TiposInmueble WHERE Descripcion = 'Casa')
UNION ALL
SELECT 'Departamento' WHERE NOT EXISTS (SELECT 1 FROM TiposInmueble WHERE Descripcion = 'Departamento')
UNION ALL
SELECT 'Monoambiente' WHERE NOT EXISTS (SELECT 1 FROM TiposInmueble WHERE Descripcion = 'Monoambiente')
UNION ALL
SELECT 'Loft' WHERE NOT EXISTS (SELECT 1 FROM TiposInmueble WHERE Descripcion = 'Loft');

INSERT INTO Inmuebles (
    Nombre, Descripcion, Direccion, Cupo, Latitud, Longitud, PrecioPorDia,
    PorcentajeReserva, ImagenPortada, Disponible, PropietarioId, TipoInmuebleId)
SELECT 'Depto Centro', 'Moderno en pleno centro', 'San Martín 650', 3,
       -33.30050000, -66.33780000, 35000.00, 15.00, NULL, 1,
       (SELECT Id FROM Propietarios WHERE Dni = '44993667'),
       (SELECT Id FROM TiposInmueble WHERE Descripcion = 'Departamento')
WHERE NOT EXISTS (SELECT 1 FROM Inmuebles WHERE Nombre = 'Depto Centro')
  AND EXISTS (SELECT 1 FROM Propietarios WHERE Dni = '44993667')
  AND EXISTS (SELECT 1 FROM TiposInmueble WHERE Descripcion = 'Departamento');

INSERT INTO Reservas (FechaInicio, FechaFin, MontoDiario, InmuebleId, InquilinoId)
SELECT DATE_ADD(CURDATE(), INTERVAL 1 DAY), DATE_ADD(CURDATE(), INTERVAL 7 DAY), 35000.00,
       (SELECT Id FROM Inmuebles WHERE Nombre = 'Depto Centro'),
       (SELECT Id FROM Inquilinos WHERE Dni = '11111111')
WHERE EXISTS (SELECT 1 FROM Inmuebles WHERE Nombre = 'Depto Centro')
  AND EXISTS (SELECT 1 FROM Inquilinos WHERE Dni = '11111111')
  AND NOT EXISTS (
      SELECT 1 FROM Reservas r
      WHERE r.InmuebleId = (SELECT Id FROM Inmuebles WHERE Nombre = 'Depto Centro')
        AND r.InquilinoId = (SELECT Id FROM Inquilinos WHERE Dni = '11111111')
        AND r.FechaInicio = DATE_ADD(CURDATE(), INTERVAL 1 DAY)
        AND r.FechaFin = DATE_ADD(CURDATE(), INTERVAL 7 DAY));