/**
 * entrevista-expertos-engine.js — Interfaz 1 (Entrevista a Expertos)
 *
 * REESCRITURA (sept. 2026): la versión anterior solo tenía
 * alias/rol/dominio/aniosExperiencia + un campo de texto libre para el
 * mapa de complejidad técnica. La rúbrica pide explícitamente:
 *   - Perfil del experto: alias, rol, dominio, años de experiencia,
 *     ORGANIZACIÓN, fecha, MEDIO (presencial/remoto).
 *   - Guion dinámico: pares pregunta→respuesta con checkbox de
 *     "cita textual clave" (no solo un texto).
 *   - Mapa de complejidad técnica ESTRUCTURADO en 4 sub-campos:
 *     conceptos, jerga, dependencias, actores (no un único textarea).
 *   - Restricciones/riesgos como lista, no un párrafo.
 *   - Referencias y notas.
 *
 * Requiere que investigacion-motor-base.js esté cargado antes que este archivo.
 */

const MotorEntrevistaExpertos = (function () {
    const STORAGE_KEY = "servaxbleu_entrevistas_expertos_v2";
    const motor = crearMotorInvestigacion(STORAGE_KEY, [
        "id", "alias", "rol", "dominio", "organizacion", "aniosExperiencia",
        "medio", "fecha", "restriccionesRiesgos", "referencias", "notas"
    ]);

    /**
     * datos = {
     *   alias, rol, dominio, organizacion, aniosExperiencia,
     *   medio: "Presencial" | "Remoto",
     *   guion: [{ pregunta, respuesta, citaClave: bool }],
     *   mapaComplejidadTecnica: { conceptos, jerga, dependencias, actores },
     *   restriccionesRiesgos: [texto, ...],
     *   referencias: [texto, ...],
     *   notas
     * }
     */
    function crearEntrevista(datos) {
        return motor.crear({
            guion: [],
            mapaComplejidadTecnica: { conceptos: "", jerga: "", dependencias: "", actores: "" },
            restriccionesRiesgos: [],
            referencias: [],
            ...datos
        });
    }

    function agregarPreguntaRespuesta(idEntrevista, { pregunta, respuesta, citaClave = false }) {
        const e = motor.obtener(idEntrevista);
        if (!e) return null;
        e.guion.push({ pregunta, respuesta, citaClave: !!citaClave });
        motor.editar(idEntrevista, e);
        return e;
    }

    function actualizarMapaComplejidad(idEntrevista, campo, valor) {
        const e = motor.obtener(idEntrevista);
        if (!e) return null;
        e.mapaComplejidadTecnica[campo] = valor;
        motor.editar(idEntrevista, e);
        return e;
    }

    function agregarRestriccionRiesgo(idEntrevista, texto) {
        const e = motor.obtener(idEntrevista);
        if (!e) return null;
        e.restriccionesRiesgos.push(texto);
        motor.editar(idEntrevista, e);
        return e;
    }

    function agregarReferencia(idEntrevista, referencia) {
        const e = motor.obtener(idEntrevista);
        if (!e) return null;
        e.referencias.push(referencia);
        motor.editar(idEntrevista, e);
        return e;
    }

    function resumenGeneral() {
        const registros = motor.listar();
        return {
            totalEntrevistas: registros.length,
            totalCitasClave: registros.reduce((acc, r) => acc + r.guion.filter(g => g.citaClave).length, 0)
        };
    }

    /** Precarga la entrevista real con Arian Castillo Luhrs si el storage está vacío. */
    function seedSiVacio() {
        if (motor.listar().length > 0) return;
        const e = crearEntrevista({
            alias: "Arian Castillo Luhrs",
            rol: "Supervisora de Operaciones de Granja",
            dominio: "Operaciones de engorda de atún / captura de datos de corrales",
            organizacion: "Servax Bleu",
            aniosExperiencia: 9,
            medio: "Presencial",
            fecha: "2026-09-18",
            notas: "Trabajo de clase Ambientes de Programación Visual 2026-2. Generar la BD junto con encargados de área; primeros bocetos de frameworks; interconectar áreas; validar privacidad antes de tocar datos reales."
        });
        [
            ["¿Quién más necesitaría entrar al sistema (dueños, personal de campo, TI)? ¿Qué debería ver o hacer cada uno?",
                "Es necesario el acceso para los encargados de control de calidad y supervisión de corrales, administrador de barcos y productos, jefes de área general y dueños de la empresa.", false],
            ["¿Podrías compartirnos un ejemplo real de cómo capturan hoy el inventario y los datos de biomasa?",
                "Sí (nos mostró tablas de Excel y el programa que utilizan). Tienen un sistema ambiguo y poco efectivo en cuestión de ahorro de tiempo.", true],
            ["¿Cuántas veces al día se captura información y hay picos donde el llenado manual cuesta más?",
                "Se capturan datos por segundo dentro del programa conectado a los sensores y después esos datos se exportan a Excel manualmente para generar reportes. Cuando el sistema detecta demasiados datos hay que transcribirlos a mano, lo cual retrasa el trabajo.", true],
            ["¿Con qué equipo cuentan (computadoras, tablets) y qué tan estable es el Wi-Fi?",
                "Se utilizan PCs entregadas por la empresa; hay una computadora más vieja que es la única con el programa de sensores, y esos datos se exportan a la PC más nueva. No hay problemas de conectividad de Wi-Fi.", false],
            ["Si el sistema funcionara perfectamente, ¿qué cambiaría en tu día a día?",
                "Que los datos no tuvieran que ser ingresados a mano uno por uno; que generara reportes, gráficas y picos de alertas ante irregularidades. También se usa una fórmula manual en Excel para la capacidad de carnada de cada barco que se podría calcular sola.", true]
        ].forEach(([pregunta, respuesta, citaClave]) => agregarPreguntaRespuesta(e.id, { pregunta, respuesta, citaClave }));

        actualizarMapaComplejidad(e.id, "conceptos", "Registros y reportes pasados; guion y mapa de complejidad; validación de privacidad; plataforma Servax Bleu; exportación ágil de datos.");
        actualizarMapaComplejidad(e.id, "jerga", "\"El sistema actual es batalloso\"; \"proceso muy ambiguo\"; picos de datos; capacidad de carnada.");
        actualizarMapaComplejidad(e.id, "dependencias", "Archivos Excel (sistema viejo) → almacenan registros y reportes pasados. Programa de sensores (PC vieja) → exporta a PC nueva para generar reporte estructurado. Entrevista en tiempo real → captura guion y mapa → llena Perfil/Guion/Restricciones → centraliza en la Plataforma Servax Bleu.");
        actualizarMapaComplejidad(e.id, "actores", "Área de Sistemas (cliente, dueño de las políticas de privacidad); Expertos de Granja (Operación, Calidad, Alimentación); Desarrolladores Servax Bleu.");

        [
            "Privacidad de los datos: no se puede acceder a los datos reales de la empresa por política; se trabajará con los encargados de cada área usando el prototipo para recibir observaciones.",
            "Conectividad con los datos extraídos del programa independiente que se usa junto con los sensores."
        ].forEach(r => agregarRestriccionRiesgo(e.id, r));

        [
            "Trabajo de clase de Ambientes de Programación Visual 2026-2 - Classroom (recuperado 18 sept. 2026).",
            "Datos/información presentada directamente en la entrevista con Arian Castillo Luhrs."
        ].forEach(r => agregarReferencia(e.id, r));
    }

    return {
        crearEntrevista,
        editarEntrevista: motor.editar,
        eliminarEntrevista: motor.eliminar,
        obtenerEntrevista: motor.obtener,
        listarEntrevistas: motor.listar,
        agregarPreguntaRespuesta,
        actualizarMapaComplejidad,
        agregarRestriccionRiesgo,
        agregarReferencia,
        resumenGeneral,
        seedSiVacio,
        exportarJSON: motor.exportarJSON,
        exportarCSV: motor.exportarCSV,
        exportarJSONArchivo: () => motor.exportarJSONArchivo("entrevistas_expertos.json"),
        exportarCSVArchivo: () => motor.exportarCSVArchivo("entrevistas_expertos.csv")
    };
})();
