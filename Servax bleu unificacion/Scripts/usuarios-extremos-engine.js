/**
 * usuarios-extremos-engine.js — Interfaz 2 (Observación de Usuarios Extremos)
 *
 * REESCRITURA (sept. 2026): la versión anterior clasificaba con
 * "Poder"/"Limitación" y NO tenía nivel de habilidad, tareas observadas,
 * errores amplificados, ni evidencia como campos propios. La rúbrica pide:
 *   - Selector de clasificación: Súper-experto / Inexperto / Mainstream.
 *   - Nivel de habilidad (escala 1-10).
 *   - Tareas observadas.
 *   - Workarounds (adaptaciones manuales).
 *   - Errores amplificados.
 *   - Necesidad extrema + HIPÓTESIS EXPLÍCITA de generalización al
 *     usuario promedio (dos campos, no uno).
 *   - Evidencia (links, fotos, marcas de tiempo).
 *
 * Requiere que investigacion-motor-base.js esté cargado antes que este archivo.
 */

const MotorUsuariosExtremos = (function () {
    const STORAGE_KEY = "servaxbleu_usuarios_extremos_v2";
    const motor = crearMotorInvestigacion(STORAGE_KEY, [
        "id", "alias", "clasificacion", "contexto", "nivelHabilidad",
        "necesidadExtrema", "hipotesisGeneralizacion", "fuente", "fecha"
    ]);

    const CLASIFICACIONES = ["Súper-experto", "Inexperto", "Mainstream"];

    /**
     * datos = {
     *   alias, clasificacion: uno de CLASIFICACIONES, contexto,
     *   nivelHabilidad (1-10),
     *   tareasObservadas: [texto, ...],
     *   workarounds: [texto, ...],
     *   erroresAmplificados: [texto, ...],
     *   necesidadExtrema, hipotesisGeneralizacion,
     *   evidencia: [texto, ...]  (links, fotos, timestamps, testimonios),
     *   fuente (entrevista/observación), notas
     * }
     */
    function crearPerfil(datos) {
        if (datos.clasificacion && !CLASIFICACIONES.includes(datos.clasificacion)) {
            throw new Error(`Clasificación inválida. Debe ser una de: ${CLASIFICACIONES.join(", ")}`);
        }
        return motor.crear({
            tareasObservadas: [], workarounds: [], erroresAmplificados: [], evidencia: [],
            nivelHabilidad: 5,
            ...datos
        });
    }

    function agregarATexto(id, campo, texto) {
        const p = motor.obtener(id);
        if (!p) return null;
        p[campo].push(texto);
        motor.editar(id, p);
        return p;
    }

    const agregarTareaObservada = (id, t) => agregarATexto(id, "tareasObservadas", t);
    const agregarWorkaround = (id, t) => agregarATexto(id, "workarounds", t);
    const agregarErrorAmplificado = (id, t) => agregarATexto(id, "erroresAmplificados", t);
    const agregarEvidencia = (id, t) => agregarATexto(id, "evidencia", t);

    function resumenGeneral() {
        const r = motor.listar();
        return {
            total: r.length,
            tieneSuperExperto: r.some(p => p.clasificacion === "Súper-experto"),
            tieneInexperto: r.some(p => p.clasificacion === "Inexperto"),
        };
    }

    /** Precarga el perfil real del Encargado de Corrales (usuario extremo súper-experto). */
    function seedSiVacio() {
        if (motor.listar().length > 0) return;
        const p = crearPerfil({
            alias: "Encargado de Corrales y Control de Datos (aplica también a Administrador de Barcos)",
            clasificacion: "Súper-experto",
            contexto: "Uso diario, continuo y bajo presión. Alterna entre una PC vieja (conectada a sensores) y una PC nueva (para reportes), en las instalaciones de la granja.",
            nivelHabilidad: 10,
            necesidadExtrema: "Eliminación total de la ingesta manual (conexión directa de sensores a base de datos) y un sistema de alertas proactivas que avise de irregularidades sin tener que generar la gráfica primero.",
            hipotesisGeneralizacion: "Si Servax Bleu automatiza el flujo para este usuario extremo (soportando picos de datos por segundo sin intervención humana), el sistema será suficientemente robusto e invisible para que el usuario promedio (dueño, jefe general) simplemente abra la plataforma y vea un dashboard en tiempo real, libre de retrasos operativos.",
            fuente: "Entrevista + observación de campo directa (sept. 2026)",
            notas: "Su eficiencia real está topada en 4/10 pese a un dominio de 10/10, por las limitaciones del software legacy que lo obliga a trabajar manualmente."
        });

        [
            "Monitoreo del programa independiente que recibe lecturas de los sensores de los corrales.",
            "Transcripción manual y exportación de datos de biomasa, mortalidad y alimentación hacia hojas de cálculo.",
            "Cálculo matemático manual en Excel de la capacidad de los barcos para transporte de carnada.",
            "Generación de gráficas y reportes históricos para dueños y jefes de área."
        ].forEach(t => agregarTareaObservada(p.id, t));

        [
            "El \"puente humano\": mueve información físicamente o transcribe de la PC vieja (sensores) a la PC nueva porque no se comunican entre sí.",
            "Calculadora improvisada: fórmulas de Excel hechas a la medida para suplir la falta de un módulo logístico de capacidad de barcos."
        ].forEach(t => agregarWorkaround(p.id, t));

        [
            "Colapso por \"picos de datos\": los sensores capturan por segundo; en picos, la capacidad humana de transcripción se satura y retrasa el análisis de corrales.",
            "El trabajo manual prolongado aumenta el riesgo de errores de \"dedo\" al transcribir métricas críticas de biomasa."
        ].forEach(t => agregarErrorAmplificado(p.id, t));

        [
            "Testimonio directo: \"Se capturan datos por segundo [...] y después esos datos se exportan a Excel manualmente\".",
            "Testimonio directo: \"Cuando el sistema empieza a detectar demasiados datos hay que transcribirlos de manera manual y toma más tiempo [...] lo cual nos retrasa en el trabajo\".",
            "Observación de campo: demostración física de las tablas de Excel, el programa aislado en la PC vieja y las fórmulas manuales de capacidad de los barcos.",
            "Restricción técnica confirmada: imposibilidad de conectar directamente a la base de datos real por políticas de privacidad del área de Sistemas del cliente."
        ].forEach(t => agregarEvidencia(p.id, t));
    }

    return {
        CLASIFICACIONES,
        crearPerfil,
        editarPerfil: motor.editar,
        eliminarPerfil: motor.eliminar,
        obtenerPerfil: motor.obtener,
        listarPerfiles: motor.listar,
        agregarTareaObservada,
        agregarWorkaround,
        agregarErrorAmplificado,
        agregarEvidencia,
        resumenGeneral,
        seedSiVacio,
        exportarJSON: motor.exportarJSON,
        exportarCSV: motor.exportarCSV,
        exportarJSONArchivo: () => motor.exportarJSONArchivo("usuarios_extremos.json"),
        exportarCSVArchivo: () => motor.exportarCSVArchivo("usuarios_extremos.csv")
    };
})();
