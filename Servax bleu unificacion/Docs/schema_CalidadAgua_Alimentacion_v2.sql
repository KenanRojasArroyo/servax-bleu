-- =====================================================================
-- schema_CalidadAgua_Alimentacion_v2.sql
-- Extensión de ServaxBleu — Calidad de Agua (factores abióticos +
-- fitoplancton) y Alimentación (cálculo de carga + consumo/inventario).
--
-- Se aplica DESPUÉS de schema_ServaxBleu.sql (usa Corral, Barco, Alimento,
-- RegistroAlimentacion ya existentes; no los reemplaza, los extiende).
--
-- SUPUESTOS A CONFIRMAR CON ARIAN (marcados en cada sección con ⚠️):
--   1. Las 3 columnas con dato en cada grupo de nutriente/O2/turbidez de la
--      hoja por sitio son 3 medidas por día (mañana/tarde/noche) — así lo
--      pidió el equipo modelarlo por ahora, PENDIENTE de confirmación.
--   2. Fitoplancton y Alimentación (Cálculo de Carga + Consumo) se
--      diseñaron solo con la descripción de texto — faltan las imágenes
--      05, 06 y 02 para verificar columnas exactas.
-- =====================================================================

-- ---------------------------------------------------------------------
-- 0. Sitios de monitoreo (3 actuales)
-- ---------------------------------------------------------------------
CREATE TABLE Sitio (
    IdSitio     INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      VARCHAR(100) NOT NULL UNIQUE   -- 'Puerto Escondido', 'Bajo Soledad', 'Altamar y Sauzal'
);
GO
INSERT INTO Sitio (Nombre) VALUES ('Puerto Escondido'), ('Bajo Soledad'), ('Altamar y Sauzal');
GO

-- Cada corral/sistema (ej. "Sistema Lobina", "Línea de Cosecha") pertenece a un sitio.
ALTER TABLE Corral ADD IdSitio INT NULL FOREIGN KEY REFERENCES Sitio(IdSitio);
GO

-- ---------------------------------------------------------------------
-- 1. FACTORES ABIÓTICOS
-- ---------------------------------------------------------------------

-- 1.1 Sensores instalados por corral/profundidad (AquaHub, BloopTracker...)
CREATE TABLE Sensor (
    IdSensor                INT IDENTITY(1,1) PRIMARY KEY,
    NumeroSensor            VARCHAR(20) NOT NULL,      -- '#180', '#178'
    Marca                   VARCHAR(50) NOT NULL,      -- 'AquaHub', 'BloopTracker'
    IdCorral                INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    ProfundidadInstalacion  DECIMAL(5,2) NOT NULL,     -- 3.0 o 20.0 (m)
    Activo                  BIT NOT NULL DEFAULT 1
);
GO

-- 1.2 Catálogo de nutrientes medidos en laboratorio
CREATE TABLE Nutriente (
    IdNutriente     INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          VARCHAR(50) NOT NULL UNIQUE,       -- Nitritos, Nitratos, Silicatos, Hierro, Amonio, Fosfatos
    UnidadMedida    VARCHAR(20) NOT NULL DEFAULT 'mg/L'
);
GO
INSERT INTO Nutriente (Nombre) VALUES
    ('Nitritos'), ('Nitratos'), ('Silicatos'), ('Hierro'), ('Amonio'), ('Fosfatos');
GO

-- 1.3 Muestreo de laboratorio por SITIO (hoja "Bajo Soledad", etc.)
--     Una fila = una fecha + un turno (mañana/tarde/noche) dentro del sitio.
--     ⚠️ Turno = supuesto a confirmar (ver encabezado del archivo).
CREATE TABLE MuestreoAbiotico (
    IdMuestreo          INT IDENTITY(1,1) PRIMARY KEY,
    IdSitio             INT NOT NULL FOREIGN KEY REFERENCES Sitio(IdSitio),
    Fecha               DATE NOT NULL,
    Turno               VARCHAR(10) NOT NULL
                        CHECK (Turno IN ('Mañana', 'Tarde', 'Noche')),
    OxigenoDisueltoMgL  DECIMAL(6,2) NULL,
    TurbidezM           DECIMAL(6,2) NULL,
    Observaciones       VARCHAR(300) NULL,
    CONSTRAINT UQ_MuestreoAbiotico UNIQUE (IdSitio, Fecha, Turno)
);
GO

-- Valor de cada nutriente para ese muestreo (una fila por nutriente medido;
-- NULL cuando el Excel traía "-").
CREATE TABLE MuestreoNutriente (
    IdMuestreo      INT NOT NULL FOREIGN KEY REFERENCES MuestreoAbiotico(IdMuestreo),
    IdNutriente     INT NOT NULL FOREIGN KEY REFERENCES Nutriente(IdNutriente),
    Valor           DECIMAL(8,3) NULL,
    PRIMARY KEY (IdMuestreo, IdNutriente)
);
GO

-- 1.4 Monitoreo diario por CORRAL/SISTEMA vía sensores (hoja "Sistema Lobina", etc.)
--     Una fila = fecha + corral + profundidad + método de captura.
--     Permite comparar lectura automática del sensor vs. dato tomado a mano al mediodía.
CREATE TABLE LecturaSensorCorral (
    IdLectura               INT IDENTITY(1,1) PRIMARY KEY,
    IdCorral                INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    Fecha                   DATE NOT NULL,
    Profundidad             DECIMAL(5,2) NOT NULL,     -- 3.0 o 20.0 (m)
    MetodoCaptura           VARCHAR(20) NOT NULL
                            CHECK (MetodoCaptura IN ('Sensor', 'Manual')),
    IdSensor                INT NULL FOREIGN KEY REFERENCES Sensor(IdSensor), -- NULL si MetodoCaptura = 'Manual'
    Temperatura             FLOAT NULL,
    OxigenoMgL              FLOAT NULL,
    SaturacionOxigenoPct    FLOAT NULL,
    CONSTRAINT UQ_LecturaSensor UNIQUE (IdCorral, Fecha, Profundidad, MetodoCaptura)
);
GO
CREATE INDEX idx_LecturaSensor_Corral_Fecha ON LecturaSensorCorral(IdCorral, Fecha);
GO

-- ---------------------------------------------------------------------
-- 2. FITOPLANCTON POR SITIO Y ESTACIÓN
--    ⚠️ Diseñado solo con descripción de texto — falta la imagen 02.
-- ---------------------------------------------------------------------
CREATE TABLE CategoriaFitoplancton (
    IdCategoria     INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          VARCHAR(50) NOT NULL UNIQUE        -- Dinoflagelados, Silicoflagelados, Diatomeas
);
GO
INSERT INTO CategoriaFitoplancton (Nombre) VALUES ('Dinoflagelados'), ('Silicoflagelados'), ('Diatomeas');
GO

CREATE TABLE MuestreoFitoplancton (
    IdMuestreoFito              INT IDENTITY(1,1) PRIMARY KEY,
    IdSitio                     INT NOT NULL FOREIGN KEY REFERENCES Sitio(IdSitio),
    Fecha                       DATE NOT NULL,
    Estacion                    VARCHAR(30) NOT NULL,  -- Primavera/Verano/Otoño/Invierno
    AbundanciaTotalCelulasL     DECIMAL(14,2) NULL,
    Especie                     VARCHAR(100) NULL,     -- para cuando empiecen a registrar por especie
    Observaciones               VARCHAR(300) NULL
);
GO

CREATE TABLE MuestreoFitoplanctonCategoria (
    IdMuestreoFito      INT NOT NULL FOREIGN KEY REFERENCES MuestreoFitoplancton(IdMuestreoFito),
    IdCategoria         INT NOT NULL FOREIGN KEY REFERENCES CategoriaFitoplancton(IdCategoria),
    AbundanciaCelulasL  DECIMAL(14,2) NULL,
    PRIMARY KEY (IdMuestreoFito, IdCategoria)
);
GO

-- ---------------------------------------------------------------------
-- 3. ALIMENTACIÓN — Cálculo de carga (1) y consumo/inventario (2)
--    ⚠️ Diseñado solo con descripción de texto — faltan imágenes 05 y 06.
-- ---------------------------------------------------------------------

-- Presentación del alimento (pesan distinto por bolsa)
CREATE TABLE PresentacionAlimento (
    IdPresentacion          INT IDENTITY(1,1) PRIMARY KEY,
    Nombre                  VARCHAR(30) NOT NULL UNIQUE,   -- 'Fresco', 'Congelado'
    PesoPromedioPorBolsaKg  DECIMAL(8,3) NOT NULL
);
GO
INSERT INTO PresentacionAlimento (Nombre, PesoPromedioPorBolsaKg) VALUES ('Fresco', 0), ('Congelado', 0);
-- ⚠️ Pesos en 0 como placeholder — falta que Arian confirme el peso real por bolsa de cada presentación.
GO

ALTER TABLE Alimento ADD IdPresentacionDefault INT NULL FOREIGN KEY REFERENCES PresentacionAlimento(IdPresentacion);
GO

-- (1) Cálculo de carga por barco antes de zarpar
CREATE TABLE CalculoCargaBarco (
    IdCalculo       INT IDENTITY(1,1) PRIMARY KEY,
    IdBarco         INT NOT NULL FOREIGN KEY REFERENCES Barco(IdBarco),
    IdAlimento      INT NOT NULL FOREIGN KEY REFERENCES Alimento(IdAlimento),
    IdPresentacion  INT NOT NULL FOREIGN KEY REFERENCES PresentacionAlimento(IdPresentacion),
    Fecha           DATE NOT NULL,
    NumeroBolsas    INT NOT NULL,
    Observaciones   VARCHAR(300) NULL
);
GO

-- Reparto de esa carga por corral, con el ajuste que indique el reporte de buceo
CREATE TABLE CalculoCargaBarcoCorral (
    IdCalculo               INT NOT NULL FOREIGN KEY REFERENCES CalculoCargaBarco(IdCalculo),
    IdCorral                INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    ToneladasAsignadas      DECIMAL(10,3) NOT NULL,
    ReporteBuceoAjuste      VARCHAR(20) NULL
                            CHECK (ReporteBuceoAjuste IN ('Subir', 'Bajar', 'Sin cambio')),
    Observaciones           VARCHAR(300) NULL,
    PRIMARY KEY (IdCalculo, IdCorral)
);
GO

-- (2) Consumo real / inventario — ya existe RegistroAlimentacion; solo se
-- agrega la Presentación para no perder ese dato en el consumo real.
ALTER TABLE RegistroAlimentacion ADD IdPresentacion INT NULL FOREIGN KEY REFERENCES PresentacionAlimento(IdPresentacion);
GO

-- =====================================================================
-- VISTAS
-- =====================================================================

-- Calidad de agua consolidada por sitio: pivotea nutrientes a columnas
-- (útil para dashboard/reportes y para que el Asistente IA la consulte directo).
CREATE VIEW vw_CalidadAguaSitio AS
SELECT
    s.Nombre AS Sitio,
    m.Fecha,
    m.Turno,
    m.OxigenoDisueltoMgL,
    m.TurbidezM,
    MAX(CASE WHEN n.Nombre = 'Nitritos'  THEN mn.Valor END) AS Nitritos,
    MAX(CASE WHEN n.Nombre = 'Nitratos'  THEN mn.Valor END) AS Nitratos,
    MAX(CASE WHEN n.Nombre = 'Silicatos' THEN mn.Valor END) AS Silicatos,
    MAX(CASE WHEN n.Nombre = 'Hierro'    THEN mn.Valor END) AS Hierro,
    MAX(CASE WHEN n.Nombre = 'Amonio'    THEN mn.Valor END) AS Amonio,
    MAX(CASE WHEN n.Nombre = 'Fosfatos'  THEN mn.Valor END) AS Fosfatos
FROM MuestreoAbiotico m
JOIN Sitio s ON s.IdSitio = m.IdSitio
LEFT JOIN MuestreoNutriente mn ON mn.IdMuestreo = m.IdMuestreo
LEFT JOIN Nutriente n ON n.IdNutriente = mn.IdNutriente
GROUP BY s.Nombre, m.Fecha, m.Turno, m.OxigenoDisueltoMgL, m.TurbidezM;
GO

-- Comparativo lectura de sensor automático vs. dato manual de mediodía, por corral/profundidad.
CREATE VIEW vw_LecturaSensorVsManual AS
SELECT
    c.Nombre AS Corral,
    l.Fecha,
    l.Profundidad,
    MAX(CASE WHEN l.MetodoCaptura = 'Sensor' THEN l.Temperatura END) AS Temp_Sensor,
    MAX(CASE WHEN l.MetodoCaptura = 'Manual' THEN l.Temperatura END) AS Temp_Manual,
    MAX(CASE WHEN l.MetodoCaptura = 'Sensor' THEN l.OxigenoMgL END) AS O2_Sensor,
    MAX(CASE WHEN l.MetodoCaptura = 'Manual' THEN l.OxigenoMgL END) AS O2_Manual
FROM LecturaSensorCorral l
JOIN Corral c ON c.IdCorral = l.IdCorral
GROUP BY c.Nombre, l.Fecha, l.Profundidad;
GO

-- Interrelación factores abióticos + fitoplancton por sitio y fecha
-- (la prioridad que marcó Arian en la reunión).
CREATE VIEW vw_AbioticoFitoplancton AS
SELECT
    s.Nombre AS Sitio,
    a.Fecha,
    AVG(a.OxigenoDisueltoMgL) AS OxigenoPromedioDia,
    AVG(a.TurbidezM) AS TurbidezPromedioDia,
    f.AbundanciaTotalCelulasL,
    f.Estacion
FROM MuestreoAbiotico a
JOIN Sitio s ON s.IdSitio = a.IdSitio
LEFT JOIN MuestreoFitoplancton f ON f.IdSitio = a.IdSitio AND f.Fecha = a.Fecha
GROUP BY s.Nombre, a.Fecha, f.AbundanciaTotalCelulasL, f.Estacion;
GO

-- Consumo de alimento por corral con presentación y peso real (según PresentacionAlimento).
CREATE VIEW vw_ConsumoAlimentoCorral AS
SELECT
    c.Nombre AS Corral,
    al.Nombre AS Alimento,
    p.Nombre AS Presentacion,
    ra.Fecha,
    ra.CantidadKg,
    ra.Mortalidad
FROM RegistroAlimentacion ra
JOIN Corral c ON c.IdCorral = ra.IdCorral
JOIN Alimento al ON al.IdAlimento = ra.IdAlimento
LEFT JOIN PresentacionAlimento p ON p.IdPresentacion = ra.IdPresentacion;
GO

-- Carga calculada por barco con el peso total ya resuelto (bolsas × peso por bolsa).
CREATE VIEW vw_CargaBarcoCalculada AS
SELECT
    cb.IdCalculo,
    b.Nombre AS Barco,
    al.Nombre AS Alimento,
    p.Nombre AS Presentacion,
    cb.Fecha,
    cb.NumeroBolsas,
    cb.NumeroBolsas * p.PesoPromedioPorBolsaKg AS PesoTotalKgEstimado
FROM CalculoCargaBarco cb
JOIN Barco b ON b.IdBarco = cb.IdBarco
JOIN Alimento al ON al.IdAlimento = cb.IdAlimento
JOIN PresentacionAlimento p ON p.IdPresentacion = cb.IdPresentacion;
GO
