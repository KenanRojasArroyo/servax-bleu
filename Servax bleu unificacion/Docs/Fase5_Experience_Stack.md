# Fase 5 — Experience Stack (Servax Bleu)

Diagrama conceptual de una sola lámina, en 3 estratos apilados, mostrando cómo
cada capa de Arquitectura de la Información sostiene a la de arriba.

```mermaid
graph TB
    subgraph ENTREGA["🎯 ENTREGA — lo que el usuario ve y usa"]
        E1["Dashboard con KPIs de biomasa,<br/>alimento y mortalidad"]
        E2["Formularios de captura rápida<br/>con validación en línea"]
        E3["Asistente IA en lenguaje natural<br/>(widget de chat)"]
        E4["Alertas por correo ante<br/>irregularidades"]
        E5["Reportes exportables<br/>(CSV) de mortalidad y carnada"]
    end

    subgraph ESTRUCTURA["🗺️ ESTRUCTURA — cómo se organiza y conecta"]
        S1["Sitemap por área operativa:<br/>Corrales/Biomasa · Alimentación/Barcos · Analítica"]
        S2["Task Flows con ruta feliz,<br/>side doors y manejo de errores"]
        S3["Rutas MVC ↔ Queries SQL<br/>(mapeo 1 a 1 documentado en Fase 1)"]
        S4["Pirámide invertida + Johnson Box<br/>en pantallas clave"]
    end

    subgraph CIMIENTOS["🏗️ CIMIENTOS — de dónde vienen los datos"]
        C1["Esquema relacional ServaxBleu<br/>(11 tablas, PK/FK, índices idx_*)"]
        C2["Servicios: Conexion.cs,<br/>ServicioNotificaciones.cs, TextToSqlService.cs"]
        C3["Fuente original: captura manual<br/>+ importación de Excel (ExcelDataReader)"]
    end

    ENTREGA --> ESTRUCTURA --> CIMIENTOS

    style ENTREGA fill:#1595bb,color:#fff
    style ESTRUCTURA fill:#087b9e,color:#fff
    style CIMIENTOS fill:#0b2034,color:#fff
```

## Lectura del diagrama

- **Cimientos → Estructura:** el esquema de 11 tablas (con sus FK e índices
  `idx_*`) es lo que hace posible que las rutas de Fase 1 tengan una query
  literal detrás de cada una — sin ese esquema, el mapeo ruta→query de
  `Fase1_Navegacion_y_Metadatos.md` no existiría.
- **Estructura → Entrega:** el sitemap agrupado por área operativa (no por
  tabla cruda) es lo que permite que el Dashboard muestre KPIs consolidados
  en vez de una lista de 11 pantallas CRUD sueltas; los Task Flows (Fase 2)
  son los que garantizan que, cuando algo sale mal en la Entrega (SMTP caído,
  IA alucinando), el usuario ve un mensaje claro en vez de un error crudo.
- **Por qué una sola lámina:** el objetivo es poder señalar, frente al
  profesor, cualquier elemento visible en el Dashboard y trazarlo hacia abajo
  hasta la tabla/columna exacta que lo alimenta — es la misma lógica de
  "3 capas" que ya se usa en los wireframes anotados (Fase 3).
