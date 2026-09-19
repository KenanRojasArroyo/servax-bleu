# Fase 1 — Los 3 Engranajes (Servax Bleu)

> Corrige y reemplaza la versión anterior de este documento: agrega el sitemap jerárquico
> (faltaba por completo) y convierte el listado de rutas en pares literales
> ruta→query, en vez de patrones genéricos, siguiendo la guía de expectativas del profesor.

## 1.1 · Mapa de AI (sitemap)

```
Inicio (Dashboard)
├── Monitoreo (histórico Calidad, legacy)
├── Corrales y Biomasa
│   ├── Corrales ──► Detalle de Corral
│   ├── Especies ──► Detalle de Especie
│   ├── Inventario de Peces
│   ├── Muestreos de Agua
│   ├── Crecimiento Anual
│   └── Historial de Corrales
├── Alimentación y Barcos
│   ├── Barcos
│   ├── Alimentos
│   ├── Registro de Alimentación
│   ├── Distribución de Carnada
│   ├── Reporte de Mortalidad ──► Exportar CSV
│   └── Reporte de Carnada ──► Exportar CSV
└── Asistente IA (Text-to-SQL) — consulta en lenguaje natural, solo lectura
```

Todas las ramas están enlazadas desde el menú principal (`Views/Shared/_Layout.cshtml`); ninguna es contenido huérfano.

## 1.2 · Categorías → ERD

Ver `ERD_ServaxBleu.drawio` (11 tablas, PK/FK y cardinalidades) y `schema_ServaxBleu.sql`.

## 1.3 · Navegación → rutas y queries principales (pares literales)

### Corrales y Biomasa

| Ruta | Query principal |
|---|---|
| `GET /Corral` | `SELECT * FROM Corral ORDER BY Nombre` |
| `GET /Corral/Details/{id}` | `SELECT * FROM Corral WHERE IdCorral = @id` |
| `POST /Corral/Create` | `INSERT INTO Corral (Nombre, Ubicacion, CapacidadMaxima, FechaInstalacion, Estado, Observaciones) VALUES (...)` |
| `POST /Corral/Edit/{id}` | `UPDATE Corral SET ... WHERE IdCorral = @id` |
| `POST /Corral/Delete/{id}` | `DELETE FROM Corral WHERE IdCorral = @id` |
| `GET /Especie` | `SELECT * FROM Especie ORDER BY Nombre` |
| `GET /Especie/Details/{id}` | `SELECT * FROM Especie WHERE IdEspecie = @id` |
| `GET /InventarioPez?idCorral={id}` | `SELECT ip.*, c.Nombre, e.Nombre FROM InventarioPez ip JOIN Corral c ON... JOIN Especie e ON... WHERE (@idCorral IS NULL OR ip.IdCorral=@idCorral)` |
| `POST /InventarioPez/CargarExcel` | Lee el Excel con `ExcelDataReader` → `INSERT INTO InventarioPez` por fila |
| `GET /MuestreoAgua?idCorral={id}` | `SELECT m.*, c.Nombre FROM MuestreoAgua m JOIN Corral c ON... WHERE (@idCorral IS NULL OR m.IdCorral=@idCorral) ORDER BY Fecha DESC` |
| `POST /MuestreoAgua/Create` | `INSERT INTO MuestreoAgua (...)`; si algún valor cae fuera de umbral, dispara `ServicioNotificaciones` (correo) |
| `GET /CrecimientoAnual?idCorral={id}` | `SELECT ca.*, c.Nombre, e.Nombre FROM CrecimientoAnual ca JOIN Corral c ON... JOIN Especie e ON... WHERE (@idCorral IS NULL OR ca.IdCorral=@idCorral) ORDER BY Anio DESC` |
| `GET /HistorialCorral?idCorral={id}` | `SELECT h.*, c.Nombre FROM HistorialCorral h JOIN Corral c ON... WHERE (@idCorral IS NULL OR h.IdCorral=@idCorral) ORDER BY Fecha DESC` |

### Alimentación y Barcos

| Ruta | Query principal |
|---|---|
| `GET /Barco` | `SELECT * FROM Barco ORDER BY Nombre` |
| `GET /Alimento` | `SELECT * FROM Alimento ORDER BY Nombre` |
| `GET /RegistroAlimentacion?idCorral={id}` | `SELECT r.*, c.Nombre, a.Nombre FROM RegistroAlimentacion r JOIN Corral c ON... JOIN Alimento a ON... WHERE (@idCorral IS NULL OR r.IdCorral=@idCorral)` |
| `GET /DistribucionAlimento` | `SELECT d.*, b.Nombre, a.Nombre, c.Nombre FROM DistribucionAlimento d JOIN Barco b ON... JOIN Alimento a ON... JOIN Corral c ON...` |
| `POST /DistribucionAlimento/Distribuir(idCorral, idAlimento, cantidadTotalToneladas, fecha)` | Reparte proporcional: `cantidadBarco = cantidadTotal * (Barco.CapacidadToneladas / SUM(CapacidadToneladas) de barcos activos)` → 1 `INSERT` por barco |
| `GET /Reporte/Mortalidad?idCorral={id}&desde={f}&hasta={f}` | `SELECT c.Nombre, DATEFROMPARTS(YEAR(Fecha),MONTH(Fecha),1) AS Mes, SUM(CantidadKg), SUM(Mortalidad) FROM RegistroAlimentacion r JOIN Corral c ON... WHERE (@idCorral IS NULL OR...) AND (@desde IS NULL OR Fecha>=@desde) AND (@hasta IS NULL OR Fecha<=@hasta) GROUP BY c.Nombre, YEAR(Fecha), MONTH(Fecha)` |
| `GET /Reporte/MortalidadCsv?...` | Misma query, exportada a `.csv` |
| `GET /Reporte/Carnada?desde={f}&hasta={f}` | `SELECT a.TipoAlimento, a.Nombre, COUNT(*), SUM(CantidadToneladas), AVG(CantidadToneladas) FROM DistribucionAlimento d JOIN Alimento a ON... WHERE (@desde IS NULL OR...) AND (@hasta IS NULL OR...) GROUP BY a.TipoAlimento, a.Nombre, a.UnidadMedida, a.CostoUnitario, a.StockActual ORDER BY SUM(CantidadToneladas) DESC` |
| `GET /Reporte/CarnadaCsv?...` | Misma query, exportada a `.csv` |

### Dashboard y monitoreo

| Ruta | Query principal |
|---|---|
| `GET /Home/Index` | Agrega: promedio de calidad de agua por corral, `CrecimientoAnual` JOIN `Corral`+`Especie` para la gráfica de crecimiento |
| `GET /Home/Monitoreo?fechaInicio={f}&fechaFin={f}` | `SELECT * FROM Calidad WHERE Fecha BETWEEN @fechaInicio AND @fechaFin` (tabla legacy) |
| `POST /Home/CargarExcel` | Carga histórica a `Calidad` vía `ExcelDataReader` |

### Asistente IA

| Ruta | Query principal |
|---|---|
| `POST /Home/ConsultaIA` (Text-to-SQL) | `TextToSqlService` traduce la pregunta en lenguaje natural a un `SELECT` de solo lectura contra las 11 tablas (nunca `INSERT/UPDATE/DELETE/DROP`) |

## 1.4 · Metadatos → índices, FK y taxonomías

Sin cambios respecto a la versión anterior — ver tabla de índices y lista de FKs ya entregada:

- **Por corral / especie / tipo de alimento / rango de fecha**: filtros presentes en la mayoría de los `Index`.
- **Índices**: `IX_MuestreoAgua_Corral_Fecha`, `IX_RegistroAlimentacion_Corral_Fecha`, `IX_HistorialCorral_Corral_Fecha` (en `schema_ServaxBleu.sql`).
- **FKs**: las 10 relaciones documentadas en el ERD (todas hacia `Corral`, `Especie`, `Alimento` o `Barco`); `Calidad` es la única tabla sin FK (legacy, aislada).
