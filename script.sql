CREATE DATABASE InmobiliariaDB;
GO

USE InmobiliariaDB;
GO

CREATE TABLE Propietarios (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150),
    Telefono NVARCHAR(50),
    FechaRegistro DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Inquilinos (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Apellido NVARCHAR(100) NOT NULL,
    Email NVARCHAR(150),
    Telefono NVARCHAR(50),
    FechaRegistro DATETIME DEFAULT GETDATE()
);
GO

INSERT INTO Propietarios (Nombre, Apellido, Email, Telefono)
VALUES 
('Carlos', 'Gomez', 'cgomez@mail.com', '2664112233'),
('Maria', 'Laura', 'mlaura@mail.com', '2664998877');

INSERT INTO Inquilinos (Nombre, Apellido, Email, Telefono)
VALUES 
('Juan', 'Perez', 'jperez@mail.com', '2664445566'),
('Ana', 'Martinez', 'amartinez@mail.com', '2664778899');
GO