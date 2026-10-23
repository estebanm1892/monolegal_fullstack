# Monolegal Billing — recordatorios de facturas

Monorepo .NET 10 + Angular 22 + MongoDB + Mailpit para la prueba técnica Monolegal.
El flujo funcional es: el usuario dispara `Procesar recordatorios` desde el dashboard,
la API envía **un correo por factura elegible** a través de Mailpit (SMTP local) y
**solo después de la aceptación SMTP** actualiza el estado de la factura.

## Requisitos

- SDK .NET 10.0.402 (o Visual Studio 2026 con la carga de trabajo ASP.NET).
- Node.js 24.19.0 y npm 11.17.0 (versiones verificadas).
- Docker Desktop con motor Linux y Docker Compose.
- Puertos locales libres: API 5091/7098, Angular 4200, MongoDB 27017, SMTP 1025, Mailpit 8025.

Angular y sus herramientas se instalan localmente con `npm ci`; no hace falta Angular CLI global.

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

| Servicio | Dirección | Uso |
|---|---|---|
| Angular | http://localhost:4200 | Dashboard: resumen, tabla, filtros, paginación y disparo manual |
| Swagger UI | http://localhost:5091/swagger | Documentación interactiva de la API, solo en Development |
| API OpenAPI | http://localhost:5091/openapi/v1.json | Documento JSON, solo en Development |
| API health | http://localhost:5091/health | Estado del proceso |
| Mailpit | http://localhost:8025 | Interfaz de captura de los correos enviados |
| MongoDB | `mongodb://localhost:27017` | Datos de demostración; requiere seed explícito |

No hay scheduler ni envío automático: el único disparador es `POST /api/reminders/process`.
El seed **no** corre al iniciar la API.

## Reglas del proceso

- Estados elegibles en MongoDB: `primerrecordatorio` → `segundorecordatorio` → `desactivado`.
  Solo se procesan los dos primeros; `desactivado` y estados desconocidos no se tocan.
- Máximo una transición por factura por ejecución.
- Cada ejecución toma un snapshot de IDs al inicio; lo que se modifica durante el lote no se
  vuelve a procesar en esa misma ejecución.
- Cada factura se reserva con un claim atómico antes de enviar. El intento pasa a `Dispatching`
  **antes** de la llamada SMTP para impedir reenvíos automáticos inseguros.
- Si SMTP falla antes de aceptar, se registra el fallo, se libera la reserva y el estado no cambia.
- Si SMTP acepta, se actualiza el estado con una escritura condicional (id + estado previo + token).
- Si la confirmación es ambigua, la factura queda en revisión manual: no se reintenta sola.

Un correo aceptado por Mailpit significa *aceptación del servidor*, no entrega al destinatario.

## Seed de desarrollo

Con Mongo levantado y la API compilada:

```powershell
dotnet run --project src/Monolegal.Billing.Api -- --seed
```

Resultado esperado: `customersInserted=3, invoicesInserted=6, customerCount=3, invoiceCount=6`.
Repetirlo no duplica documentos ni sobrescribe estados ya procesados
(`customersInserted=0, invoicesInserted=0`).

Para volver al estado inicial de la demo (facturas, intentos de notificación y correos de
Mailpit), usar el procedimiento de [Restablecer la demo](#restablecer-la-demo).

La conexión se toma de `Mongo:ConnectionString` (`appsettings.Development.json`) y puede
sobrescribirse con `MONGO_CONNECTION_STRING`.

### Visual Studio

Abrir `Monolegal.Billing.slnx`, seleccionar `Monolegal.Billing.Api` como proyecto de inicio y usar
el perfil `http` con F5. El perfil `https` usa 7098 y puede requerir
`dotnet dev-certs https --trust`. Angular se inicia desde su propia terminal, no desde Compose.

## Restablecer la demo

Deja la demo en su estado inicial: 3 clientes, 6 facturas con los estados del seed y Mailpit sin
correos. No es necesario borrar el volumen de MongoDB ni recrear contenedores.

### 1. Restaurar MongoDB

Con MongoDB levantado, desde la raíz del repositorio:

```powershell
dotnet run --project src/Monolegal.Billing.Api -- --seed --reset
```

El comando elimina únicamente los ids documentados del seed —clientes, facturas e intentos de
notificación asociados— y vuelve a insertar los datos con `$setOnInsert`; no toca otros
documentos. Es exclusivo de Development y no debe usarse contra datos reales. `--reset` sin
`--seed` termina con código `1`, imprime el error y no inicia el servidor.

Salida esperada:

```text
Reset requested: only the documented seed ids are deleted before seeding.
Seed completed: customersInserted=3, invoicesInserted=6, customerCount=3, invoiceCount=6
```

Estados iniciales de las 6 facturas:

| Estado | Cantidad |
|---|---|
| `primerrecordatorio` | 2 |
| `segundorecordatorio` | 2 |
| `pendiente` | 1 |
| `desactivado` | 1 |
| **Total** | **6** |

Si la API está corriendo desde el mismo binario o configuración, detenerla antes de ejecutar el
comando para evitar que Windows bloquee los DLL durante la compilación. El seed es un proceso
aparte y no inicia el servidor; después se puede volver a iniciar la API.

### 2. Vaciar Mailpit

Mailpit captura los correos en memoria y no tiene volumen persistente. Para vaciarlo sin recrear
el contenedor:

```powershell
Invoke-RestMethod -Method Delete 'http://localhost:8025/api/v1/messages'
```

Verificación:

```powershell
(Invoke-RestMethod 'http://localhost:8025/api/v1/messages').messages_count
# 0
```

También se puede vaciar desde la interfaz de Mailpit. Recrear el contenedor
(`docker compose up -d --force-recreate mailpit`) también lo vacía, porque no tiene volumen
persistente. No usar `docker compose down -v` solo para limpiar Mailpit: elimina el volumen de
MongoDB y con él los datos de la demo.

### 3. Verificar el estado inicial

Con la API en ejecución:

```powershell
Invoke-RestMethod 'http://localhost:5091/api/invoices/summary'
```

El resumen debe mostrar los cuatro estados de la tabla anterior. La demo queda lista para
disparar `Procesar recordatorios` desde Angular o `POST /api/reminders/process` desde Swagger.

## Swagger UI y OpenAPI

La API conserva `Microsoft.AspNetCore.OpenApi` 10.0.13 como generador del documento y utiliza
únicamente `Swashbuckle.AspNetCore.SwaggerUI` 10.3.1 para presentar una interfaz interactiva.
`dotnet restore` instala ambos paquetes; no se requiere una herramienta global ni una instalación
adicional.

Con la API ejecutándose mediante el perfil `http` en Development:

- Swagger UI: http://localhost:5091/swagger
- Documento OpenAPI: http://localhost:5091/openapi/v1.json

Desde Swagger UI se pueden desplegar las operaciones agrupadas por Clientes, Facturas,
Recordatorios y Salud del servicio, revisar parámetros, esquemas, ejemplos y ejecutar consultas
GET mediante **Try it out**.

> **Advertencia:** `POST /api/reminders/process` no es una simulación. Procesa globalmente todas
> las facturas elegibles, modifica sus estados en MongoDB después de la aceptación SMTP y genera
> un mensaje individual por factura en Mailpit. Los filtros del dashboard Angular no restringen
> su alcance. Para una prueba repetible, usa únicamente datos locales controlados y restaura la
> demo con el procedimiento de [Restablecer la demo](#restablecer-la-demo).

Swagger UI y el documento OpenAPI se publican solo en Development. En Production ambas rutas
responden `404`.

## Endpoints

| Ruta | Método | Notas |
|---|---|---|
| `/api/customers` | GET | Clientes del seed |
| `/api/invoices` | GET | `page`, `pageSize` (1..100), `customerId`, `status`; ítems planos y metadatos de paginación |
| `/api/invoices/summary` | GET | Totales por estado, por cliente y monto COP |
| `/api/reminders/process` | POST | Solo en Development; devuelve el resumen del lote |
| `/api/reminders/runs/{batchId}` | GET | Intentos de notificación registrados para el lote |
| `/health` | GET | Health check |

La paginación inválida devuelve `400` y los errores usan ProblemDetails
(`application/problem+json`).

## Configuración y seguridad

`.env.example` es una plantilla sin secretos. La API **no carga archivos `.env`**:
`MONGO_CONNECTION_STRING`, `MONGO_DATABASE` y `MAIL__*` se leen del entorno del proceso y tienen
valores locales por defecto en `appsettings.Development.json`; también funcionan las claves
jerárquicas estándar de .NET. Un `.env` real no se versiona.

CORS está habilitado solo en Development y limitado a `http://localhost:4200`. El `POST` de
procesamiento responde `403` fuera de Development. MongoDB y Mailpit solo publican puertos en
`127.0.0.1`; son servicios de prueba sin credenciales y no deben exponerse a Internet.
El adaptador SMTP rechaza hosts que no sean loopback, TLS, credenciales y direcciones fuera de
`example.test`.

## Verificaciones

Desde la raíz:

```powershell
dotnet build Monolegal.Billing.slnx
dotnet test Monolegal.Billing.slnx -c Release
dotnet format Monolegal.Billing.slnx --verify-no-changes
docker compose config
docker compose ps
docker compose exec -T mongo mongosh --quiet --eval 'db.adminCommand({ ping: 1 })'
```

Desde `frontend/monolegal-billing`:

```powershell
npm run build
npm test -- --watch=false
```

Suites actuales: 70 tests unitarios + 45 de integración en backend, 31 en frontend.
CI en `.github/workflows/ci.yml` con jobs de backend, frontend y E2E; la ejecución en GitHub Actions queda pendiente del primer push.

## Estructura y dependencias

```text
frontend/monolegal-billing             Angular independiente
src/Monolegal.Billing.Domain           Sin referencias a otras capas
src/Monolegal.Billing.Application      -> Domain
src/Monolegal.Billing.Infrastructure   -> Application, Domain
src/Monolegal.Billing.Api              -> Application, Infrastructure
tests/Monolegal.Billing.UnitTests         -> Domain, Application
tests/Monolegal.Billing.IntegrationTests   -> Infrastructure, Api
```

## Limitaciones conocidas

- Confirmación SMTP ambigua: la factura queda en revisión manual; no hay reintento automático ni
  reconciliación automática entre MongoDB y el servidor SMTP.
- Un lote sin candidatos devuelve un `batchId` cuyo `GET /api/reminders/runs/{batchId}` responde
  `404`, porque no se persiste una colección de ejecuciones vacías.
- No hay autenticación: el `POST` de procesamiento está pensado para el demo local, no para
  exponerse a Internet.

## Detener servicios

Detener API y Angular con Ctrl+C. `docker compose stop` detiene Mongo y Mailpit sin eliminar el
volumen de Mongo. Para restaurar los datos sin borrar el volumen, ver
[Restablecer la demo](#restablecer-la-demo). No usar `down -v` salvo que se quieran borrar los
datos de prueba. Mailpit no tiene volumen persistente: recrear su contenedor puede perder los
mensajes capturados.
