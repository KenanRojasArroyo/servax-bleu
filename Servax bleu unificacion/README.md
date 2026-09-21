# Servax Bleu - Sistema de Gestión Acuícola e Inteligencia Artificial (Text-to-SQL)

El presente repositorio aloja el código y la documentación del proyecto desarrollado para **Servax Bleu**, una granja acuícola dedicada a la engorda de atún ubicada en Ensenada, Baja California. Este sistema fue conceptualizado y desarrollado como parte de la materia de Tecnologías Emergentes para el Desarrollo de Soluciones en la Universidad Autónoma del Estado de Baja California (UABC).

---

## Contexto y Problemática

Tras una investigación de campo y entrevistas con el personal operativo (incluyendo a la Supervisora de Operaciones de Granja y personal de Control de Calidad), se identificó un grave problema de ineficiencia operativa causado por la gestión manual de datos.

Actualmente, la información vital de la granja se encuentra fragmentada en múltiples sistemas, archivos y registros de Excel. Los trabajadores se ven obligados a realizar capturas repetitivas y trasladar manualmente métricas provenientes de sensores y otros sistemas de monitoreo. Esto provoca que actividades críticas, como la conciliación de inventarios y el seguimiento de datos relacionados con la alimentación, corrales, crecimiento, mortalidad y calidad del agua, consuman una parte considerable de la jornada laboral.

Además, la falta de centralización dificulta la consulta de información histórica, la generación de reportes integrales para vincular distintas áreas (por ejemplo, comparar la alimentación con la mortalidad) y la rápida detección de irregularidades.

---

## Propuesta de Solución y MVP

Para resolver esta problemática de manera definitiva, el proyecto propone una aplicación de escritorio conectada a una base de datos relacional local que automatiza el flujo de información y sustituye la captura tradicional por un flujo digital automatizado. El Producto Mínimo Viable (MVP) que entrega este repositorio se compone de:

1. **Módulo de Inventario y Gestión:** Un sistema de registro y consulta (CRUD) que unifica la información de operaciones de granja y control de calidad en una sola plataforma, reduciendo drásticamente el margen de error humano.
2. **Dashboard Operativo:** Interfaces gráficas que permiten visualizar métricas, indicadores clave mediante historiales gráficos por periodos y alertas automáticas para detectar anomalías.
3. **Módulo Text-to-SQL (Inteligencia Artificial):** El diferenciador principal del sistema. Consiste en un asistente integrado mediante tecnología emergente que permite a cualquier operador sin conocimientos de programación realizar consultas en lenguaje natural. La IA (ejecutada localmente) traduce peticiones cotidianas a código SQL, ejecutando la consulta en la base de datos y retornando un reporte estructurado de manera instantánea.

---

## Arquitectura y Diseño Centrado en el Humano

La construcción del sistema no es solo técnica, sino que está profundamente respaldada por metodologías de Diseño Centrado en el Humano y Arquitectura de la Información:

* **Investigación de Usuarios:** Se mapearon las necesidades ocultas del personal mediante entrevistas y el método del "Iceberg", determinando que el sistema debe priorizar flujos ágiles que absorban las frustraciones de las actividades manuales previas.
* **Arquitectura de la Información:** Se desarrollaron modelos Entidad-Relación (ERD) robustos para garantizar la integridad de los registros, acompañados de *Task Flows* que previenen el colapso ante errores y una estructura visual basada en la regla de la *Pirámide Invertida*, colocando la información más crítica (quién, qué, cuándo y dónde) en la zona de mayor visibilidad.

---

## Stack Tecnológico

* **Frontend e Interfaz:** Aplicación de escritorio desarrollada mediante entornos visuales en C# con .NET, priorizando tiempos de carga mínimos y curvas de aprendizaje cortas.
* **Backend y Persistencia de Datos:** Lógica de negocio estructurada en arquitectura cliente por capas. Base de datos local gestionada en SQL Server.
* **Tecnología Emergente (Text-to-SQL):** Integración de IA de forma estrictamente local mediante **Ollama** Específicamente, se utiliza el modelo **qwen2.5-coder:7b**, una variante optimizada para la generación y comprensión de código (incluyendo SQL). Esto garantiza la privacidad de los datos operativos de la granja y elimina la dependencia de APIs externas pagas.

---

## Requisitos del Sistema y Ejecución

Para desplegar y compilar este proyecto, se requiere lo siguiente:

* **Hardware (Desarrollo):** Equipos de cómputo con procesadores multi-núcleo y 16 GB de RAM para soportar la ejecución del modelo de IA local.
* **Entorno de Producción:** Computadoras de escritorio o laptops en entorno Windows, sin necesidad de infraestructura de servidores costosa.
* **Dependencias Externas:** Instalación de Ollama en el equipo para gestionar el modelo local.

```bash
# 1. Iniciar el motor de Inteligencia Artificial local
# Asegúrate de tener Ollama instalado en tu sistema y ejecuta:
ollama run qwen2.5-coder:7b

# 2. Clonar el repositorio del sistema
git clone [https://github.com/KenanRojasArroyo/servax-bleu.git](https://github.com/KenanRojasArroyo/servax-bleu.git)

# 3. Configuración e inicio:
# - Abrir la solución en Visual Studio.
# - Restaurar los paquetes NuGet requeridos.
# - Configurar la cadena de conexión a tu base de datos local en App.config o appsettings.json.
# - Ejecutar los scripts SQL proporcionados para la creación de tablas.
# - Asegurarse de que el puerto local de Ollama (por defecto 11434) esté accesible para la aplicación.
# - Compilar y ejecutar el proyecto (F5).