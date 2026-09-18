/**
 * entrevista-expertos-engine.js — Interfaz 1
 *
 * Campos según la rúbrica del examen: alias, rol, dominio, años de
 * experiencia, guion pregunta→respuesta con citas clave, mapa de
 * complejidad técnica, restricciones/riesgos, referencias, notas.
 *
 * Requiere que investigacion-motor-base.js esté cargado antes que este archivo.
 */

const MotorEntrevistaExpertos = (function () {
    const motor = crearMotorInvestigacion("servaxbleu_entrevistas_expertos", [
        "id", "alias", "rol", "dominio", "aniosExperiencia",
        "mapaComplejidadTecnica", "restriccionesRiesgos", "referencias", "notas", "fecha"
    ]);

    /**
     * datos = {
     *   alias, rol, dominio, aniosExperiencia,
     *   guion: [{ pregunta, respuesta, citaClave }],
     *   mapaComplejidadTecnica, restriccionesRiesgos,
     *   referencias: [texto, ...], notas
     * }
     */
    function crearEntrevista(datos) {
        return motor.crear({ guion: [], referencias: [], ...datos });
    }

    function agregarPreguntaRespuesta(idEntrevista, { pregunta, respuesta, citaClave = "" }) {
        const entrevista = motor.obtener(idEntrevista);
        if (!entrevista) return null;
        entrevista.guion.push({ pregunta, respuesta, citaClave });
        motor.editar(idEntrevista, entrevista);
        return entrevista;
    }

    function agregarReferencia(idEntrevista, referencia) {
        const entrevista = motor.obtener(idEntrevista);
        if (!entrevista) return null;
        entrevista.referencias.push(referencia);
        motor.editar(idEntrevista, entrevista);
        return entrevista;
    }

    return {
        crearEntrevista,
        editarEntrevista: motor.editar,
        eliminarEntrevista: motor.eliminar,
        obtenerEntrevista: motor.obtener,
        listarEntrevistas: motor.listar,
        agregarPreguntaRespuesta,
        agregarReferencia,
        exportarJSON: motor.exportarJSON,
        exportarCSV: motor.exportarCSV,
        exportarJSONArchivo: () => motor.exportarJSONArchivo("entrevistas_expertos.json"),
        exportarCSVArchivo: () => motor.exportarCSVArchivo("entrevistas_expertos.csv")
    };
})();
