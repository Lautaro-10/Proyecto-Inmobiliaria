DROP DATABASE IF EXISTS InmobiliariaDb;
CREATE DATABASE InmobiliariaDB;

USE InmobiliariaDB;

CREATE TABLE Propietarios (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Apellido NVARCHAR(50) NOT NULL,
    Dni NVARCHAR(15) NOT NULL UNIQUE,
    Email NVARCHAR(100),
    Telefono NVARCHAR(50),
    FechaRegistro DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE Inquilinos (
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
    ImagenPortada VARCHAR (255),
    ImagenesAdicionales VARCHAR(255),
    Disponible TINYINT(1) NOT NULL DEFAULT 1,
    FechaCreacion  NOT NULL,
    PropietarioId INT NOT NULL,
    TipoInmuebleId INT NOT NULL,
    FechaCreacion DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PropietarioId) REFERENCES Propietarios(Id),
    FOREIGN KEY (TipoInmuebleId) REFERENCES TiposInmueble(Id)
);

CREATE TABLE IF NOT EXISTS Reservas (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    FechaInicio DATE NOT NULL,
    FechaFin DATE NOT NULL,
    MontoDiario DECIMAL(10,2) NOT NULL,
    InmuebleId INT NOT NULL,
    InquilinoId INT NOT NULL,
    FOREIGN KEY (InmuebleId) REFERENCES Inmuebles(Id),
    FOREIGN KEY (InquilinoId) REFERENCES Inquilinos(Id)
);


INSERT INTO Propietarios (Nombre, Apellido,Dni, Email, Telefono)
VALUES 
('Lautaro', 'Cadelago', '44993667', 'laucadelago123@gmail.com', '2664112233'),
('Maria', 'Laura', '12345678', 'mlaura@mail.com', '2664998877');

INSERT INTO Inquilinos (Nombre, Apellido, Dni, Email, Telefono)
VALUES 
('Juan', 'Perez','11111111', 'jperez@mail.com', '2664445566'),
('Ana', 'Martinez','22222222', 'amartinez@mail.com', '2664778899');