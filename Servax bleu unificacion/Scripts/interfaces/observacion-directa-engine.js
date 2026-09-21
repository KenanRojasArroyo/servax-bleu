/**
 * observacion-directa-engine.js — Interfaz 3 ("El Iceberg")
 *
 * Metáfora del iceberg: necesidades visibles (lo que la persona dice/hace,
 * "sobre el agua") vs. necesidades ocultas (lo que se infiere, "bajo el agua").
 * El examen pide mínimo 5 necesidades obvias y 3 ocultas con fuente — este
 * motor las modela como dos listas separadas por sesión de observación.
 *
 * Requiere que investigacion-motor-base.js esté cargado antes que este archivo.
 */

const MotorObservacionDirecta = (function () {
    const motor = crearMotorInvestigacion("servaxbleu_observacion_directa", [
        "id", "contexto", "fecha", "usuarioObservado"
    ]);

    /**
     * datos = { contexto, usuarioObservado, fecha }
     * Cada sesión arranca sin necesidades; se agregan con las funciones de abajo.
     */
    function crearSesion(datos) {
        return motor.crear({ necesidadesVisibles: [], necesidadesOcultas: [], ...datos });
    }

    /** necesidad = { texto, fuente } — fuente: qué dijo/hizo la persona que lo reveló */
    function agregarNecesidadVisible(idSesion, necesidad) {
        const sesion = motor.obtener(idSesion);
        if (!sesion) return null;
        sesion.necesidadesVisibles.push(necesidad);
        motor.editar(idSesion, sesion);
        return sesion;
    }

    /** necesidad = { texto, fuente, inferenciaDe } — inferenciaDe: de qué observación se dedujo */
    function agregarNecesidadOculta(idSesion, necesidad) {
        const sesion = motor.obtener(idSesion);
        if (!sesion) return null;
        sesion.necesidadesOcultas.push(necesidad);
        motor.editar(idSesion, sesion);
        return sesion;
    }

    /** Resumen agregado de todas las sesiones, útil para el documento de reflexión */
    function resumenGeneral() {
        const sesiones = motor.listar();
        return {
            totalSesiones: sesiones.length,
            totalNecesidadesVisibles: sesiones.reduce((acc, s) => acc + s.necesidadesVisibles.length, 0),
            totalNecesidadesOcultas: sesiones.reduce((acc, s) => acc + s.necesidadesOcultas.length, 0),
            cumpleMinimoExamen:
                sesiones.reduce((acc, s) => acc + s.necesidadesVisibles.length, 0) >= 5 &&
                sesiones.reduce((acc, s) => acc + s.necesidadesOcultas.length, 0) >= 3
        };
    }

    return {
        crearSesion,
        editarSesion: motor.editar,
        eliminarSesion: motor.eliminar,
        obtenerSesion: motor.obtener,
        listarSesiones: motor.listar,
        agregarNecesidadVisible,
        agregarNecesidadOculta,
        resumenGeneral,
        exportarJSON: motor.exportarJSON,
        exportarCSV: motor.exportarCSV,
        exportarJSONArchivo: () => motor.exportarJSONArchivo("observacion_directa.json"),
        exportarCSVArchivo: () => motor.exportarCSVArchivo("observacion_directa.csv")
    };
})();
