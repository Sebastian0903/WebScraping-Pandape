# Instrucciones para Ejecutar Pruebas

## Requisitos Previos

- .NET SDK 6.0 o superior
- Visual Studio 2022 o Visual Studio Code con extensiones para .NET

## Ejecutar Todas las Pruebas

Para ejecutar todas las pruebas del proyecto, utilice el siguiente comando desde la raíz del proyecto:

```bash
dotnet test
```

## Ejecutar Pruebas Específicas

Para ejecutar pruebas de un proyecto específico:

```bash
dotnet test ./Tests/Tests.csproj
```

Para ejecutar una categoría específica de pruebas (usando filtros):

```bash
dotnet test --filter "Category=Unit"
```

Para ejecutar pruebas de una clase específica:

```bash
dotnet test --filter "FullyQualifiedName~Tests.Controllers.TodosControllerTests"
```

## Pruebas Disponibles

El proyecto incluye las siguientes categorías de pruebas:

1. **Pruebas de Controladores**
   - `TodosControllerTests`: Prueba las operaciones CRUD para los todos.

2. **Pruebas de Validadores**
   - `CreateTodoCommandValidatorTests`: Valida las reglas para la creación de todos.

3. **Pruebas de Autenticación**
   - `AuthenticationTests`: Verifica el comportamiento de los endpoints autenticados.

4. **Pruebas de Servicios**
   - `LdapServiceTests`: Prueba la integración con el servicio LDAP (requiere configuración).
   - `UserServiceTests`: Prueba la lógica de gestión de usuarios.

## Notas Importantes

- Algunas pruebas están marcadas con `Skip` porque requieren recursos externos como un servidor LDAP.
- Las pruebas de integración pueden requerir una base de datos de prueba configurada.
- Para ejecutar pruebas que requieren autenticación, asegúrese de configurar los valores adecuados en `appsettings.json`.

## Solución de Problemas Comunes

### Error: No se puede conectar al servidor LDAP

Las pruebas de LDAP están configuradas para omitirse por defecto. Si desea ejecutarlas, debe:

1. Configurar un servidor LDAP de prueba o usar uno existente.
2. Actualizar la configuración en `appsettings.json` o usar variables de entorno.
3. Eliminar el atributo `Skip` de las pruebas relevantes.

### Error: Pruebas de controlador fallan con error de autenticación

Asegúrese de que el contexto de autenticación esté correctamente configurado en las pruebas. Revise `AuthenticationTests.cs` para ver ejemplos de cómo configurar un usuario autenticado simulado.

## Mejores Prácticas

1. Ejecute las pruebas regularmente durante el desarrollo.
2. Mantenga las pruebas independientes entre sí.
3. Use datos de prueba que sean representativos pero no sensibles.
4. Agregue nuevas pruebas al implementar nuevas funcionalidades.