/**
 * roper-dynagram-engine.js — Interfaz 5 (Roper Dynagram)
 *
 * CORRECCIÓN (sept. 2026): la versión anterior modelaba los 8 segmentos
 * del "Cultural Dynagram" (Economía, Tecnología...), que NO es lo que pide
 * la rúbrica. La rúbrica pide segmentación de VALORES de Roper con 4
 * segmentos: Realists, Open Minded, Adventurers, Organics — y asignación
 * de usuarios reales (de las entrevistas de campo) a cada segmento, con
 * evidencia, no "insights" sueltos con una intensidad inventada.
 *
 * "Ley" de recálculo: el % de cada segmento se deriva SIEMPRE de contar
 * cuántos usuarios reales están asignados a ese segmento — nunca se
 * escribe el porcentaje a mano. Agregar/editar/eliminar un usuario
 * recalcula automáticamente toda la rueda.
 */

const MotorRoperDynagram = (function () {
  const STORAGE_KEY = "servaxbleu_roper_dynagram_v2";

  // Los 4 segmentos oficiales de la rúbrica (no cambiar sin confirmar con el profesor).
  const SEGMENTOS = ["Realists", "Open Minded", "Adventurers", "Organics"];

  // Panel dinámico por segmento: Requisito UX, Funcionalidad y Tono
  // (contenido editorial fijo del equipo, no viene de las entrevistas).
  const PERFIL_SEGMENTO = {
    "Realists": {
      requisitoUX: "Control total y trazabilidad de cada dato capturado",
      funcionalidad: "CRUD explícito, historial de cambios, confirmaciones antes de guardar",
      tono: "Directo, técnico, sin adornos"
    },
    "Open Minded": {
      requisitoUX: "Flexibilidad para explorar datos de formas no previstas",
      funcionalidad: "Filtros combinables, exportación libre, Asistente de IA en lenguaje natural",
      tono: "Exploratorio, invita a probar"
    },
    "Adventurers": {
      requisitoUX: "Velocidad para tomar una decisión operativa ya",
      funcionalidad: "Alertas push, atajos de captura rápida, dashboard con lo crítico primero",
      tono: "Urgente, accionable, pocas palabras"
    },
    "Organics": {
      requisitoUX: "Contexto y relación entre variables antes de actuar",
      funcionalidad: "Vistas cruzadas (agua + alimento + mortalidad), reportes narrativos, historial por corral",
      tono: "Explicativo, conecta causa y efecto"
    }
  };

  let usuarios = [];
  let siguienteId = 1;

  function guardar() {
    try { localStorage.setItem(STORAGE_KEY, JSON.stringify({ usuarios, siguienteId })); }
    catch (e) { console.error("No se pudo guardar el Roper Dynagram:", e); }
  }

  function cargar() {
    try {
      const crudo = localStorage.getItem(STORAGE_KEY);
      if (crudo) {
        const parsed = JSON.parse(crudo);
        usuarios = parsed.usuarios || [];
        siguienteId = parsed.siguienteId || 1;
      }
    } catch (e) { console.error("No se pudo leer el Roper Dynagram, se usa estado vacío:", e); }
  }

  /**
   * Asigna a un usuario REAL (de las entrevistas/observaciones de campo)
   * a uno de los 4 segmentos, con la evidencia que sustenta esa asignación.
   */
  function asignarUsuario({ alias, segmento, evidencia, fuente = "", esUsuarioExtremo = false, esExperto = false, fecha = new Date().toISOString() }) {
    if (!SEGMENTOS.includes(segmento)) {
      throw new Error(`Segmento inválido: ${segmento}. Debe ser uno de ${SEGMENTOS.join(", ")}`);
    }
    if (!alias || !evidencia) {
      throw new Error("alias y evidencia son obligatorios — no se aceptan asignaciones sin evidencia.");
    }
    const nuevo = { id: siguienteId++, alias, segmento, evidencia, fuente, esUsuarioExtremo, esExperto, fecha };
    usuarios.push(nuevo);
    guardar();
    return nuevo;
  }

  function editarUsuario(id, cambios) {
    const usuario = usuarios.find(u => u.id === id);
    if (!usuario) return null;
    Object.assign(usuario, cambios);
    guardar();
    return usuario;
  }

  function eliminarUsuario(id) {
    usuarios = usuarios.filter(u => u.id !== id);
    guardar();
  }

  function listarUsuarios(segmento = null) {
    return segmento ? usuarios.filter(u => u.segmento === segmento) : [...usuarios];
  }

  /**
   * LEY DE RECÁLCULO: % de cada segmento = usuarios asignados a ese
   * segmento / total de usuarios asignados. Nunca se captura a mano.
   */
  function calcularRueda() {
    const total = usuarios.length;
    return SEGMENTOS.map(segmento => {
      const delSegmento = usuarios.filter(u => u.segmento === segmento);
      const porcentaje = total ? Math.round((delSegmento.length / total) * 1000) / 10 : 0;
      return {
        segmento,
        cantidad: delSegmento.length,
        porcentaje,
        perfil: PERFIL_SEGMENTO[segmento]
      };
    });
  }

  function resumenCobertura() {
    const total = usuarios.length;
    return {
      totalUsuarios: total,
      cubreExtremo: usuarios.some(u => u.esUsuarioExtremo),
      cubreExperto: usuarios.some(u => u.esExperto),
      cumpleMinimoExamen: total >= 4 && usuarios.some(u => u.esUsuarioExtremo) && usuarios.some(u => u.esExperto)
    };
  }

  function exportarJSON() {
    return JSON.stringify({ segmentos: SEGMENTOS, usuarios, rueda: calcularRueda() }, null, 2);
  }

  function exportarCSV() {
    const filas = [["Alias", "Segmento", "Evidencia", "Fuente", "UsuarioExtremo", "Experto", "Fecha"]];
    usuarios.forEach(u => {
      filas.push([u.alias, u.segmento, `"${u.evidencia.replace(/"/g, '""')}"`, u.fuente, u.esUsuarioExtremo, u.esExperto, u.fecha]);
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
    SEGMENTOS, PERFIL_SEGMENTO,
    asignarUsuario, editarUsuario, eliminarUsuario, listarUsuarios,
    calcularRueda, resumenCobertura,
    exportarJSON, exportarCSV,
    exportarJSONArchivo: () => descargarArchivo("roper_dynagram.json", exportarJSON(), "application/json"),
    exportarCSVArchivo: () => descargarArchivo("roper_dynagram.csv", exportarCSV(), "text/csv")
  };
})();
