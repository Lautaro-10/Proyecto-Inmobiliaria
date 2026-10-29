# Proyecto Inmobiliaria - Laboratorio de Programación 2

## Integrantes del Grupo
* Lautaro Cadelago

## Instrucciones para levantar la Base de Datos (MySQL / XAMPP)
El script de inicialización no elimina bases existentes y puede volver a ejecutarse sin borrar los datos:
1. Iniciar los módulos de **Apache** y **MySQL** desde el panel de control de XAMPP.
2. Abrir el navegador e ingresar a phpMyAdmin (`http://localhost/phpmyadmin`).
3. Seleccionar la pestaña **Importar** en el menú superior.
4. Seleccionar el archivo `script.sql` ubicado en la raíz de este repositorio y presionar el botón "Importar" en la parte inferior.
5. Esto creará automáticamente la base de datos `InmobiliariaDB`, las tablas correspondientes y los datos de prueba que aún no existan.

Las imágenes de inmuebles se cargan desde el formulario y se guardan en `Inmobiliaria/wwwroot/uploads/inmuebles`. La portada es obligatoria al crear; al editar se puede conservar la actual o reemplazarla. Se admiten archivos PNG, JPEG y WebP de hasta 5 MB cada uno y hasta 10 imágenes adicionales por carga.

## Diagrama Entidad-Relación (DER)
```mermaid
---
config:
  layout: elk
---
erDiagram
    PROPIETARIO ||--o{ INMUEBLE : posee
    TIPO_INMUEBLE ||--o{ INMUEBLE : clasifica
    INQUILINO ||--o{ RESERVA : realiza
    INMUEBLE ||--o{ RESERVA : contiene
    RESERVA ||--o{ PAGO : genera
    USUARIO o|--o{ RESERVA : crea
    USUARIO o|--o{ RESERVA : termina
    USUARIO o|--o{ PAGO : crea
    USUARIO o|--o{ PAGO : modifica
    USUARIO o|--o{ PAGO : anula

    PROPIETARIO {
        int id PK
        nvarchar(50) nombre
        nvarchar(50) apellido
        nvarchar(15) dni UK
        nvarchar(100) email
        nvarchar(50) telefono
        datetime fechaRegistro
    }

    TIPO_INMUEBLE {
        int id PK
        varchar(100) descripcion
    }

    INMUEBLE {
        int id PK
        int propietarioId FK
        int tipoInmuebleId FK
        varchar(200) nombre
        varchar(255) descripcion
        varchar(200) direccion
        int cupo
        decimal latitud "DECIMAL(10,8)"
        decimal longitud "DECIMAL(11,8)"
        decimal precioPorDia "DECIMAL(10,2)"
        decimal porcentajeReserva "DECIMAL(5,2)"
        varchar(255) imagenPortada
        text imagenesAdicionales
        boolean disponible
        datetime fechaCreacion
    }

    INQUILINO {
        int id PK
        nvarchar(50) nombre
        nvarchar(50) apellido
        nvarchar(15) dni UK
        nvarchar(100) email
        nvarchar(50) telefono
        datetime fechaRegistro
    }

    RESERVA {
        int id PK
        date fechaInicio
        date fechaFin
        decimal montoDiario "DECIMAL(10,2)"
        int inmuebleId FK
        int inquilinoId FK
        varchar(30) estado
        datetime fechaCreacion
        date fechaFinOriginal
        datetime fechaTerminacion
        int creadoPorUsuarioId FK
        int terminadoPorUsuarioId FK
    }

    PAGO {
        int id PK
        int reservaId FK
        decimal monto "DECIMAL(10,2)"
        varchar(100) concepto
        varchar(30) estado
        datetime fechaPago
        datetime fechaCreacion
        int creadoPorUsuarioId FK
        int modificadoPorUsuarioId FK
        datetime fechaModificacion
        int anuladoPorUsuarioId FK
        datetime fechaAnulacion
    }

    USUARIO {
        int id PK
        varchar(150) email UK
        varchar(500) passwordHash
        varchar(100) nombre
        varchar(100) apellido
        varchar(255) avatarUrl
        varchar(30) rol
        boolean activo
        datetime fechaCreacion
        datetime ultimoAcceso
    }
