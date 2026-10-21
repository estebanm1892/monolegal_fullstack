# Monolegal Billing — entorno de desarrollo

Monorepo .NET + Angular para la prueba técnica Monolegal. **La Fase 1 prepara el entorno; todavía no procesa facturas ni envía correos.** La evidencia ejecutada y las limitaciones están en [docs/RESULTS.md](docs/RESULTS.md).

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
| MongoDB | `mongodb://localhost:27017` | Servicio local sin seed |

La raíz de la API y las rutas de negocio devuelven 404: aún no están implementadas. OpenAPI no incluye Swagger UI. No hay scheduler, seed automático ni envío al iniciar.

### Visual Studio

Abrir `Monolegal.Billing.slnx`, seleccionar `Monolegal.Billing.Api` como proyecto de inicio y usar el perfil `http` con F5. El perfil `https` usa 7098 y puede requerir confiar en el certificado local mediante `dotnet dev-certs https --trust`. Angular se inicia desde su terminal, no desde Compose. La comprobación automatizada del SDK no sustituye una prueba interactiva de F5 en el IDE.

### Configuración y seguridad

`.env.example` es una plantilla sin secretos para las fases de persistencia y correo. Copiarla a `.env` es opcional en Fase 1; **la API no carga archivos `.env` ni consume esas claves todavía**. Compose fija los puertos de Mongo/Mailpit; los perfiles de `launchSettings.json` fijan los de la API. Las claves provisionales de la plantilla no modifican esos puertos ni habilitan CORS.

MongoDB y Mailpit solo publican puertos en `127.0.0.1`. Son servicios de prueba sin credenciales; no exponerlos a Internet. Los futuros correos usarán únicamente direcciones ficticias `example.test` y SMTP Mailpit.

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

Las pruebas de bootstrap no demuestran las reglas de facturas, integración Mongo/SMTP ni el recorrido E2E final. Esos criterios permanecen pendientes en [ACCEPTANCE_TESTS.md](docs/ACCEPTANCE_TESTS.md).

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

**Siguiente fase, solo con aprobación:** dominio y pruebas de transición. No existe aún un comando de seed ni un flujo para demostrar los correos.
