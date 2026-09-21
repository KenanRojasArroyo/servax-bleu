# Servax Bleu — Suite de Investigación de Usuarios (Examen 1)

Suite web de 6 interfaces para capturar, estructurar y trazar el trabajo de campo del
proyecto **Servax Bleu** (granja acuícola de engorda de atún, Ensenada B.C.), conforme a
la rúbrica de *Investigación de Usuarios* de la materia Tecnologías Emergentes para el
Desarrollo de Soluciones (UABC, Grupo 952).

No requiere backend, build ni instalación de dependencias: es HTML + JS "vanilla" con
persistencia en `localStorage` del navegador y exportación a `.json` / `.csv`.

---

## 1. Requisitos previos

* Un navegador moderno (Chrome, Edge o Firefox recientes). No se requiere Node, .NET ni
  base de datos para esta suite — es independiente del sistema Servax Bleu (ASP.NET MVC5).
* Conexión a Internet **solo** para cargar Tailwind CSS y Chart.js desde CDN
  (`cdn.tailwindcss.com`, `cdn.jsdelivr.net`). Si se abre sin Internet, la app funciona pero
  sin estilos/gráfica de Roper Dynagram.

## 2. Instalación / ejecución

Todos los archivos viven en la carpeta `Scripts/` del repositorio principal.

**Opción A — abrir directo (más simple):**
1. Ir a `Scripts/`.
2. Abrir `index.html` con doble clic (o "Abrir con → navegador").
3. Navegar entre las 6 interfaces desde las tarjetas del menú.

**Opción B — servidor local (recomendado si el navegador bloquea `file://`):**
```bash
cd "Servax bleu unificacion/Servax bleu unificacion/Scripts"
python -m http.server 8080
# luego abrir http://localhost:8080/index.html
```

No hay paso de compilación: cualquier edición a los `.html` o `.js` se ve al recargar.

## 3. Estructura de archivos

```
Scripts/
├── index.html                          # Menú de navegación de la suite (con toggle 🌓)
├── interfaz1-entrevista-expertos.html  # Interfaz 1 — Entrevista a Expertos
├── interfaz2-usuarios-extremos.html    # Interfaz 2 — Usuarios Extremos
├── interfaz3-needfinding-iceberg.html  # Interfaz 3 — Needfinding ("El Iceberg")
├── interfaz4-empathy-map.html          # Interfaz 4 — Empathy Map ("The Parser")
├── Interfaz5-roper-dynagram.html       # Interfaz 5 — Roper Dynagram
├── Interfaz6-mapeo-requerimientos.html # Interfaz 6 — Mapeo de Requerimientos (obligatoria)
├── investigacion-motor-base.js         # Factory CRUD + localStorage + export (usa 1, 2 y 3)
├── entrevista-expertos-engine.js       # Motor de datos de la Interfaz 1
├── usuarios-extremos-engine.js         # Motor de datos de la Interfaz 2
├── observacion-directa-engine.js       # Motor de datos de la Interfaz 3
├── empathy-map-engine.js               # Motor de datos de la Interfaz 4 (cuadrícula 2×2)
├── roper-dynagram-engine.js            # Motor de datos de la Interfaz 5 (rueda + % en vivo)
├── requisitos-engine.js                # Motor de datos de la Interfaz 6 (ley embebida)
├── dashboard-charts.js                 # Gráficas del dashboard operativo del sistema
└── app.json                            # Dataset de referencia (RF/RNF, insights, decisiones)
```

> **Nota sobre la Interfaz 5 y 6:** antes vivían en un solo archivo
> (`InterfazesComb(5-6).html`) con pestañas. Se separaron en dos páginas independientes
> (`Interfaz5-roper-dynagram.html` e `Interfaz6-mapeo-requerimientos.html`) para que cada una
> tenga su propia URL, cumpla el mismo patrón de navegación que el resto de la suite (botón
> "← Suite", exportación e interruptor de tema en el header) y sea más fácil de demostrar por
> separado en la exposición oral.

## 4. Qué hace cada interfaz

| # | Interfaz | Persistencia (`localStorage`) | Motor |
|---|---|---|---|
| 1 | Entrevista a Expertos | `servaxbleu_entrevistas_expertos_v2` | `entrevista-expertos-engine.js` |
| 2 | Usuarios Extremos | `servaxbleu_usuarios_extremos_v2` | `usuarios-extremos-engine.js` |
| 3 | Needfinding ("El Iceberg") | motor propio de Observación Directa | `observacion-directa-engine.js` |
| 4 | Empathy Map ("The Parser") | `servaxbleu_empathy_map_v2` | `empathy-map-engine.js` |
| 5 | Roper Dynagram | `servaxbleu_roper_dynagram_v2` | `roper-dynagram-engine.js` |
| 6 | Mapeo de Requerimientos | `servaxbleu_requisitos_engine_v1` | `requisitos-engine.js` |

Cada interfaz incluye en su encabezado:
* **← Suite** — regresa al menú (`index.html`).
* **Exportar JSON / Exportar CSV** — descarga los registros capturados en esa interfaz.
* **🌓** — alterna entre tema claro y oscuro (se aplica con la clase `dark` en `<html>`).

Puntos especiales por interfaz:
* **Interfaz 4 (Empathy Map):** renderiza la cuadrícula real 2×2 (Dice/Hace/Piensa/Siente),
  no solo tablas de texto.
* **Interfaz 5 (Roper Dynagram):** el gráfico de dona (Chart.js) y los porcentajes de cada
  segmento (Realists, Open Minded, Adventurers, Organics) se recalculan siempre a partir del
  conteo real de usuarios asignados — nunca se escriben a mano.
* **Interfaz 6 (Mapeo de Requerimientos):** implementa la "ley embebida": al vincular un
  nuevo insight a un requisito, la prioridad calculada de ese requisito se recalcula
  automáticamente y se refleja al instante en la tabla de trazabilidad.

## 5. Carga de datos reales

El equipo debe reemplazar/completar en cada interfaz los datos de ejemplo con los de los
**4 usuarios reales** entrevistados u observados (mínimo 1 usuario extremo y 1 experto de
dominio), capturándolos directamente desde el formulario de cada pantalla. La Interfaz 6
incluye una semilla `SEMILLA_PLACEHOLDER` con RF/RNF de ejemplo — debe sustituirse por el
listado real (RF-01 a RF-21, RNF-01 a RNF-07) antes de la entrega final.

## 6. Exportar / respaldar datos

Cada interfaz exporta de forma independiente (no hay un "exportar todo"):
1. Abrir la interfaz.
2. Click en **Exportar JSON** o **Exportar CSV** en el header (o en el panel, en la
   Interfaz 5).
3. El navegador descarga el archivo con los registros de esa interfaz.

Como todo vive en `localStorage`, los datos son **por navegador y por origen**: cambiar de
navegador, usar modo incógnito o limpiar datos del sitio borra la captura. Se recomienda
exportar a JSON/CSV con frecuencia como respaldo antes de la demo en vivo.

## 7. Solución de problemas

| Síntoma | Causa probable | Solución |
|---|---|---|
| No cargan estilos / se ve sin diseño | Sin Internet (Tailwind/Chart.js son CDN) | Verificar conexión o servir localmente con acceso a Internet |
| La rueda de la Interfaz 5 no aparece | Chart.js no cargó | Revisar la consola del navegador y la conexión a `cdn.jsdelivr.net` |
| Los datos capturados "desaparecieron" | Se abrió en otro navegador/perfil o se limpió `localStorage` | Restaurar desde el último JSON exportado |
| Un botón "← Suite" no regresa | El archivo se abrió suelto, fuera de `Scripts/` | Ejecutar siempre desde dentro de la carpeta `Scripts/` |

---

*Documento generado como parte del entregable de Investigación de Usuarios — Examen 1,
Tecnologías Emergentes para el Desarrollo de Soluciones, UABC FIAD, Grupo 952.*
