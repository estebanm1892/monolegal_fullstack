# Monolegal Billing — entorno de desarrollo

Monorepo .NET + Angular para la prueba técnica Monolegal. **El dominio, las reglas de transición y la persistencia MongoDB con seed ya existen; todavía no se envían correos ni hay endpoints de negocio.** La evidencia ejecutada y las limitaciones están en [docs/RESULTS.md](docs/RESULTS.md).

## Requisitos

- Visual Studio 2026 con desarrollo ASP.NET, o SDK .NET 10.0.402 para compilar desde terminal.
- Node.js 24.19.0 y npm 11.17.0 (versiones verificadas).
- Docker Desktop con motor Linux y Docker Compose.
- Puertos locales disponibles: API 5091/7098, Angular 4200, MongoDB 27017, SMTP 1025, Mailpit 8025.

Angular y sus herramientas se instalan localmente mediante `npm ci`; no es necesario instalar Angular CLI globalmente. Versiones y aprobación: [DECISIONS_LOCKED.md](docs/DECISIONS_LOCKED.md).

## Inicio local

Desde la raíz del repositorio:

```powershell
docker compose up -d
dotnet build Monolegal.Billing.slnx
dotnet run --project src/Monolegal.Billing.Api --launch-profile http
```

En otra terminal:

```powershell
cd frontend/monolegal-billing
npm ci
npm start -- --host localhost --port 4200
```

| Servicio | Dirección | Resultado esperado en Fase 1 |
|---|---|---|
| Angular | http://localhost:4200 | Página inicial estática |
| API OpenAPI | http://localhost:5091/openapi/v1.json | Documento JSON, solo en Development |
| Mailpit | http://localhost:8025 | Interfaz de captura local |
| MongoDB | `mongodb://localhost:27017` | Servicio local; requiere seed explícito |

La raíz de la API y las rutas de negocio devuelven 404: aún no están implementadas. OpenAPI no incluye Swagger UI. No hay scheduler ni envío al iniciar. El seed **no** se ejecuta al iniciar la API; requiere el comando explícito de la sección siguiente.

## Seed de desarrollo (Fase 3)

Con Mongo levantado y la API compilada:

```powershell
dotnet run --project src/Monolegal.Billing.Api -- --seed
```

Resultado esperado: `customersInserted=3, invoicesInserted=6, customerCount=3, invoiceCount=6`. Repetir el comando no duplica documentos ni sobrescribe estados ya procesados (`customersInserted=0, invoicesInserted=0`).

Para restaurar los estados iniciales de las 6 facturas de demostración:

```powershell
dotnet run --project src/Monolegal.Billing.Api -- --seed --reset
```

`--reset` solo elimina los ids documentados del seed, es exclusivo del entorno Development y no debe usarse contra datos reales. `--reset` sin `--seed` falla con código 1 y un mensaje claro, sin iniciar la API. La conexión se toma de `Mongo:ConnectionString` (`appsettings.Development.json`) y puede sobrescribirse con la variable de entorno `MONGO_CONNECTION_STRING`.

### Visual Studio

Abrir `Monolegal.Billing.slnx`, seleccionar `Monolegal.Billing.Api` como proyecto de inicio y usar el perfil `http` con F5. El perfil `https` usa 7098 y puede requerir confiar en el certificado local mediante `dotnet dev-certs https --trust`. Angular se inicia desde su terminal, no desde Compose. La comprobación automatizada del SDK no sustituye una prueba interactiva de F5 en el IDE.

### Configuración y seguridad

`.env.example` es una plantilla sin secretos. La API **no carga archivos `.env`**: `MONGO_CONNECTION_STRING`, `MONGO_DATABASE` y `MAIL__*` se leen del entorno del proceso y tienen valores locales por defecto en `appsettings.Development.json`; también funcionan las claves jerárquicas estándar de .NET. CORS todavía no se enlaza. Compose fija los puertos de Mongo/Mailpit; los perfiles de `launchSettings.json` fijan los de la API.

MongoDB y Mailpit solo publican puertos en `127.0.0.1`. Son servicios de prueba sin credenciales; no exponerlos a Internet. El adaptador SMTP rechaza hosts que no sean loopback, TLS, credenciales y direcciones fuera de `example.test`.

## Verificaciones

Desde la raíz:

```powershell
dotnet build Monolegal.Billing.slnx
dotnet test Monolegal.Billing.slnx
docker compose config
docker compose ps
docker compose exec -T mongo mongosh --quiet --eval 'db.adminCommand({ ping: 1 })'
```

Desde `frontend/monolegal-billing`:

```powershell
npm run build
npm test -- --watch=false
```

Las pruebas actuales cubren bootstrap, reglas de dominio, integración real con MongoDB y las plantillas/validación segura de Mailpit. Todavía no demuestran el lote SMTP real ni el recorrido E2E final. Esos criterios permanecen pendientes en [ACCEPTANCE_TESTS.md](docs/ACCEPTANCE_TESTS.md).

## Estructura y dependencias

```text
frontend/monolegal-billing     Angular independiente
src/Monolegal.Billing.Domain   Sin referencias a otras capas
src/Monolegal.Billing.Application     -> Domain
src/Monolegal.Billing.Infrastructure  -> Application, Domain
src/Monolegal.Billing.Api             -> Application, Infrastructure
tests/Monolegal.Billing.UnitTests         -> Domain, Application
tests/Monolegal.Billing.IntegrationTests   -> Infrastructure, Api
```

La DI de ejemplo usa un reloj reemplazable. La arquitectura prevista está en [ARCHITECTURE_DECISIONS.md](docs/ARCHITECTURE_DECISIONS.md); el orden autorizado de implementación está en [IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md).

## Detener servicios

Detener API y Angular con Ctrl+C. Desde la raíz, `docker compose stop` detiene Mongo y Mailpit sin eliminar el volumen Mongo. No usar `down -v` salvo que se quiera eliminar explícitamente los datos de prueba. Mailpit no tiene volumen persistente configurado: recrear su contenedor puede perder mensajes capturados.

**Estado:** Fase 4 y Fase 5 completadas en backend. Correo Mailpit, orquestación de recordatorios y endpoints REST (clientes, facturas, resumen, procesamiento, health) existen y tienen pruebas. **Siguiente fase, solo con aprobación:** dashboard Angular (Fase 6). No hay ejecución automática ni envío a direcciones reales.
