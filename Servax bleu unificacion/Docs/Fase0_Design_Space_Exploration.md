# Fase 0 — Design Space Exploration

## Divergencia

### Necesidades obvias
1. **Centralizar la información de las tres áreas** para facilitar el intercambio de datos entre granja, control de calidad y alimentación. *(Fuente: entrevista con Arian Castillo Luhrs, Supervisora de Operaciones de Granja, 18 ago 2026)*
2. **Automatizar la generación de reportes y gráficas** utilizando la información almacenada por periodos. *(Fuente: entrevista con Arian Castillo Luhrs)*
3. **Integrar al sistema los datos obtenidos por sensores**, como temperatura, profundidad y oxígeno. *(Fuente: entrevista con Arian Castillo Luhrs)*
4. **Mantener un historial del inventario de los corrales** incluyendo especies, cantidad, estado y otros datos relacionados. *(Fuente: entrevista con Arian Castillo Luhrs)*
5. **Implementar alertas y notificaciones** para muestreos, chequeos semanales, irregularidades, picos de alerta y presencia de especies tóxicas. *(Fuente: entrevista con Arian Castillo Luhrs)*
6. **Automatizar la distribución de carnada entre los barcos**, considerando las toneladas disponibles y la capacidad de cada embarcación. *(Fuente: entrevista con Arian Castillo Luhrs)*
7. **Mostrar información actualizada en tiempo real**, incluyendo datos provenientes del sistema de monitoreo. *(Fuente: entrevista con Arian Castillo Luhrs)*
8. **Consultar el historial de crecimiento de los peces** durante el año para analizar su evolución. *(Fuente: entrevista con Arian Castillo Luhrs)*

> Nota: las 8 necesidades obvias vienen hoy de una sola entrevistada (la única entrevista real realizada hasta ahora). Conforme se agenden las 4 entrevistas de campo pendientes, conviene reetiquetar cada necesidad con la entrevista/usuario específico que la confirme o la amplíe — la guía del profesor pide trazabilidad a la fuente exacta, no solo "entrevista" en genérico.

---

### Necesidades ocultas
1. **Reducir errores provocados por la captura manual de información.**
   La empresa menciona que actualmente migran datos manualmente y que este proceso es tedioso. De ahí podemos inferir que automatizarlo también ayudaría a disminuir errores humanos.
   *(Fuente: inferencia a partir del proceso actual de migración manual de datos)*

2. **Tener una única fuente de información confiable.**
   Si diferentes áreas necesitan acceder a información de otras y actualmente existen archivos de Excel y datos en una computadora antigua, sería importante evitar que existan distintas versiones de la misma información.
   *(Fuente: inferencia a partir del uso de Excel, la computadora anterior y la necesidad de conectar áreas)*

3. **Detectar problemas antes de que afecten significativamente la operación.**
   La solicitud de alertas por irregularidades, calidad del agua, especies tóxicas y monitoreo mediante sensores indica una necesidad más profunda: detectar situaciones anormales rápidamente para poder actuar.
   *(Fuente: inferencia a partir de la solicitud de alertas y monitoreo)*

---

## Cross-pollination

### 1. Sistemas de acuicultura inteligente
Se tomó como referencia el funcionamiento de los sistemas de acuicultura basados en IoT, ya que utilizan sensores para obtener datos como temperatura, oxígeno y calidad del agua y enviarlos a un sistema para su monitoreo. De este modelo se toma la **arquitectura por capas**, separando la obtención de datos mediante sensores, su procesamiento y almacenamiento, y finalmente su visualización en el sistema.

* **¿Cómo lo aplicamos?**
  Los sensores que ya utiliza la empresa podrían alimentar la base de datos y permitir generar automáticamente gráficas, históricos y reportes.

### 2. Sistemas de monitoreo industrial
Se tomó como referencia el monitoreo utilizado en entornos industriales donde diferentes sensores recopilan información constantemente y esta se concentra en un dashboard. De este modelo se toma el **patrón de monitoreo en tiempo real basado en eventos**, permitiendo generar alertas cuando determinados valores presentan irregularidades.

* **¿Cómo lo aplicamos?**
  El sistema podría monitorear datos como temperatura, oxígeno y calidad del agua y generar alertas o notificaciones cuando se detecten valores fuera de los parámetros establecidos.

### 3. Sistemas de gestión empresarial
Se tomó como referencia la forma en que los sistemas empresariales integran diferentes áreas dentro de una misma plataforma. Para nuestro proyecto se propone utilizar una **arquitectura modular con información centralizada**, donde las áreas de granja, control de calidad y alimentación tengan sus propias funciones, pero comparten la información que necesitan.

* **¿Cómo lo aplicamos?**
  En lugar de que cada área maneje información por separado, las tres podrían consultar datos de una misma base de datos. Por ejemplo: alimentación podría utilizar información de los corrales mientras control de calidad consulta datos obtenidos por los sensores.

---

## Convergencia

Esta versión del sistema cubre: gestión de corrales y especies; inventario de peces por corral; muestreos de calidad de agua con alertas automáticas por correo cuando un valor cae fuera de umbral; historial de crecimiento anual por corral/especie; registro de alimentación y distribución de carnada entre barcos según su capacidad; reportes exportables de mortalidad y de tipos de carnada; un historial general por corral; y un asistente de IA que traduce preguntas en lenguaje natural a consultas SQL de solo lectura sobre estos datos.

Queda deliberadamente fuera de esta versión: aplicaciones móviles (el sistema es de escritorio/web, no hay app nativa); integración directa con los sensores físicos o el software existente de la granja (por ahora la captura es manual o vía importación de Excel, no en tiempo real desde hardware); pagos o facturación; y gestión de usuarios/roles con permisos diferenciados (hoy el sistema no distingue perfiles de acceso).

---

## Coordinar y ejecutar trabajo de campo

* Agendar mínimo 4 entrevistas asegurando que se cubra **1 usuario extremo** y **1 experto de dominio**.
  * 24 de septiembre de 12:00 a 1:00 PM
  * 01 de octubre de 12:00 a 1:00 PM
  * 15 de octubre de 12:00 a 1:00 PM
  * 29 de octubre de 12:00 a 1:00 PM
* Documentar las entrevistas.
* Resolver dudas en la entrevista.
