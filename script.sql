CREATE DATABASE InmobiliariaDB;
GO

USE InmobiliariaDB;
GO

CREATE TABLE Propietarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Apellido NVARCHAR(50) NOT NULL,
    Dni NVARCHAR(15) NOT NULL UNIQUE,
    Email NVARCHAR(100),
    Telefono NVARCHAR(50),
    FechaRegistro DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Inquilinos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL,
    Apellido NVARCHAR(50) NOT NULL,
    Dni NVARCHAR(15) NOT NULL UNIQUE,
    Email NVARCHAR(100),
    Telefono NVARCHAR(50),
    FechaRegistro DATETIME DEFAULT GETDATE()
);
GO

INSERT INTO Propietarios (Dni, Nombre, Apellido, Email, Telefono)
VALUES 
('Lautaro', 'Cadelago', '44993667', 'laucadelago123@gmail.com', '2664112233'),
('Maria', 'Laura', '12345678', 'mlaura@mail.com', '2664998877');

INSERT INTO Inquilinos (Dni, Nombre, Apellido, Email, Telefono)
VALUES 
('Juan', 'Perez','11111111', 'jperez@mail.com', '2664445566'),
('Ana', 'Martinez','22222222', 'amartinez@mail.com', '2664778899');
GO