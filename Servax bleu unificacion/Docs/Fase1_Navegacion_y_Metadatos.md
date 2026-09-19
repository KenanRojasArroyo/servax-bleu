# Fase 1 — Engranajes "Navegación" y "Metadatos" (Servax Bleu)

> Preparado por Kenan (backend/BD). Listo para incrustar en el documento de arquitectura (Fase 1).

## Engranaje: Navegación — rutas y queries principales

Convención de rutas ASP.NET MVC: `/{Controlador}/{Accion}/{id?}`

### CRUD de catálogo/operación (patrón repetido en 9 controladores)

| Ruta | Acción | Query principal |
|---|---|---|
| `GET /{Entidad}` | Listar | `SELECT ... FROM {Tabla} [JOIN ...] [WHERE @filtro IS NULL OR ...] ORDER BY ...` |
| `GET /{Entidad}/Create` | Formulario alta | `SELECT` de catálogos para combos (Corral, Especie, Alimento, Barco) |
| `POST /{Entidad}/Create` | Alta | `INSERT INTO {Tabla} (...) VALUES (...)` |
| `GET /{Entidad}/Edit/{id}` | Formulario edición | `SELECT ... WHERE Id{Entidad} = @id` |
| `POST /{Entidad}/Edit/{id}` | Editar | `UPDATE {Tabla} SET ... WHERE Id{Entidad} = @id` |
| `GET /{Entidad}/Delete/{id}` | Confirmar baja | `SELECT` del registro a borrar |
| `POST /{Entidad}/Delete/{id}` | Baja | `DELETE FROM {Tabla} WHERE Id{Entidad} = @id` |

Entidades que siguen este patrón completo: `Corral`, `Especie` (+ `Details`), `InventarioPez`, `MuestreoAgua`, `Alimento`, `Barco`, `RegistroAlimentacion`, `CrecimientoAnual`.

Entidades con patrón reducido (solo alta + baja, son bitácoras/transacciones, no se editan):
- `HistorialCorral`: `GET/POST /HistorialCorral`, `Create`, `Delete` — `INSERT INTO HistorialCorral ...` / `DELETE ... WHERE IdHistorial = @id`
- `DistribucionAlimento`: `GET /DistribucionAlimento`, `GET/POST /DistribucionAlimento/Distribuir(idCorral, idAlimento, cantidadTotalToneladas, fecha)`, `Delete` — reparte proporcionalmente por `Barco.CapacidadToneladas` entre las embarcaciones activas.

### Rutas de importación (Excel → BD)
- `POST /InventarioPez/CargarExcel(archivoExcel)`
- `POST /MuestreoAgua/CargarExcel(archivoExcel)` — dispara alerta por correo si algún valor cae fuera de umbral
- `POST /Home/CargarExcel(archivoExcel)` — carga histórica a `Calidad` (legacy)

### Rutas del dashboard / monitoreo
- `GET /Home/Index` → agrega: promedio calidad de agua por corral, últimas alertas, `CrecimientoAnual` por especie (JOIN `Corral`+`Especie`)
- `GET /Home/Monitoreo(fechaInicio, fechaFin)` → `SELECT ... FROM Calidad WHERE Fecha BETWEEN @fechaInicio AND @fechaFin`
- `POST /Home/ProbarCorreo` → prueba de `ServicioNotificaciones` (SMTP)

### Rutas de reportes
- `GET /Reporte/Mortalidad(idCorral?, desde?, hasta?)` → `SELECT SUM(CantidadKg), SUM(Mortalidad) FROM RegistroAlimentacion JOIN Corral ... GROUP BY Corral, Mes`
- `GET /Reporte/MortalidadCsv(...)` → mismo query, exportado a CSV
- `GET /Reporte/Carnada(desde?, hasta?)` → `SELECT SUM/AVG(CantidadToneladas), COUNT(*) FROM DistribucionAlimento JOIN Alimento GROUP BY TipoAlimento`
- `GET /Reporte/CarnadaCsv(...)` → mismo query, exportado a CSV

### Consulta en lenguaje natural (IA)
- `POST /Home/[endpoint TextToSql]` → `TextToSqlService` traduce la pregunta a `SELECT` de solo lectura contra las 11 tablas (nunca `INSERT/UPDATE/DELETE/DROP`)

## Engranaje: Metadatos — taxonomías, filtros, índices y FKs

### Taxonomías / filtros
- **Por corral** (`IdCorral`): filtro presente en `InventarioPez`, `MuestreoAgua`, `CrecimientoAnual`, `RegistroAlimentacion`, `DistribucionAlimento`, `HistorialCorral`, `Reporte.Mortalidad`
- **Por especie** (`IdEspecie`): `InventarioPez.Estado`, `Especie.EsToxica` (bandera de especie tóxica), `Especie.Activo`
- **Por rango de fecha** (`desde`/`hasta`): `Reporte.Mortalidad`, `Reporte.Carnada`, `Home.Monitoreo`
- **Por tipo de alimento** (`Alimento.TipoAlimento`): agrupación en `Reporte.Carnada`
- **Por estado de alerta**: `MuestreoAgua.Irregularidad` (texto libre generado cuando un valor cae fuera de umbral); dispara notificación por correo

### Índices (definidos en `schema_ServaxBleu.sql`)
| Índice | Tabla(s) | Para qué consulta |
|---|---|---|
| `IX_MuestreoAgua_Corral_Fecha` | `MuestreoAgua(IdCorral, Fecha)` | Historial de calidad de agua por corral y rango de fechas |
| `IX_RegistroAlimentacion_Corral_Fecha` | `RegistroAlimentacion(IdCorral, Fecha)` | Reporte de mortalidad agrupado por mes |
| `IX_HistorialCorral_Corral_Fecha` | `HistorialCorral(IdCorral, Fecha)` | Línea de tiempo de un corral |

### Claves foráneas (FK)
`InventarioPez.IdCorral→Corral`, `InventarioPez.IdEspecie→Especie`, `MuestreoAgua.IdCorral→Corral`, `CrecimientoAnual.IdCorral→Corral`, `CrecimientoAnual.IdEspecie→Especie`, `RegistroAlimentacion.IdCorral→Corral`, `RegistroAlimentacion.IdAlimento→Alimento`, `DistribucionAlimento.IdBarco→Barco`, `DistribucionAlimento.IdAlimento→Alimento`, `DistribucionAlimento.IdCorral→Corral`, `HistorialCorral.IdCorral→Corral`.

`Calidad` no tiene FK: es la tabla legacy protegida, aislada del resto del modelo relacional.
