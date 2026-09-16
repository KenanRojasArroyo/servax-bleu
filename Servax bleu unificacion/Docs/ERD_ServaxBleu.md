# ERD — Servax Bleu (Fase 1, Categorías)

**Estado: 11 de 11 tablas con estructura CONFIRMADA** contra `INFORMATION_SCHEMA.COLUMNS` (16/09/2026).

## Diagrama

```mermaid
erDiagram
    CORRAL ||--o{ INVENTARIOPEZ : contiene
    CORRAL ||--o{ MUESTREOAGUA : registra
    CORRAL ||--o{ CRECIMIENTOANUAL : mide
    CORRAL ||--o{ REGISTROALIMENTACION : recibe
    CORRAL ||--o{ HISTORIALCORRAL : historia
    CORRAL ||--o{ DISTRIBUCIONALIMENTO : recibe_carnada

    ESPECIE ||--o{ INVENTARIOPEZ : clasifica
    ESPECIE ||--o{ CRECIMIENTOANUAL : clasifica

    ALIMENTO ||--o{ REGISTROALIMENTACION : usado_en
    ALIMENTO ||--o{ DISTRIBUCIONALIMENTO : distribuido_como

    BARCO ||--o{ DISTRIBUCIONALIMENTO : transporta

    CALIDAD {
        int id PK
        date Fecha
        float Temperatura
        float Oxigeno
        float Profundidad
    }

    CORRAL {
        int IdCorral PK
        varchar Nombre
        varchar Ubicacion
        decimal CapacidadMaxima
        date FechaInstalacion
        varchar Estado
        varchar Observaciones
    }

    ESPECIE {
        int IdEspecie PK
        varchar Nombre
        varchar NombreCientifico
        varchar Tipo
        bit EsToxica
        varchar Descripcion
        bit Activo
    }

    INVENTARIOPEZ {
        int IdInventario PK
        int IdCorral FK
        int IdEspecie FK
        int Cantidad
        decimal PesoPromedioKg
        varchar Estado
        date FechaRegistro
        varchar Observaciones
    }

    MUESTREOAGUA {
        int IdMuestreo PK
        int IdCorral FK
        date Fecha
        float Temperatura
        float Oxigeno
        float Profundidad
        float PH
        float Salinidad
        varchar Nutrientes
        varchar Irregularidad
        varchar Observaciones
    }

    CRECIMIENTOANUAL {
        int IdCrecimiento PK
        int IdCorral FK
        int IdEspecie FK
        int Anio
        decimal PesoPromedioInicial
        decimal PesoPromedioFinal
        float TasaCrecimiento
        varchar Observaciones
    }

    REGISTROALIMENTACION {
        int IdRegistro PK
        int IdCorral FK
        int IdAlimento FK
        date Fecha
        decimal CantidadKg
        varchar Responsable
        int Mortalidad
        varchar Observaciones
    }

    ALIMENTO {
        int IdAlimento PK
        varchar Nombre
        varchar TipoAlimento
        varchar UnidadMedida
        decimal StockActual
        decimal CostoUnitario
        varchar Observaciones
    }

    BARCO {
        int IdBarco PK
        varchar Nombre
        decimal CapacidadToneladas
        varchar Estado
        date FechaAlta
        varchar Observaciones
    }

    DISTRIBUCIONALIMENTO {
        int IdDistribucion PK
        int IdBarco FK
        int IdAlimento FK
        int IdCorral FK
        datetime Fecha
        decimal CantidadToneladas
        varchar FormulaAplicada
        varchar Observaciones
    }

    HISTORIALCORRAL {
        int IdHistorial PK
        int IdCorral FK
        datetime Fecha
        int CantidadPeces
        varchar EstadoGeneral
        varchar ResumenCalidadAgua
        varchar ResumenNutrientes
        varchar Observaciones
    }
```

## Notas de diseño

- **`Calidad` no tiene relación FK real con ninguna otra tabla del esquema nuevo.** Es la tabla legacy
  protegida (no se puede modificar su estructura). El monitoreo real por corral vive en `MuestreoAgua`,
  que sí tiene `IdCorral` y las mismas métricas (Temperatura, Oxígeno, Profundidad) más PH, Salinidad,
  Nutrientes e Irregularidad. Considerar si `Calidad` se deprecia a futuro en favor de `MuestreoAgua`
  (pregunta para el cliente, no decisión unilateral del equipo).
- **`DistribucionAlimento`** implementa el RF del cliente "fórmula de distribución de carnada por
  tonelaje según capacidad de barco". `FormulaAplicada` guarda como texto qué regla se usó para esa
  distribución específica (útil para auditar/explicar el cálculo). Como todavía no hay una fórmula de
  negocio confirmada por el cliente, el backend usa un reparto proporcional a `CapacidadToneladas`
  como placeholder — ver `DistribucionAlimentoController.cs`.
- **`HistorialCorral`** es una tabla de snapshot/bitácora: guarda una foto periódica del estado del
  corral (`CantidadPeces`, `EstadoGeneral`, resúmenes de calidad de agua y nutrientes en texto), no un
  registro transaccional como `MuestreoAgua`. Sirve para el RF de "historial de inventario por corral".

## Estado: cerrado ✅
Las 11 tablas están confirmadas. Este ERD ya se puede usar como entregable formal de la Fase 1 del
examen de Arquitectura de la Información (falta pasarlo a un diagrama editable tipo draw.io/Lucidchart
para el PDF final, pero el contenido/relaciones ya están correctos).

