/**
 * roper-dynagram-engine.js — Interfaz 5 (Roper Dynagram)
 *
 * SUPUESTO A CONFIRMAR: se modelan los 8 segmentos clásicos del Cultural
 * Dynagram de Elisa Roper (Economía, Político/Legal, Tecnología,
 * Religión/Valores, Educación, Sociedad/Familia, Estética/Arte, Recreación).
 * Si el profesor pide un set de segmentos distinto, solo hay que cambiar
 * el arreglo SEGMENTOS de abajo — el resto de la lógica no cambia.
 *
 * "Ley" de recálculo (visible/editable, igual que en requisitos-engine.js):
 * el nivel de cada segmento de la rueda = promedio de intensidad de los
 * insights vinculados a ese segmento. Sin insights vinculados, el segmento
 * arranca en 0 (vacío en la rueda, no hay evidencia todavía).
 */

const MotorRoperDynagram = (function () {
    const STORAGE_KEY = "servaxbleu_roper_dynagram";

    const SEGMENTOS = [
        "Economía", "Político/Legal", "Tecnología", "Religión/Valores",
        "Educación", "Sociedad/Familia", "Estética/Arte", "Recreación"
    ];

    let insights = [];
    let siguienteId = 1;

    function guardar() {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify({ insights, siguienteId })); }
        catch (e) { console.error("No se pudo guardar el Roper Dynagram:", e); }
    }

    function cargar() {
        try {
            const crudo = localStorage.getItem(STORAGE_KEY);
            if (crudo) {
                const parsed = JSON.parse(crudo);
                insights = parsed.insights || [];
                siguienteId = parsed.siguienteId || 1;
            }
        } catch (e) { console.error("No se pudo leer el Roper Dynagram, se usa estado vacío:", e); }
    }

    /** segmento debe ser uno de SEGMENTOS; intensidad 1-5 */
    function agregarInsight({ segmento, texto, intensidad = 3, fuente = "", fecha = new Date().toISOString() }) {
        if (!SEGMENTOS.includes(segmento)) {
            throw new Error(`Segmento inválido: ${segmento}. Debe ser uno de ${SEGMENTOS.join(", ")}`);
        }
        const nuevo = { id: siguienteId++, segmento, texto, intensidad: clamp(intensidad, 1, 5), fuente, fecha };
        insights.push(nuevo);
        guardar();
        return nuevo;
    }

    function editarInsight(id, cambios) {
        const insight = insights.find(i => i.id === id);
        if (!insight) return null;
        Object.assign(insight, cambios);
        guardar();
        return insight;
    }

    function eliminarInsight(id) {
        insights = insights.filter(i => i.id !== id);
        guardar();
    }

    function listarInsights(segmento = null) {
        return segmento ? insights.filter(i => i.segmento === segmento) : [...insights];
    }

    /**
     * LEY DE RECÁLCULO: nivel del segmento = promedio de intensidad de
     * sus insights vinculados. Esto es lo que hace que "la rueda" se
     * redibuje automáticamente en la UI cada vez que se agrega/edita
     * un insight — la UI solo tiene que volver a llamar esta función.
     */
    function calcularRueda() {
        return SEGMENTOS.map(segmento => {
            const delSegmento = insights.filter(i => i.segmento === segmento);
            const nivel = delSegmento.length
                ? delSegmento.reduce((acc, i) => acc + i.intensidad, 0) / delSegmento.length
                : 0;
            return { segmento, nivel: Math.round(nivel * 100) / 100, numInsights: delSegmento.length };
        });
    }

    function exportarJSON() {
        return JSON.stringify({ segmentos: SEGMENTOS, insights, rueda: calcularRueda() }, null, 2);
    }

    function exportarCSV() {
        const filas = [["Segmento", "Texto", "Intensidad", "Fuente", "Fecha"]];
        insights.forEach(i => {
            filas.push([i.segmento, `"${i.texto.replace(/"/g, '""')}"`, i.intensidad, i.fuente, i.fecha]);
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

    function clamp(v, min, max) { return Math.min(max, Math.max(min, v)); }

    cargar();

    return {
        SEGMENTOS,
        agregarInsight, editarInsight, eliminarInsight, listarInsights,
        calcularRueda,
        exportarJSON, exportarCSV,
        exportarJSONArchivo: () => descargarArchivo("roper_dynagram.json", exportarJSON(), "application/json"),
        exportarCSVArchivo: () => descargarArchivo("roper_dynagram.csv", exportarCSV(), "text/csv")
    };
})();
