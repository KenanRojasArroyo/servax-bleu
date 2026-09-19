# Documento de Reflexión — Evolución del problema (Servax Bleu)

## 1. De qué partimos

La entrevista inicial con Arian Castillo Luhrs (Supervisora de Operaciones,
18 ago 2026) se levantó como un problema de **ineficiencia operativa por
dispersión de datos en Excel**: transcripción manual de sensores,
conciliación de inventarios y generación de reportes consumiendo una parte
crítica de la jornada. Esa fue la lectura de superficie, y sigue siendo
válida — pero al pasarla por el método del Iceberg (necesidades obvias vs.
ocultas) y por el ejercicio de Cross-Pollination, el problema se movió de
"les falta un sistema" a algo más específico.

## 2. Lo que cambió al separar lo obvio de lo oculto

Las 8 necesidades obvias documentadas en `Fase0_Design_Space_Exploration.md`
son, en esencia, una lista de features: centralizar datos, automatizar
reportes, integrar sensores, alertas, distribución de carnada. Si el equipo
se hubiera quedado ahí, el resultado habría sido un ERP genérico más.

Lo que cambia el enfoque son las 3 necesidades ocultas inferidas del mismo
material:

1. **Reducir errores de captura manual** — no está pedido explícitamente,
   pero se infiere de que Arian describe la migración de datos como
   "tediosa"; el problema real no es "no tener un sistema", es que el
   sistema actual (Excel + transcripción) **introduce error humano en cada
   paso**.
2. **Una única fuente de verdad** — inferida del uso simultáneo de Excel y
   una computadora antigua; el riesgo oculto no es la falta de reportes,
   es que puedan existir dos versiones distintas del mismo dato circulando
   al mismo tiempo.
3. **Detectar antes de que escale** — inferida de la insistencia en alertas
   por calidad de agua, especies tóxicas y sensores; lo que Arian pide no es
   "ver los datos", es **enterarse a tiempo** de algo antes de perder
   biomasa.

Esta relectura es la que justificó, técnicamente, decisiones concretas del
sistema que no estaban en la lista original de features:
- El disparo automático de correo en `ServicioNotificaciones` cuando
  `MuestreoAgua` sale de umbral (necesidad oculta #3) — no es un reporte, es
  una alerta activa.
- Que `HistorialCorral` exista como tabla propia en vez de solo dejar que el
  usuario cruce manualmente `MuestreoAgua` + `RegistroAlimentacion` —
  intento de resolver la necesidad oculta #2 (una sola versión consolidada
  por corral).
- Que el Asistente de IA (Text-to-SQL) se restrinja a solo-lectura: no es
  una limitación técnica arbitraria, es una respuesta directa a la
  necesidad oculta #1 — si el problema de fondo es el error humano en la
  captura, no tiene sentido introducir un canal de IA que también pueda
  escribir mal un dato.

## 3. Dónde el problema sigue abierto

El ejercicio también dejó visibles los límites de lo que sabemos hoy:

- Las 8 necesidades obvias y las 3 ocultas vienen **todas de una sola
  entrevistada**. Es un punto de partida válido, pero el propio documento
  de Fase 0 lo marca como una advertencia: sin las 3 entrevistas/
  observaciones de campo pendientes (incluyendo 1 usuario extremo y 1
  experto de dominio distinto a Arian), no se puede confirmar si estas
  necesidades se generalizan al resto del personal o son específicas de su
  rol de supervisión.
- La fórmula de distribución de carnada, el catálogo de especies tóxicas y
  el protocolo real de lectura de sensores siguen sin confirmar con el
  cliente — el sistema hoy corre con supuestos razonables mas no
  validados (ver comentario explícito en `DistribucionAlimentoController`).
- Cross-Pollination tomó prestados patrones de IoT industrial y sistemas
  empresariales modulares, pero ninguno de esos análogos resuelve
  directamente el problema de **confianza en un modelo de IA local** que
  traduce lenguaje natural a SQL — esa parte del diseño (validación de
  solo-lectura como segunda capa de defensa) no vino de ningún caso
  análogo, fue una decisión propia del equipo ante un riesgo que no existía
  en los referentes usados.

## 4. Conclusión

El problema evolucionó de "digitalizar Excel" a "reducir el costo del error
humano en tres puntos distintos del flujo: captura, consolidación y
detección de irregularidades". El sistema que existe hoy en el repositorio
ya refleja esa relectura en decisiones concretas de arquitectura, pero la
validación de que esa relectura es correcta para *todo* el personal —no
solo para la supervisora— sigue pendiente del trabajo de campo restante.
