# Proyecto Inmobiliaria - Laboratorio de Programación 2

## Integrantes del Grupo
* Lautaro Cadelago

## Instrucciones para levantar la Base de Datos (MySQL / XAMPP)
Tal como se solicita, el repositorio incluye el script de inicialización:
1. Iniciar los módulos de **Apache** y **MySQL** desde el panel de control de XAMPP.
2. Abrir el navegador e ingresar a phpMyAdmin (`http://localhost/phpmyadmin`).
3. Seleccionar la pestaña **Importar** en el menú superior.
4. Seleccionar el archivo `script.sql` ubicado en la raíz de este repositorio y presionar el botón "Importar" (o "Continuar") en la parte inferior.
5. Esto creará automáticamente la base de datos `InmobiliariaDB`, las tablas correspondientes y cargará los datos de prueba.

*(Nota para la corrección: En esta primera iteración, la aplicación MVC utiliza listas en memoria para garantizar la estabilidad del ABM, mientras que la estructura definitiva de persistencia en MySQL se encuentra en el script adjunto).*

## Diagrama Entidad-Relación (DER)
```mermaid
---
config:
  layout: elk
---
erDiagram
    PROPIETARIO ||--o{ INMUEBLE : posee
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
        string dni
        string email
        string telefono
        datetime fecha_registro
    }
    
    INMUEBLE {
        int id PK
        int propietario_id FK
        string nombre
        string descripcion
        string direccion
        decimal precio_diario
        string imagen_portada
        string imagenes_adicionales
        datetime fecha_creacion
    }
    
    INQUILINO {
        int id PK
        string nombre
        string apellido
        string dni
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
        string estado
        datetime fecha_creacion
    }
    
    PAGO {
        int id PK
        int reserva_id FK
        decimal monto
        string tipo_pago
        string estado
        datetime fecha_pago
    }