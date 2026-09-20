/**
 * empathy-map-engine.js — Interfaz 4 ("The Parser")
 *
 * REESCRITURA (sept. 2026): la versión anterior solo guardaba arreglos
 * planos de texto por cuadrante, sin bandeja de ruido cualitativo, sin
 * intensidad emocional y sin generador de insights. La rúbrica pide:
 *   - Bandeja de ruido cualitativo sin procesar (frases sueltas).
 *   - Asignación de cada fragmento a un cuadrante (Dice/Hace/Piensa/Siente).
 *   - Escala de intensidad emocional en "Siente" (Baja/Media/Alta).
 *   - Generador de insights: tipo, descripción, prioridad.
 *   - Visualización real de la cuadrícula 2x2 (se resuelve en el HTML).
 *
 * No usa investigacion-motor-base.js porque su estructura no es una lista
 * plana de registros (es una cuadrícula + bandeja + insights).
 */

const MotorEmpathyMap = (function () {
    const STORAGE_KEY = "servaxbleu_empathy_map_v2";
    const CUADRANTES = ["dice", "hace", "piensa", "siente"];
    let mapas = [];
    let siguienteId = 1;
    let siguienteInsightId = 1;

    function guardar() {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify({ mapas, siguienteId, siguienteInsightId })); }
        catch (e) { console.error("No se pudo guardar el Empathy Map:", e); }
    }

    function cargar() {
        try {
            const crudo = localStorage.getItem(STORAGE_KEY);
            if (crudo) {
                const parsed = JSON.parse(crudo);
                mapas = parsed.mapas || [];
                siguienteId = parsed.siguienteId || 1;
                siguienteInsightId = parsed.siguienteInsightId || 1;
            }
        } catch (e) { console.error("No se pudo leer el Empathy Map, se usa estado vacío:", e); }
    }

    /** perfil: alias/rol de la persona (o segmento) a la que le estamos haciendo el mapa */
    function crearMapa({ alias, contexto = "", fecha = new Date().toISOString().slice(0, 10) }) {
        const nuevo = {
            id: siguienteId++, alias, contexto, fecha,
            bandejaRuido: [],           // frases sueltas sin procesar
            dice: [], hace: [], piensa: [],
            siente: [],                 // { texto, intensidad: "Baja"|"Media"|"Alta" }
            insights: []                // { id, texto, tipo, prioridad }
        };
        mapas.push(nuevo);
        guardar();
        return nuevo;
    }

    function obtenerMapa(id) { return mapas.find(m => m.id === id) || null; }
    function listarMapas() { return [...mapas]; }
    function eliminarMapa(id) { mapas = mapas.filter(m => m.id !== id); guardar(); }

    function agregarRuido(idMapa, texto) {
        const m = obtenerMapa(idMapa);
        if (!m) return null;
        m.bandejaRuido.push({ texto, asignado: false });
        guardar();
        return m;
    }

    /** Mueve un fragmento de la bandeja de ruido a un cuadrante (lo "parsea"). */
    function asignarRuidoACuadrante(idMapa, indiceRuido, cuadrante, intensidad = "Media") {
        const m = obtenerMapa(idMapa);
        if (!m || !CUADRANTES.includes(cuadrante)) return null;
        const item = m.bandejaRuido[indiceRuido];
        if (!item) return null;
        agregarItem(idMapa, cuadrante, item.texto, intensidad);
        item.asignado = true;
        guardar();
        return m;
    }

    function agregarItem(idMapa, cuadrante, texto, intensidad = "Media") {
        if (!CUADRANTES.includes(cuadrante)) {
            throw new Error(`Cuadrante inválido: ${cuadrante}. Debe ser uno de ${CUADRANTES.join(", ")}`);
        }
        const m = obtenerMapa(idMapa);
        if (!m) return null;
        m[cuadrante].push(cuadrante === "siente" ? { texto, intensidad } : { texto });
        guardar();
        return m;
    }

    function quitarItem(idMapa, cuadrante, indice) {
        const m = obtenerMapa(idMapa);
        if (!m || !CUADRANTES.includes(cuadrante)) return null;
        m[cuadrante].splice(indice, 1);
        guardar();
        return m;
    }

    function agregarInsight(idMapa, { texto, tipo = "Necesidad oculta", prioridad = "Media" }) {
        const m = obtenerMapa(idMapa);
        if (!m) return null;
        m.insights.push({ id: siguienteInsightId++, texto, tipo, prioridad });
        guardar();
        return m;
    }

    function eliminarInsight(idMapa, idInsight) {
        const m = obtenerMapa(idMapa);
        if (!m) return null;
        m.insights = m.insights.filter(i => i.id !== idInsight);
        guardar();
        return m;
    }

    function exportarJSON() { return JSON.stringify(mapas, null, 2); }

    function exportarCSV() {
        const filas = [["Alias", "Cuadrante/Bandeja", "Texto", "Intensidad/Tipo", "Prioridad"]];
        mapas.forEach(m => {
            m.bandejaRuido.forEach(r => filas.push([m.alias, "Bandeja de ruido", r.texto, r.asignado ? "Asignado" : "Sin asignar", ""]));
            CUADRANTES.forEach(c => m[c].forEach(item =>
                filas.push([m.alias, c, item.texto, item.intensidad || "", ""])));
            m.insights.forEach(i => filas.push([m.alias, "Insight", i.texto, i.tipo, i.prioridad]));
        });
        return filas.map(f => f.map(v => `"${(v ?? "").toString().replace(/"/g, '""')}"`).join(",")).join("\n");
    }

    function descargarArchivo(nombre, contenido, tipoMime) {
        const blob = new Blob([contenido], { type: tipoMime });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url; a.download = nombre; a.click();
        URL.revokeObjectURL(url);
    }

    /** Precarga el Empathy Map real del Encargado de Corrales (Interfaz 2). */
    function seedSiVacio() {
        if (mapas.length > 0) return;
        const m = crearMapa({
            alias: "Encargado de Corrales y Control de Datos",
            contexto: "Síntesis de la entrevista + observación directa en instalaciones de Servax Bleu, sept. 2026.",
            fecha: "2026-09-18"
        });
        ["pues igual busco la manera de que no se acumule", "no conozco otra forma de hacerlo más rápido",
            "esto ya lo veníamos arrastrando desde antes"].forEach(t => agregarRuido(m.id, t));

        agregarItem(m.id, "dice", "Cuando hay demasiados datos, transcribirlos a mano nos da más trabajo.");
        agregarItem(m.id, "dice", "Que no tuvieran que ser ingresados a mano me facilitaría la eficiencia.");

        agregarItem(m.id, "hace", "Funge como \"puente humano\" para transcribir datos de sensores.");
        agregarItem(m.id, "hace", "Mueve información físicamente de la PC vieja (con el programa) a la PC nueva.");
        agregarItem(m.id, "hace", "Calcula fórmulas manuales en Excel para los barcos.");

        agregarItem(m.id, "piensa", "Es ilógico hacer trabajo manual en un entorno con sensores en tiempo real.");
        agregarItem(m.id, "piensa", "Automatizar la plataforma es la única forma real de tener control de los corrales.");
        agregarItem(m.id, "piensa", "El sistema actual es \"ambiguo y enredoso\", un obstáculo en lugar de ayuda.");

        agregarItem(m.id, "siente", "Frustración por ser bloqueado por tareas repetitivas.", "Alta");
        agregarItem(m.id, "siente", "Agobio por la bola de nieve operativa cuando hay picos de datos.", "Media");
        agregarItem(m.id, "siente", "Esperanza por tener un sistema que avise de todo automáticamente.", "Alta");

        agregarInsight(m.id, {
            texto: "Transcripción insostenible: el usuario compensa las fallas de conexión transcribiendo datos masivos, con alta propensión a errores y pérdida de tiempo.",
            tipo: "Necesidad oculta", prioridad: "Alta"
        });
        agregarInsight(m.id, {
            texto: "Cálculo de flota automático: integrar la fórmula matemática de carnada de los barcos para eliminar el cuello de botella del área hermana.",
            tipo: "Requisito funcional", prioridad: "Alta"
        });
        agregarInsight(m.id, {
            texto: "Sistema de alertas tempranas: Servax Bleu debe ser proactivo y enviar notificaciones ante irregularidades en los corrales, sin que el usuario tenga que mirar gráficas.",
            tipo: "Necesidad oculta", prioridad: "Alta"
        });
        agregarInsight(m.id, {
            texto: "Aislamiento de privacidad: uso obligatorio de datos simulados (mock data) para desarrollo y diseño de una API sencilla para el departamento de TI del cliente.",
            tipo: "Restricción técnica", prioridad: "Media"
        });
    }

    cargar();

    return {
        CUADRANTES,
        crearMapa, obtenerMapa, listarMapas, eliminarMapa,
        agregarRuido, asignarRuidoACuadrante, agregarItem, quitarItem,
        agregarInsight, eliminarInsight,
        seedSiVacio,
        exportarJSON, exportarCSV,
        exportarJSONArchivo: () => descargarArchivo("empathy_map.json", exportarJSON(), "application/json"),
        exportarCSVArchivo: () => descargarArchivo("empathy_map.csv", exportarCSV(), "text/csv")
    };
})();
