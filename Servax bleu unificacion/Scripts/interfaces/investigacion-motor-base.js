/**
 * investigacion-motor-base.js
 * ---------------------------------------------------------------
 * Factory genérica de CRUD + persistencia (localStorage) + exportación
 * JSON/CSV, reutilizada por los motores de las Interfaces 1, 2 y 3
 * (Entrevista a Expertos, Usuarios Extremos, Observación Directa).
 *
 * Interfaz 4 (Empathy Map) e Interfaz 5 (Roper Dynagram) NO usan esta
 * base porque su estructura no es una lista plana de registros
 * (una es una cuadrícula, la otra es una rueda con recálculo) — tienen
 * su propio motor (empathy-map-engine.js, roper-dynagram-engine.js),
 * igual que requisitos-engine.js de la Interfaz 6.
 * ---------------------------------------------------------------
 */

function crearMotorInvestigacion(storageKey, camposCsv = []) {
    let registros = [];
    let siguienteId = 1;

    function guardar() {
        try {
            localStorage.setItem(storageKey, JSON.stringify({ registros, siguienteId }));
        } catch (e) {
            console.error(`No se pudo guardar ${storageKey} en localStorage:`, e);
        }
    }

    function cargar() {
        try {
            const crudo = localStorage.getItem(storageKey);
            if (crudo) {
                const parsed = JSON.parse(crudo);
                registros = parsed.registros || [];
                siguienteId = parsed.siguienteId || 1;
            }
        } catch (e) {
            console.error(`No se pudo leer ${storageKey}, se usa estado vacío:`, e);
        }
    }

    function crear(datos) {
        const nuevo = { id: siguienteId++, fecha: new Date().toISOString(), ...datos };
        registros.push(nuevo);
        guardar();
        return nuevo;
    }

    function editar(id, cambios) {
        const registro = obtener(id);
        if (!registro) return null;
        Object.assign(registro, cambios);
        guardar();
        return registro;
    }

    function eliminar(id) {
        registros = registros.filter(r => r.id !== id);
        guardar();
    }

    function obtener(id) {
        return registros.find(r => r.id === id) || null;
    }

    function listar() {
        return [...registros];
    }

    function exportarJSON() {
        return JSON.stringify(registros, null, 2);
    }

    function exportarCSV() {
        const encabezados = camposCsv.length ? camposCsv : Object.keys(registros[0] || {});
        const filas = [encabezados.join(",")];
        registros.forEach(r => {
            filas.push(encabezados.map(campo => {
                let valor = r[campo];
                if (Array.isArray(valor)) valor = valor.join(" | ");
                if (typeof valor === "object" && valor !== null) valor = JSON.stringify(valor);
                valor = (valor ?? "").toString().replace(/"/g, '""');
                return `"${valor}"`;
            }).join(","));
        });
        return filas.join("\n");
    }

    function descargarArchivo(nombre, contenido, tipoMime) {
        const blob = new Blob([contenido], { type: tipoMime });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = nombre;
        a.click();
        URL.revokeObjectURL(url);
    }

    function exportarJSONArchivo(nombreArchivo) {
        descargarArchivo(nombreArchivo || `${storageKey}.json`, exportarJSON(), "application/json");
    }

    function exportarCSVArchivo(nombreArchivo) {
        descargarArchivo(nombreArchivo || `${storageKey}.csv`, exportarCSV(), "text/csv");
    }

    cargar();

    return {
        crear, editar, eliminar, obtener, listar,
        exportarJSON, exportarCSV, exportarJSONArchivo, exportarCSVArchivo
    };
}
