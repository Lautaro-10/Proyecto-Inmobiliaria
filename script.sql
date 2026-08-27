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

INSERT INTO Propietarios (Nombre, Apellido,Dni, Email, Telefono)
VALUES 
('Lautaro', 'Cadelago', '44993667', 'laucadelago123@gmail.com', '2664112233'),
('Maria', 'Laura', '12345678', 'mlaura@mail.com', '2664998877');

INSERT INTO Inquilinos (Nombre, Apellido, Dni, Email, Telefono)
VALUES 
('Juan', 'Perez','11111111', 'jperez@mail.com', '2664445566'),
('Ana', 'Martinez','22222222', 'amartinez@mail.com', '2664778899');