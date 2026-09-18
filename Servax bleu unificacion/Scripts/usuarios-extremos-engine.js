/**
 * usuarios-extremos-engine.js — Interfaz 2
 *
 * Perfil de usuario extremo (poder o limitación) — necesario para cubrir
 * el requisito del examen de incluir explícitamente 1 usuario extremo
 * en el trabajo de campo.
 *
 * Requiere que investigacion-motor-base.js esté cargado antes que este archivo.
 */

const MotorUsuariosExtremos = (function () {
    const motor = crearMotorInvestigacion("servaxbleu_usuarios_extremos", [
        "id", "alias", "tipoExtremo", "contexto", "fuente", "fecha"
    ]);

    /**
     * datos = {
     *   alias, tipoExtremo: "Poder" | "Limitación",
     *   contexto (situación/rol de la persona),
     *   necesidadesExtremas: [texto, ...],
     *   comportamientosAtipicos: [texto, ...],
     *   fuente (entrevista/observación), notas
     * }
     */
    function crearPerfil(datos) {
        return motor.crear({ necesidadesExtremas: [], comportamientosAtipicos: [], ...datos });
    }

    function agregarNecesidadExtrema(id, texto) {
        const perfil = motor.obtener(id);
        if (!perfil) return null;
        perfil.necesidadesExtremas.push(texto);
        motor.editar(id, perfil);
        return perfil;
    }

    function agregarComportamientoAtipico(id, texto) {
        const perfil = motor.obtener(id);
        if (!perfil) return null;
        perfil.comportamientosAtipicos.push(texto);
        motor.editar(id, perfil);
        return perfil;
    }

    return {
        crearPerfil,
        editarPerfil: motor.editar,
        eliminarPerfil: motor.eliminar,
        obtenerPerfil: motor.obtener,
        listarPerfiles: motor.listar,
        agregarNecesidadExtrema,
        agregarComportamientoAtipico,
        exportarJSON: motor.exportarJSON,
        exportarCSV: motor.exportarCSV,
        exportarJSONArchivo: () => motor.exportarJSONArchivo("usuarios_extremos.json"),
        exportarCSVArchivo: () => motor.exportarCSVArchivo("usuarios_extremos.csv")
    };
})();
