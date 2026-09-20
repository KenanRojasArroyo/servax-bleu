# Suite de Investigación de Usuarios — Servax Bleu (Examen 1)

Este README documenta **solo la suite de 6 interfaces de investigación de usuarios**
(`Scripts/interfaz1-*.html` … `Scripts/InterfazesComb(5-6).html`). Para el sistema
operativo (ASP.NET MVC / SQL Server / Text-to-SQL) ver el `README.md` de la raíz.

## Cómo ejecutarla
No requiere backend ni build. Basta con abrir en el navegador, dentro de `Scripts/`:

```
Scripts/index.html
```

Desde ahí hay un link a cada una de las 6 interfaces. Cada interfaz:
- Persiste sus datos en `localStorage` del navegador (una llave distinta por interfaz,
  prefijo `servaxbleu_*`), así que los datos sobreviven a recargar la página.
- Precarga datos reales de campo la primera vez que se abre (`seedSiVacio()` en cada
  motor), tomados de la entrevista con Arian Castillo Luhrs y de la entrevista/
  observación al Encargado de Corrales (usuario extremo). Si ya existen registros en
  `localStorage`, el seed no se vuelve a ejecutar (para no duplicar datos).
- Tiene botones "Exportar JSON" y "Exportar CSV" en el header.

## Estructura de archivos (`Scripts/`)
| Archivo | Interfaz |
|---|---|
| `investigacion-motor-base.js` | Factory CRUD + localStorage + export, reutilizada por 1, 2 y 3 |
| `entrevista-expertos-engine.js` + `interfaz1-entrevista-expertos.html` | 1 — Entrevista a Expertos |
| `usuarios-extremos-engine.js` + `interfaz2-usuarios-extremos.html` | 2 — Usuarios Extremos |
| `observacion-directa-engine.js` + `interfaz3-needfinding-iceberg.html` | 3 — Needfinding / Iceberg |
| `empathy-map-engine.js` + `interfaz4-empathy-map.html` | 4 — Empathy Map |
| `roper-dynagram-engine.js` + `requisitos-engine.js` + `InterfazesComb(5-6).html` | 5 y 6 — Roper Dynagram y Mapeo de Requerimientos (mismo archivo, con tabs) |
| `index.html` | Hub de navegación entre las 6 |

## Estado del trabajo de campo (usuarios reales cargados)
| # | Alias | Clasificación |
|---|---|---|
| 1 | Arian Castillo Luhrs | Experta de dominio (Supervisora de Operaciones, 9 años) |
| 2 | Encargado de Corrales y Control de Datos | Usuario extremo — Súper-experto |

**Pendiente:** el examen exige mínimo 4 usuarios reales. Faltan 2 entrevistas/
observaciones más, al menos una de perfil *mainstream* (ej. un operador de
alimentación con uso normal, no extremo). Ver `Docs/Fase0_Design_Space_Exploration.md`
para el detalle de qué necesidades siguen sin confirmar con más de una fuente.
