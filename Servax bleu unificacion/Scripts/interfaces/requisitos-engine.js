/**
 * requisitos-engine.js
 * ---------------------------------------------------------------
 * Motor de datos para la Interfaz 6 — Mapeo de Requerimientos
 * (Dynagram interactivo, obligatoria en el examen de Investigación
 * de Usuarios).
 *
 * Responsabilidad de este módulo (backend/lógica, sin UI):
 *   - Modelo de datos de Requisitos (RF/RNF), Insights y Decisiones.
 *   - "Ley embebida" de recálculo automático de prioridad.
 *   - Trazabilidad Insight -> Requisito -> Decisión.
 *   - Persistencia en localStorage.
 *   - Exportación a JSON y CSV.
 *
 * Fer/Carlos consumen esta API desde la UI del dynagram; este
 * archivo NO dibuja nada, solo mantiene y calcula el estado.
 * ---------------------------------------------------------------
 */

const RequisitosEngine = (function () {
    const STORAGE_KEY = "servaxbleu_requisitos_engine_v1";

    /** Estado en memoria. Se sincroniza con localStorage en cada mutación. */
    let estado = {
        requisitos: [],   // { id, codigo, descripcion, tipo, prioridadBase, prioridadCalculada, insightIds: [], decision: null }
        insights: [],      // { id, fuente, texto, urgencia (1-5), impacto (1-5), fecha, usuario }
        siguienteIdRequisito: 1,
        siguienteIdInsight: 1
    };

    // ---------------------------------------------------------------
    // Persistencia
    // ---------------------------------------------------------------

    function guardar() {
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(estado));
        } catch (e) {
            console.error("No se pudo guardar en localStorage:", e);
        }
    }

    function cargar() {
        try {
            const crudo = localStorage.getItem(STORAGE_KEY);
            if (crudo) {
                estado = JSON.parse(crudo);
            }
        } catch (e) {
            console.error("No se pudo leer localStorage, se usa estado vacío:", e);
        }
    }

    /**
     * Carga inicial de los RF-01..RF-21 / RNF-01..RNF-07 ya documentados
     * en el levantamiento de requerimientos, si el motor está vacío.
     * Recibe un arreglo [{ codigo, descripcion, tipo, prioridadBase }, ...]
     */
    function sembrarSiVacio(listaRequisitosIniciales) {
        if (estado.requisitos.length > 0) return false; // ya hay datos, no pisar
        listaRequisitosIniciales.forEach(r => crearRequisito(r));
        return true;
    }

    // ---------------------------------------------------------------
    // CRUD Requisitos
    // ---------------------------------------------------------------

    function crearRequisito({ codigo, descripcion, tipo = "Funcional", prioridadBase = 3 }) {
        const nuevo = {
            id: estado.siguienteIdRequisito++,
            codigo,
            descripcion,
            tipo,               // "Funcional" | "No Funcional"
            prioridadBase: clamp(prioridadBase, 1, 5),
            prioridadCalculada: clamp(prioridadBase, 1, 5),
            insightIds: [],
            decision: null      // { estado: "Aceptado"|"Rechazado"|"En análisis", justificacion, fecha }
        };
        estado.requisitos.push(nuevo);
        guardar();
        return nuevo;
    }

    function editarRequisito(id, cambios) {
        const req = obtenerRequisito(id);
        if (!req) return null;
        Object.assign(req, cambios);
        recalcularPrioridad(id);
        guardar();
        return req;
    }

    function eliminarRequisito(id) {
        estado.requisitos = estado.requisitos.filter(r => r.id !== id);
        // limpiar referencias en insights vinculados
        estado.insights.forEach(i => {
            i.requisitoIds = (i.requisitoIds || []).filter(rid => rid !== id);
        });
        guardar();
    }

    function obtenerRequisito(id) {
        return estado.requisitos.find(r => r.id === id) || null;
    }

    function listarRequisitos() {
        return [...estado.requisitos];
    }

    // ---------------------------------------------------------------
    // CRUD Insights
    // ---------------------------------------------------------------

    function crearInsight({ fuente, texto, urgencia = 3, impacto = 3, fecha = new Date().toISOString(), usuario = "" }) {
        const nuevo = {
            id: estado.siguienteIdInsight++,
            fuente,       // "Entrevista" | "Observación" | "Usuario extremo" | etc.
            texto,
            urgencia: clamp(urgencia, 1, 5),
            impacto: clamp(impacto, 1, 5),
            fecha,
            usuario,
            requisitoIds: []
        };
        estado.insights.push(nuevo);
        guardar();
        return nuevo;
    }

    function editarInsight(id, cambios) {
        const insight = obtenerInsight(id);
        if (!insight) return null;
        Object.assign(insight, cambios);
        // Si cambió urgencia/impacto, recalcular todos los requisitos vinculados
        insight.requisitoIds.forEach(recalcularPrioridad);
        guardar();
        return insight;
    }

    function eliminarInsight(id) {
        const insight = obtenerInsight(id);
        if (insight) {
            insight.requisitoIds.forEach(rid => {
                const req = obtenerRequisito(rid);
                if (req) req.insightIds = req.insightIds.filter(iid => iid !== id);
            });
        }
        estado.insights = estado.insights.filter(i => i.id !== id);
        insight?.requisitoIds.forEach(recalcularPrioridad);
        guardar();
    }

    function obtenerInsight(id) {
        return estado.insights.find(i => i.id === id) || null;
    }

    function listarInsights() {
        return [...estado.insights];
    }

    // ---------------------------------------------------------------
    // Vínculos Insight <-> Requisito (relación muchos a muchos)
    // ---------------------------------------------------------------

    function vincularInsightRequisito(insightId, requisitoId) {
        const insight = obtenerInsight(insightId);
        const req = obtenerRequisito(requisitoId);
        if (!insight || !req) return false;

        if (!insight.requisitoIds.includes(requisitoId)) insight.requisitoIds.push(requisitoId);
        if (!req.insightIds.includes(insightId)) req.insightIds.push(insightId);

        recalcularPrioridad(requisitoId);
        guardar();
        return true;
    }

    function desvincularInsightRequisito(insightId, requisitoId) {
        const insight = obtenerInsight(insightId);
        const req = obtenerRequisito(requisitoId);
        if (insight) insight.requisitoIds = insight.requisitoIds.filter(id => id !== requisitoId);
        if (req) req.insightIds = req.insightIds.filter(id => id !== insightId);

        recalcularPrioridad(requisitoId);
        guardar();
    }

    // ---------------------------------------------------------------
    // LEY EMBEBIDA de recálculo de prioridad
    // ---------------------------------------------------------------
    /**
     * Fórmula (documentar tal cual en el entregable, es la "ley" que pide el examen):
     *
     *   prioridadCalculada = clamp(1, 5, round(
     *        prioridadBase        * 0.40
     *      + promedioUrgencia     * 0.35
     *      + promedioImpacto      * 0.25
     *      + bonusEvidencia
     *   ))
     *
     *   bonusEvidencia = +0.5 si hay 3 o más insights vinculados
     *                    (evidencia robusta de campo, no solo la
     *                    entrevista inicial del cliente)
     *
     * Si un requisito no tiene insights vinculados todavía,
     * prioridadCalculada = prioridadBase (no hay evidencia que la mueva).
     */
    function recalcularPrioridad(requisitoId) {
        const req = obtenerRequisito(requisitoId);
        if (!req) return null;

        const insightsVinculados = req.insightIds
            .map(obtenerInsight)
            .filter(Boolean);

        if (insightsVinculados.length === 0) {
            req.prioridadCalculada = req.prioridadBase;
            guardar();
            return req.prioridadCalculada;
        }

        const promedioUrgencia = promedio(insightsVinculados.map(i => i.urgencia));
        const promedioImpacto = promedio(insightsVinculados.map(i => i.impacto));
        const bonusEvidencia = insightsVinculados.length >= 3 ? 0.5 : 0;

        const bruto =
            req.prioridadBase * 0.40 +
            promedioUrgencia * 0.35 +
            promedioImpacto * 0.25 +
            bonusEvidencia;

        req.prioridadCalculada = clamp(Math.round(bruto), 1, 5);
        guardar();
        return req.prioridadCalculada;
    }

    function recalcularTodo() {
        estado.requisitos.forEach(r => recalcularPrioridad(r.id));
    }

    // ---------------------------------------------------------------
    // Decisiones
    // ---------------------------------------------------------------

    function registrarDecision(requisitoId, { estado: estadoDecision, justificacion, fecha = new Date().toISOString() }) {
        const req = obtenerRequisito(requisitoId);
        if (!req) return null;
        req.decision = { estado: estadoDecision, justificacion, fecha };
        guardar();
        return req.decision;
    }

    // ---------------------------------------------------------------
    // Trazabilidad Insight -> Requisito -> Decisión
    // ---------------------------------------------------------------

    function obtenerTrazabilidad(requisitoId) {
        const req = obtenerRequisito(requisitoId);
        if (!req) return null;
        return {
            requisito: req,
            insights: req.insightIds.map(obtenerInsight).filter(Boolean),
            decision: req.decision
        };
    }

    function obtenerTrazabilidadCompleta() {
        return estado.requisitos.map(r => obtenerTrazabilidad(r.id));
    }

    // ---------------------------------------------------------------
    // Exportación
    // ---------------------------------------------------------------

    function exportarJSON() {
        return JSON.stringify(estado, null, 2);
    }

    function exportarCSV() {
        const filas = [["Codigo", "Descripcion", "Tipo", "PrioridadBase", "PrioridadCalculada", "NumInsights", "EstadoDecision"]];
        estado.requisitos.forEach(r => {
            filas.push([
                r.codigo,
                `"${(r.descripcion || "").replace(/"/g, '""')}"`,
                r.tipo,
                r.prioridadBase,
                r.prioridadCalculada,
                r.insightIds.length,
                r.decision ? r.decision.estado : "Sin decisión"
            ]);
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

    function exportarJSONArchivo() {
        descargarArchivo("requisitos_servaxbleu.json", exportarJSON(), "application/json");
    }

    function exportarCSVArchivo() {
        descargarArchivo("requisitos_servaxbleu.csv", exportarCSV(), "text/csv");
    }

    // ---------------------------------------------------------------
    // Utilidades
    // ---------------------------------------------------------------

    function clamp(valor, min, max) {
        return Math.min(max, Math.max(min, valor));
    }

    function promedio(lista) {
        if (!lista.length) return 0;
        return lista.reduce((a, b) => a + b, 0) / lista.length;
    }

    // Cargar estado guardado al inicializar el módulo
    cargar();

    // ---------------------------------------------------------------
    // API pública
    // ---------------------------------------------------------------
    return {
        sembrarSiVacio,
        crearRequisito, editarRequisito, eliminarRequisito, obtenerRequisito, listarRequisitos,
        crearInsight, editarInsight, eliminarInsight, obtenerInsight, listarInsights,
        vincularInsightRequisito, desvincularInsightRequisito,
        recalcularPrioridad, recalcularTodo,
        registrarDecision,
        obtenerTrazabilidad, obtenerTrazabilidadCompleta,
        exportarJSON, exportarCSV, exportarJSONArchivo, exportarCSVArchivo
    };
})();
