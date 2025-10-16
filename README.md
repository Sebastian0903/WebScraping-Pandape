# SG_Semilla_PostgreSQL - Proyecto Semilla con Clean Architecture + CQRS + PostgreSQL

Este proyecto es una plantilla base para aplicaciones .NET 9 siguiendo los principios de Clean Architecture y el patrón CQRS (Command Query Responsibility Segregation). Incluye gestión de usuarios con soporte para autenticación LDAP y JWT, sistema de permisos basado en roles, una interfaz de usuario en Angular, y utiliza PostgreSQL como base de datos.

## Estructura del Proyecto

### Backend (.NET 9)

- **Api**: ASP.NET Core Web API con configuración de Serilog, Swagger, CORS, JWT y LDAP.
  - **Controllers**: Endpoints para Auth, Users, Roles, Permissions y Todos.
  - **Services**: Implementación de servicios para autenticación, usuarios, roles y permisos.
  - **Middleware**: Manejo de excepciones y extensiones.

- **Application**: Implementación de CQRS con MediatR.
  - **Features**: Commands, Queries y Handlers organizados por entidad.
  - **DTOs**: Objetos de transferencia de datos para la API.
  - **Validators**: Validación de datos con FluentValidation.
  - **Mappings**: Configuración de AutoMapper.

- **Domain**: Capa central con reglas de negocio.
  - **Entities**: User, Role, Permission, RolePermission y Todo.
  - **Repositories**: Interfaces para acceso a datos.

- **Infrastructure**: Implementación de acceso a datos.
  - **Repositories**: Implementaciones concretas de los repositorios.
  - **Persistence**: Configuración de Entity Framework Core.
  - **Migrations**: Migraciones de base de datos.

### Frontend (Angular)

- **sg-semilla-ui**: Aplicación Angular con arquitectura modular.
  - **Core**: Servicios, guardias y modelos compartidos.
  - **Pages**: Componentes de página (Auth, Dashboard, Users, etc.).
  - **Shared**: Componentes, directivas y pipes reutilizables.
  - **Layout**: Componentes de estructura (Header, Sidebar, etc.).

## Características

### Arquitectura y Patrones
- **Clean Architecture**: Separación clara de responsabilidades en capas (Domain, Application, Infrastructure, API)
- **CQRS**: Implementado con MediatR para separar operaciones de lectura y escritura
- **Repository Pattern**: Abstracción del acceso a datos mediante interfaces
- **Dependency Injection**: Inyección de dependencias nativa de ASP.NET Core

### Seguridad
- **Autenticación JWT**: Tokens JWT para autenticación de API
- **ximage.png**: Soporte para autenticación contra directorios LDAP corporativos
- **Autorización basada en políticas**: Permisos granulares basados en roles
- **Hashing seguro de contraseñas**: Implementación de salt y SHA-256

### Validación y Calidad
- **FluentValidation**: Validación declarativa de comandos y DTOs
- **Validación de campos numéricos**: Reglas específicas para DocumentNumber
- **Manejo centralizado de excepciones**: Middleware para captura y formateo de errores

### Herramientas y Utilidades
- **Swagger/OpenAPI**: Documentación interactiva de API con soporte para JWT
- **Serilog**: Logging estructurado y configurable
- **Entity Framework Core**: ORM para acceso a datos con migraciones
- **CORS**: Configurado para aplicaciones Angular

### Frontend
- **Angular**: Framework moderno para la interfaz de usuario
- **Lazy Loading**: Carga diferida de módulos para mejor rendimiento
- **Guards**: Protección de rutas basada en autenticación
- **Servicios HTTP**: Comunicación con la API backend

## Requisitos

### Backend
- .NET 9 SDK
- PostgreSQL 15 o superior
- Visual Studio 2022 o superior (recomendado) o Visual Studio Code

### Frontend
- Node.js 18.x o superior
- npm 9.x o superior
- Angular CLI 16.x o superior

## Configuración y Ejecución

### Configuración del Backend

1. Clona el repositorio:
   ```bash
   git clone [url-del-repositorio]
   cd SG_Semilla
   ```

2. Configura la cadena de conexión en `Api/appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=SG_Semilla;Username=postgres;Password=admin"
   }
   ```

3. Configura los parámetros de JWT en `Api/appsettings.json`:
   ```json
   "JwtSettings": {
     "SecretKey": "tu-clave-secreta-muy-larga-y-segura",
     "Issuer": "SG_Semilla",
     "Audience": "SG_SemillaUsers",
     "ExpirationMinutes": 60
   }
   ```

4. Configura los parámetros de LDAP (opcional) en `Api/appsettings.json`:
   ```json
   "LdapSettings": {
     "Server": "ldap.example.com",
     "Port": 389,
     "BindDn": "cn=admin,dc=example,dc=com",
     "BindPassword": "admin_password",
     "SearchBase": "ou=users,dc=example,dc=com",
     "SearchFilter": "(uid={0})"
   }
   ```

5. Ejecuta las migraciones para crear la base de datos:
   ```bash
   dotnet ef database update --project Infrastructure --startup-project Api
   ```

6. Inicia la API:
   ```bash
   cd Api
   dotnet run
   ```
   La API estará disponible en `https://localhost:7168` y `http://localhost:5207`

### Configuración del Frontend

1. Navega al directorio del frontend:
   ```bash
   cd sg-semilla-ui
   ```

2. Instala las dependencias:
   ```bash
   npm install
   ```

3. Configura la URL de la API en `src/environments/environment.ts`:
   ```typescript
   export const environment = {
     production: false,
     apiUrl: 'https://localhost:7001/api/v1'
   };
   ```

4. Inicia el servidor de desarrollo:
   ```bash
   npm start
   ```
   La aplicación estará disponible en `http://localhost:4200`

## Pruebas y Documentación

### Swagger UI

La API incluye documentación interactiva con Swagger UI, accesible en:

```
https://localhost:7001/swagger
```

Desde Swagger puedes:
- Explorar todos los endpoints disponibles
- Probar las operaciones directamente desde el navegador
- Ver los modelos de datos y esquemas de respuesta
- Autenticarte usando el endpoint `/api/v1/Auth/login` y el botón "Authorize"

### Pruebas Unitarias

El proyecto incluye pruebas unitarias que puedes ejecutar con:

```bash
dotnet test
```

### Datos de Prueba

Al iniciar por primera vez, el sistema crea automáticamente:
- Usuario administrador: `admin` / `admin123`
- Roles predefinidos: Admin, User
- Permisos básicos para cada recurso

## Implementación CQRS

El proyecto implementa el patrón CQRS (Command Query Responsibility Segregation) para separar las operaciones de lectura y escritura:

### Comandos y Consultas
- **Commands**: Operaciones que modifican el estado (Create, Update, Delete)
  - Ejemplo: `CreateTodoCommand`, `UpdateUserCommand`
  - Retornan resultados que indican éxito/fracaso y datos relevantes

- **Queries**: Operaciones que solo leen datos (Get)
  - Ejemplo: `GetTodosQuery`, `GetUserByIdQuery`
  - Optimizadas para lectura y proyección de datos

- **Handlers**: Procesadores específicos para cada Command o Query
  - Implementan la lógica de negocio
  - Utilizan repositorios para acceder a los datos

- **Validators**: Validación de datos de entrada para Commands
  - Implementados con FluentValidation
  - Ejecutados automáticamente por pipeline de MediatR

## Modelo de Datos

El sistema se basa en las siguientes entidades principales:

- **User**: Usuarios del sistema
  - Propiedades: Id (Guid), Username, Email, DocumentNumber, PasswordHash, Salt, IsActive, IsLdapUser
  - Relaciones: Pertenece a un Role

- **Role**: Roles de usuario con permisos asociados
  - Propiedades: Id (int), Name, Description, IsActive
  - Relaciones: Tiene muchos Users, tiene muchos Permissions a través de RolePermission

- **Permission**: Permisos individuales del sistema
  - Propiedades: Id (Guid), Name, Code, Description, Category
  - Relaciones: Pertenece a muchos Roles a través de RolePermission

- **RolePermission**: Tabla de relación muchos a muchos
  - Propiedades: RoleId, PermissionId
  - Relaciones: Pertenece a un Role y un Permission

- **Todo**: Ejemplo de entidad de negocio
  - Propiedades: Id (Guid), Title, Description, IsCompleted, CreatedAt, UserId
  - Relaciones: Pertenece a un User

## Autenticación y Autorización

### Autenticación JWT

La API utiliza tokens JWT para autenticación:

- **Generación de token**: En login y registro exitoso
- **Validación de token**: Middleware JWT integrado
- **Renovación de token**: Endpoint específico para renovar tokens
- **Claims personalizados**: Incluye username, rol y permisos

Para pruebas, puedes usar las credenciales predefinidas:
- Usuario: `admin`
- Contraseña: `admin123`

### Integración LDAP

El sistema soporta autenticación contra directorios LDAP corporativos:

- **Autenticación dual**: Intenta primero autenticación local, luego LDAP
- **Creación automática**: Los usuarios LDAP se crean automáticamente en la base de datos local en su primer inicio de sesión
- **Marcado de usuarios**: El campo `IsLdapUser` indica si un usuario se autentica mediante LDAP
- **Verificación de grupo**: Valida pertenencia a grupos LDAP específicos
- **Configuración flexible**: Parámetros configurables en appsettings.json

### Autorización basada en políticas

Implementa un sistema de autorización granular:

- **Políticas por recurso**: Definidas para Users, Roles, Permissions y Content
- **Permisos por operación**: Read, Write, Delete para cada recurso
- **Atributos en controladores**: Aplicados a nivel de controlador o acción
- **Verificación en tiempo de ejecución**: Validación de permisos en cada solicitud

## Validaciones

El sistema implementa validaciones robustas para garantizar la integridad de los datos:

- **Validación de DocumentNumber**:
  - Debe contener solo dígitos numéricos
  - Implementado con expresiones regulares

- **Validación de credenciales**:
  - Nombres de usuario: entre 3 y 50 caracteres
  - Contraseñas: entre 6 y 100 caracteres
  - Email: formato válido y único

- **Validación de entidades**:
  - Unicidad de username y email
  - Existencia de relaciones (RoleId, etc.)
  - Reglas de negocio específicas

- **Seguridad de contraseñas**:
  - Almacenamiento con hash y salt
  - Algoritmo SHA-256 para hashing

## Contribución y Desarrollo

### Flujo de Trabajo para Contribuciones

Si deseas contribuir a este proyecto, por favor sigue estos pasos:

1. Haz un fork del repositorio
2. Crea una rama para tu característica (`git checkout -b feature/amazing-feature`)
3. Implementa tus cambios siguiendo las convenciones de código
4. Asegúrate de que todas las pruebas pasen (`dotnet test`)
5. Haz commit de tus cambios siguiendo las convenciones de commit
6. Haz push a la rama (`git push origin feature/amazing-feature`)
7. Abre un Pull Request con una descripción detallada

### Convenciones de Código

- **C#**: Seguimos las [Convenciones de Codificación de Microsoft](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- **Angular**: Seguimos la [Guía de Estilo de Angular](https://angular.io/guide/styleguide)
- **Commits**: Utilizamos [Conventional Commits](https://www.conventionalcommits.org/) para mensajes de commit

### Agregar Nuevas Características

Para agregar nuevas entidades o características al proyecto:

1. Crea la entidad en `Domain/Entities`
2. Implementa la interfaz del repositorio en `Domain/Repositories`
3. Crea los DTOs correspondientes en `Application/DTOs`
4. Implementa los Commands y Queries en `Application/Features`
5. Agrega validadores en `Application/Validators`
6. Implementa el repositorio en `Infrastructure/Repositories`
7. Registra las dependencias en `Infrastructure/DependencyInjection.cs`
8. Crea el controlador en `Api/Controllers`
9. Implementa los componentes frontend en Angular



## Contacto y Soporte

Para preguntas, sugerencias o reportes de errores, por favor:

- Abre un Issue en el repositorio

---

*Este proyecto fue desarrollado como una semilla para aplicaciones empresariales con .NET y Angular, siguiendo las mejores prácticas de arquitectura limpia y seguridad.*