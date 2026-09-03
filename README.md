# Proyecto Inmobiliaria - Laboratorio de Programación 2

## Integrantes del Grupo
* Lautaro Cadelago

## Instrucciones para levantar la Base de Datos (MySQL / XAMPP)
Tal como se solicita, el repositorio incluye el script de inicialización:
1. Iniciar los módulos de **Apache** y **MySQL** desde el panel de control de XAMPP.
2. Abrir el navegador e ingresar a phpMyAdmin (`http://localhost/phpmyadmin`).
3. Seleccionar la pestaña **Importar** en el menú superior.
4. Seleccionar el archivo `script.sql` ubicado en la raíz de este repositorio y presionar el botón "Importar" en la parte inferior.
5. Esto creará automáticamente la base de datos `InmobiliariaDB`, las tablas correspondientes y cargará los datos de prueba.

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
    USUARIO ||--o{ PROPIETARIO : es
    USUARIO ||--o{ INQUILINO : es
    USUARIO ||--o{ EMPLEADO : actua_como

    PROPIETARIO {
        int id PK
        string nombre
        string apellido
        string dni UK
        string email
        string telefono
        datetime fecha_registro
    }

    TIPO_INMUEBLE {
        int id PK
        string descripcion
    }

    INMUEBLE {
        int id PK
        int propietario_id FK
        int tipo_inmueble_id FK
        string nombre
        string descripcion
        string direccion
        int cupo
        decimal latitud
        decimal longitud
        decimal precio_diario
        decimal porcentaje_reserva
        string imagen_portada
        string imagenes_adicionales
        boolean disponible
        date fecha_creacion
    }

    INQUILINO {
        int id PK
        string nombre
        string apellido
        string dni UK
        string email
        string telefono
        datetime fecha_registro
    }

    RESERVA {
        int id PK
        int inquilino_id FK
        int inmueble_id FK
        decimal monto_diario
        date fecha_inicio
        date fecha_fin
    }

    PAGO {
        int id PK
        int reserva_id FK
        decimal monto
        string concepto
        string estado
        datetime fecha_pago
    }

    USUARIO {
        int id PK
        string email UK
        string password_hash
        string rol
        boolean activo
        datetime fecha_creacion
        datetime ultimo_acceso
    }

    EMPLEADO {
        int id PK
        int usuario_id FK
        string nombre
        string puesto
        boolean puede_eliminar
        boolean puede_gestionar_usuarios
    }
