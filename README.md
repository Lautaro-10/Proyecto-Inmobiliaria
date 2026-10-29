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

Para una base ya creada, ejecutar una sola vez `scripts/actualizar-esquema-inmuebles-pagos.sql`. La migración amplía el campo de galería y agrega la auditoría específica de anulación de pagos; no borra los datos existentes.

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
        text imagenes_adicionales
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
