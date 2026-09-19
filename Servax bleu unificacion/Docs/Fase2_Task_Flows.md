# Fase 2 — Task Flows (Servax Bleu)

Notación: **(U)** = acción del usuario, **(S)** = respuesta del sistema.
Los 3 flujos se construyeron a partir de los controllers y servicios reales del
repositorio (`MuestreoAguaController`, `DistribucionAlimentoController`,
`HomeController.ConsultarAsistente` + `TextToSqlService`), no son hipotéticos.

---

## Flujo 1 — Registrar muestreo de agua con alerta automática

**Módulo:** `MuestreoAgua` · **Actores:** Control de Calidad / operador de granja

### Ruta feliz
1. (U) Entra a *Corrales y Biomasa → Muestreos de Agua* y pulsa "Nuevo".
2. (S) Muestra formulario con `Corral` precargado desde `ObtenerCorrales()`.
3. (U) Captura Temperatura, Oxígeno, Profundidad, PH, Salinidad, Nutrientes.
4. (U) Envía el formulario (`POST /MuestreoAgua/Create`).
5. (S) Valida `ModelState` (requiere `IdCorral` > 0).
6. (S) Inserta el registro en `MuestreoAgua`.
7. (S) Compara los valores contra los umbrales de irregularidad.
8. (S) Si algún valor está fuera de rango, dispara `ServicioNotificaciones.EnviarCorreoAlertaAsync` (SMTP) de forma asíncrona.
9. (S) Redirige a `Index` con mensaje de confirmación.

### Side doors
- Desde `Index`, filtrar por `idCorral` (`GET /MuestreoAgua?idCorral={id}`) sin pasar por `Create`.
- Desde el Dashboard (`Home/Index`), la gráfica de calidad promedio enlaza directo al corral con el valor más reciente.

### Comportamientos erráticos
- Usuario no selecciona corral → `ModelState.AddModelError("IdCorral", ...)`, se re-renderiza el formulario sin perder los demás valores capturados.
- Usuario captura valores fuera de rango físico (ej. PH negativo) → **hueco actual**: no hay validación de rango en el modelo, solo el disparo de alerta; se agregará `RangeAttribute` en `Models/MuestreoAgua.cs` (ver Backlog al final).
- Falla el envío de correo (SMTP caído) → `ServicioNotificaciones` captura la excepción y la escribe en `Debug.WriteLine`; el registro en base de datos **ya se guardó**, la alerta es best-effort y no bloquea el flujo.

### Respuesta de la arquitectura
El insert a `MuestreoAgua` y la evaluación de alerta están desacoplados: el `INSERT` se confirma primero y el correo se envía después, dentro de un `try/catch` que nunca propaga la excepción hacia la vista. Así, un fallo de red o de credenciales SMTP nunca revierte el guardado del muestreo ni tira un error 500 al usuario — en el peor caso, la alerta simplemente no llega y el dato queda disponible para revisarlo manualmente desde el `Index`.

---

## Flujo 2 — Distribuir carnada entre barcos

**Módulo:** `DistribucionAlimento` · **Actor:** Logística / Alimentación

### Ruta feliz
1. (U) Entra a *Alimentación y Barcos → Distribución de Carnada → Distribuir*.
2. (S) Carga combos de Corral, Alimento y Barcos activos (`CargarListasDesplegables`).
3. (U) Elige corral, alimento, cantidad total en toneladas y fecha.
4. (U) Envía (`POST /DistribucionAlimento/Distribuir`).
5. (S) Valida `cantidadTotalToneladas > 0`.
6. (S) Obtiene barcos activos (`ObtenerBarcosActivos`).
7. (S) Calcula el reparto proporcional a `CapacidadToneladas` de cada barco (`CalcularDistribucion`).
8. (S) Inserta un registro en `DistribucionAlimento` por barco, con `FormulaAplicada` documentando el cálculo usado.
9. (S) Redirige a `Index` mostrando el reparto resultante.

### Side doors
- Consultar reparto histórico ya hecho desde `Index` sin pasar por `Distribuir` (`GET /DistribucionAlimento`).
- Reporte agregado de carnada por tipo/periodo (`GET /Reporte/Carnada` y su export CSV) como vista alterna del mismo dato.

### Comportamientos erráticos
- Cantidad total ≤ 0 → `TempData["Error"]`, se recarga el formulario con los combos intactos, no se pierde el corral/alimento ya elegidos.
- No hay barcos activos → mensaje explícito ("Da de alta al menos uno en el módulo de Barcos") en lugar de un reparto vacío o una excepción.
- **Riesgo documentado en el propio código:** la fórmula de reparto proporcional es un *placeholder* — el cliente (Arian) aún no confirma la fórmula real de distribución de carnada (pendiente en `Docs/Fase0...` y en la Parte 4.1 del documento maestro).

### Respuesta de la arquitectura
`CalcularDistribucion` es una función pura, aislada del resto del controller: recibe el total y la lista de barcos activos, y devuelve el reparto sin tocar la base de datos. Esto significa que cuando el cliente confirme la fórmula real, el cambio se aísla a esa única función — no hay que tocar la validación, la carga de combos ni el `INSERT` por barco. El campo `FormulaAplicada` en cada registro deja trazabilidad de qué regla se usó para cada distribución histórica, así que un cambio de fórmula no vuelve ambiguos los registros ya guardados.

---

## Flujo 3 — Consulta al Asistente de IA (Text-to-SQL)

**Módulo:** `HomeController.ConsultarAsistente` + `TextToSqlService` (Ollama) · **Actor:** Cualquier operador sin conocimientos de SQL

### Ruta feliz
1. (U) Escribe una pregunta en lenguaje natural en el widget del Dashboard.
2. (U) Envía (`POST /Home/ConsultarAsistente`, AJAX).
3. (S) Valida que el mensaje no esté vacío.
4. (S) `TextToSqlService.GenerarConsultaSqlAsync` arma el prompt con el esquema de las 11 tablas y llama a Ollama (`qwen2.5-coder:7b`).
5. (S) Ollama regresa el SQL generado; se limpia de markdown residual.
6. (S) `ValidarSoloLectura` verifica que no contenga `DELETE/DROP/UPDATE/INSERT/TRUNCATE/ALTER/EXEC`.
7. (S) Ejecuta el `SELECT` contra la base real.
8. (S) Regresa JSON con el SQL interpretado + los datos, se renderizan en el widget.

### Side doors
- Ninguno formal: es el único punto de entrada al módulo de IA en el sitemap actual.

### Comportamientos erráticos
- Ollama no está corriendo (`ollama serve` apagado) → excepción controlada con mensaje explícito de diagnóstico ("Verifica que esté corriendo... y que el modelo esté descargado").
- El modelo "alucina" y genera un `DROP TABLE` o similar → `ValidarSoloLectura` lo bloquea **antes** de tocar la base de datos, lanzando excepción con mensaje "Por seguridad solo se permiten consultas de lectura".
- El modelo genera SQL sintácticamente inválido (pasa el filtro de solo-lectura pero no es SQL válido) → **hueco actual**: el error de SQL Server se propagaría tal cual hasta el `catch` genérico del action; falta un mensaje amigable específico para este caso (ver Backlog).
- El SQL generado consulta columnas que no existen en el esquema real → mismo hueco que el punto anterior.

### Respuesta de la arquitectura
La validación de solo-lectura es una segunda capa de defensa completamente independiente del modelo de IA: no importa qué tan "convencido" esté Ollama de que debe modificar datos, `ValidarSoloLectura` corre en `EjecutarConsultaAsync` sobre el string final, no sobre la intención del modelo. Aun si en el futuro se cambia de modelo o de proveedor de IA, esta capa sigue bloqueando cualquier instrucción destructiva sin depender de que el modelo "se porte bien" — es la mitigación de riesgo que ya identifica el documento maestro (Parte 1.3).

---

## Backlog técnico detectado al construir esta fase
(para ti, Carlos — no forma parte de la rúbrica de AI, pero salió del análisis)

- [ ] Agregar `RangeAttribute`/validación de rango físico en `Models/MuestreoAgua.cs` (PH, temperatura, etc.).
- [ ] Envolver el `SELECT` del Asistente IA en un `try/catch` específico que traduzca errores de sintaxis SQL a un mensaje amigable ("no pude interpretar esa pregunta, intenta reformularla") en lugar de dejar pasar el error crudo de SQL Server.
