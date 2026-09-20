# Fase 3 — Algoritmos de Atención (Servax Bleu)

Dos pantallas clave, siguiendo la misma lógica de los Task Flows de la Fase 2:
la **entrada** (`Home/Index`, el Dashboard) y la **pantalla de tarea principal**
que cierra el Flujo 1 (`MuestreoAgua`, registrar un muestreo de agua con alerta
automática). Notación de capas en cada elemento anotado:

- **Capa BASE** → tabla/columna/query SQL de donde viene el dato.
- **Capa MEDIA** → paso del Task Flow (Fase 2) al que pertenece.
- **Capa SUPERIOR** → componente de UI concreto.

---

## Pantalla 1 — Dashboard (`Home/Index`)

### Johnson Box
El cuadro resumen escaneable es la **tarjeta de alertas activas**: si algún
`MuestreoAgua` reciente cayó fuera de umbral, aparece arriba de todo, antes que
cualquier gráfica de crecimiento o KPI de biomasa — es lo primero que Arian dijo
que quiere ver ("que me avise de irregularidades sin tener que armar la gráfica").

### Pirámide invertida
- **Nivel 1 (crítico):** corrales con alerta activa hoy (calidad de agua fuera
  de rango, especie tóxica detectada).
- **Nivel 2 (esencial):** KPIs de biomasa, alimento y mortalidad por corral,
  promedio de calidad de agua.
- **Nivel 3 (secundario):** gráfica de crecimiento anual histórico, accesos a
  reportes exportables.

### Wireframe anotado

```
┌──────────────────────────────────────────────────────────┐
│ [LOGO Servax Bleu]  Corrales y Biomasa ▾  Alimentación ▾  │─① Navegación global (Fase 1,
│                      Analítica ▾   Asistente IA           │   sitemap _Layout.cshtml)
│ ┌────────────────────────────────────────────────────┐   │
│ │ ⚠ 2 corrales con alerta de calidad de agua HOY      │   │─② JOHNSON BOX — nivel 1
│ │   Corral 4: pH 6.1 (bajo umbral)  [ Ver detalle ▸ ] │   │  pirámide invertida
│ └────────────────────────────────────────────────────┘   │
│                                                            │
│  KPI Biomasa total   KPI Alimento hoy   KPI Mortalidad %  │─③ nivel 2
│  ┌──────────┐        ┌──────────┐       ┌──────────┐      │
│  │  38.2 t  │        │  1.4 t   │       │   0.6 %  │      │
│  └──────────┘        └──────────┘       └──────────┘      │
│                                                            │
│  ── Crecimiento anual por especie (gráfica) ──            │─④ nivel 3
│  ── Reportes: Mortalidad CSV · Carnada CSV ──             │
└──────────────────────────────────────────────────────────┘
```

| # | Capa BASE (SQL) | Capa MEDIA (Task Flow) | Capa SUPERIOR (UI) |
|---|---|---|---|
| ① | — (estático, `_Layout.cshtml`) | Punto de entrada de los 3 módulos del sitemap (Fase 1) | Barra de navegación fija |
| ② | `SELECT TOP N * FROM MuestreoAgua WHERE FueraDeUmbral=1 ORDER BY Fecha DESC` (misma comparación que dispara `ServicioNotificaciones` en Flujo 1, paso 7-8) | Cierre visible del Flujo 1: la alerta que el correo ya mandó, ahora también visible sin salir del Dashboard | Tarjeta Johnson Box, color de advertencia, con CTA "Ver detalle" |
| ③ | Agregados de `RegistroAlimentacion`, `HistorialCorral`, `MuestreoAgua` (query documentada en Fase 1, `GET /Home/Index`) | Punto de entrada informal a los módulos "Corrales y Biomasa" / "Alimentación y Barcos" | 3 tarjetas KPI |
| ④ | `CrecimientoAnual JOIN Corral JOIN Especie` | Side door hacia `Reporte/Mortalidad` y `Reporte/Carnada` (Flujo 2) | Gráfica + links de exportación CSV |

---

## Pantalla 2 — `MuestreoAgua` (cierre del Flujo 1: registrar muestreo con alerta)

### Johnson Box
Aquí el Johnson Box es el **resumen de umbrales activos** mostrado arriba del
formulario ("PH normal: 6.5–8.5 · Oxígeno mínimo: 5 mg/L…") — le permite al
operador de granja saber, antes de capturar, si el valor que está por escribir
ya se ve fuera de rango, sin tener que enviar el formulario para enterarse.

### Pirámide invertida
- **Nivel 1:** corral seleccionado + resultado inmediato (guardado / alerta
  disparada), que es la confirmación que el operador necesita primero.
- **Nivel 2:** los 6 valores capturados (Temperatura, Oxígeno, Profundidad, pH,
  Salinidad, Nutrientes).
- **Nivel 3:** historial de muestreos anteriores del mismo corral, visible al
  final para dar contexto sin competir con la captura.

### Wireframe anotado

```
┌──────────────────────────────────────────────────────────┐
│ [LOGO]   Corrales y Biomasa ▾ ...                         │─① Navegación global
│                                                            │
│  Corral: [ Corral 4 ▾ ]                                   │─② Capa media: paso 2
│                                                            │   del Flujo 1 (form
│  ┌ Umbrales de referencia ──────────────────────────┐     │   precargado)
│  │ pH 6.5–8.5 · O₂ ≥5 mg/L · Temp 18–26°C            │     │─③ JOHNSON BOX
│  └────────────────────────────────────────────────────┘   │
│                                                            │
│  Temperatura [___] °C     Oxígeno [___] mg/L               │─④ nivel 2
│  Profundidad [___] m      pH [___]                          │
│  Salinidad [___] ppm      Nutrientes [___]                  │
│                                                            │
│              [ Guardar muestreo ]                          │─⑤ paso 4 del Flujo 1
│                                                            │
│  ⚠ Fuera de umbral: se notificó por correo automáticamente│─⑥ paso 7-8 (resultado)
│                                                            │
│  ── Historial de muestreos de este corral ──               │─⑦ nivel 3 / side door
└──────────────────────────────────────────────────────────┘
```

| # | Capa BASE (SQL) | Capa MEDIA (Task Flow) | Capa SUPERIOR (UI) |
|---|---|---|---|
| ② | `ObtenerCorrales()` → `SELECT * FROM Corral` | Flujo 1, paso 2 (formulario con Corral precargado) | `<select>` con combo |
| ③ | Constantes de umbral usadas en la comparación del paso 7 | Flujo 1, paso 7 (comparación contra umbrales) | Johnson Box informativo, no editable |
| ④ | Columnas `Temperatura, Oxigeno, Profundidad, PH, Salinidad, Nutrientes` de `MuestreoAgua` | Flujo 1, paso 3 (captura) | Grupo de inputs numéricos con validación |
| ⑤ | `INSERT INTO MuestreoAgua (...)` | Flujo 1, paso 4-6 | Botón primario `POST /MuestreoAgua/Create` |
| ⑥ | Resultado de `ServicioNotificaciones.EnviarCorreoAlertaAsync` (best-effort, no bloquea el insert) | Flujo 1, paso 8 — respuesta elástica documentada en Fase 2 | Banner de confirmación/alerta |
| ⑦ | `GET /MuestreoAgua?idCorral={id}` | Side door del Flujo 1 | Tabla de historial, orden `Fecha DESC` |

---

## Nota de alcance
Estas dos pantallas se eligieron porque cierran, de extremo a extremo, el
**Flujo 1** de la Fase 2 (el recomendado para el prototipo navegable por ser
el de mayor valor y el más fácil de demostrar en vivo) y porque el Dashboard es
literalmente la pantalla de entrada del sitemap de la Fase 1. No se wireframearon
pantallas de los Flujos 2 y 3 porque el examen solo pide **2 pantallas clave**,
no una por flujo.
