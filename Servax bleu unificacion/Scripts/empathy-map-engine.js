/**
 * empathy-map-engine.js — Interfaz 4 ("The Parser")
 *
 * Cuadrícula obligatoria del Empathy Map: Dice / Piensa / Hace / Siente,
 * por usuario/sesión de investigación. No usa el motor base genérico
 * porque la estructura es una cuadrícula de 4 categorías, no una lista plana.
 */

const MotorEmpathyMap = (function () {
    const STORAGE_KEY = "servaxbleu_empathy_map";
    let mapas = [];
    let siguienteId = 1;

    function guardar() {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify({ mapas, siguienteId })); }
        catch (e) { console.error("No se pudo guardar el Empathy Map:", e); }
    }

    function cargar() {
        try {
            const crudo = localStorage.getItem(STORAGE_KEY);
            if (crudo) {
                const parsed = JSON.parse(crudo);
                mapas = parsed.mapas || [];
                siguienteId = parsed.siguienteId || 1;
            }
        } catch (e) { console.error("No se pudo leer el Empathy Map, se usa estado vacío:", e); }
    }

    /** perfil: alias/rol de la persona a la que le estamos haciendo el mapa */
    function crearMapa({ alias, contexto = "", fecha = new Date().toISOString() }) {
        const nuevo = {
            id: siguienteId++,
            alias,
            contexto,
            fecha,
            dice: [],
            piensa: [],
            hace: [],
            siente: []
        };
        mapas.push(nuevo);
        guardar();
        return nuevo;
    }

    const CUADRANTES = ["dice", "piensa", "hace", "siente"];

    function agregarItem(idMapa, cuadrante, texto) {
        if (!CUADRANTES.includes(cuadrante)) {
            throw new Error(`Cuadrante inválido: ${cuadrante}. Debe ser uno de ${CUADRANTES.join(", ")}`);
        }
        const mapa = obtenerMapa(idMapa);
        if (!mapa) return null;
        mapa[cuadrante].push(texto);
        guardar();
        return mapa;
    }

    function quitarItem(idMapa, cuadrante, indice) {
        const mapa = obtenerMapa(idMapa);
        if (!mapa || !CUADRANTES.includes(cuadrante)) return null;
        mapa[cuadrante].splice(indice, 1);
        guardar();
        return mapa;
    }

    function obtenerMapa(id) {
        return mapas.find(m => m.id === id) || null;
    }

    function listarMapas() {
        return [...mapas];
    }

    function eliminarMapa(id) {
        mapas = mapas.filter(m => m.id !== id);
        guardar();
    }

    function exportarJSON() {
        return JSON.stringify(mapas, null, 2);
    }

    function exportarCSV() {
        // Una fila por item de cualquier cuadrante, para que sea tabular.
        const filas = [["Alias", "Cuadrante", "Item"]];
        mapas.forEach(m => {
            CUADRANTES.forEach(c => {
                m[c].forEach(item => {
                    filas.push([m.alias, c, `"${item.replace(/"/g, '""')}"`]);
                });
            });
        });
        return filas.map(f => f.join(",")).join("\n");
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

    cargar();

    return {
        crearMapa, agregarItem, quitarItem, obtenerMapa, listarMapas, eliminarMapa,
        exportarJSON, exportarCSV,
        exportarJSONArchivo: () => descargarArchivo("empathy_map.json", exportarJSON(), "application/json"),
        exportarCSVArchivo: () => descargarArchivo("empathy_map.csv", exportarCSV(), "text/csv")
    };
})();
