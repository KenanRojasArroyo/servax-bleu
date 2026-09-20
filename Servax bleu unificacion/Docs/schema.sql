-- =====================================================================
-- UNIFIED SCHEMA: ServaxBleu + Calidad de Agua & Alimentación v2
-- ORDENADO JERÁRQUICAMENTE PARA CREACIÓN DESDE CERO (SIN ALTER TABLES)
-- =====================================================================

-- 1. TABLAS INDEPENDIENTES / LEGACY
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Calidad')
BEGIN
    CREATE TABLE Calidad (
        id              INT IDENTITY(1,1) PRIMARY KEY,
        Fecha           DATE NOT NULL,
        Temperatura     FLOAT NULL,
        Oxigeno         FLOAT NULL,
        Profundidad     FLOAT NULL
    );
END
GO

CREATE TABLE Sitio (
    IdSitio     INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      VARCHAR(100) NOT NULL UNIQUE
);
GO
INSERT INTO Sitio (Nombre) VALUES ('Puerto Escondido'), ('Bajo Soledad'), ('Altamar y Sauzal');
GO

CREATE TABLE PresentacionAlimento (
    IdPresentacion          INT IDENTITY(1,1) PRIMARY KEY,
    Nombre                  VARCHAR(30) NOT NULL UNIQUE,
    PesoPromedioPorBolsaKg  DECIMAL(8,3) NOT NULL
);
GO
INSERT INTO PresentacionAlimento (Nombre, PesoPromedioPorBolsaKg) VALUES ('Fresco', 0), ('Congelado', 0);
GO

CREATE TABLE Especie (
    IdEspecie           INT IDENTITY(1,1) PRIMARY KEY,
    Nombre               VARCHAR(100) NOT NULL,
    NombreCientifico     VARCHAR(150) NULL,
    Tipo                 VARCHAR(50)  NULL,
    EsToxica             BIT NOT NULL DEFAULT 0,
    Descripcion          VARCHAR(500) NULL,
    Activo               BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE Barco (
    IdBarco              INT IDENTITY(1,1) PRIMARY KEY,
    Nombre               VARCHAR(100) NOT NULL,
    CapacidadToneladas   DECIMAL(10,2) NOT NULL,
    Estado               VARCHAR(50) NULL,
    FechaAlta            DATE NULL,
    Observaciones        VARCHAR(500) NULL
);
GO

CREATE TABLE Nutriente (
    IdNutriente     INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          VARCHAR(50) NOT NULL UNIQUE,
    UnidadMedida    VARCHAR(20) NOT NULL DEFAULT 'mg/L'
);
GO
INSERT INTO Nutriente (Nombre) VALUES ('Nitritos'), ('Nitratos'), ('Silicatos'), ('Hierro'), ('Amonio'), ('Fosfatos');
GO

CREATE TABLE CategoriaFitoplancton (
    IdCategoria     INT IDENTITY(1,1) PRIMARY KEY,
    Nombre          VARCHAR(50) NOT NULL UNIQUE 
);
GO
INSERT INTO CategoriaFitoplancton (Nombre) VALUES ('Dinoflagelados'), ('Silicoflagelados'), ('Diatomeas');
GO

-- Estaciones físicas de monitoreo de fitoplancton (en los datos reales: 1 a 5 y "El Sauzal").
-- NO son temporadas del año: es un punto de muestreo. Se modela como catálogo propio.
-- ⚠️ Pendiente confirmar con Arian si cada estación pertenece a un Sitio concreto; mientras
--    tanto no se fuerza esa relación (MuestreoFitoplancton ya guarda el IdSitio de la muestra).
CREATE TABLE EstacionMonitoreo (
    IdEstacion  INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      VARCHAR(50) NOT NULL UNIQUE
);
GO
INSERT INTO EstacionMonitoreo (Nombre) VALUES ('1'), ('2'), ('3'), ('4'), ('5'), ('El Sauzal');
GO


-- =====================================================================
-- 2. TABLAS DE PRIMER NIVEL DE DEPENDENCIA
-- =====================================================================

CREATE TABLE Corral (
    IdCorral             INT IDENTITY(1,1) PRIMARY KEY,
    IdSitio              INT NULL FOREIGN KEY REFERENCES Sitio(IdSitio), -- Asignado desde el origen
    Nombre               VARCHAR(100) NOT NULL,
    Ubicacion            VARCHAR(150) NULL,
    CapacidadMaxima      DECIMAL(10,2) NULL,
    FechaInstalacion     DATE NULL,
    Estado               VARCHAR(50) NULL,
    Observaciones        VARCHAR(500) NULL
);
GO

CREATE TABLE Alimento (
    IdAlimento              INT IDENTITY(1,1) PRIMARY KEY,
    IdPresentacionDefault   INT NULL FOREIGN KEY REFERENCES PresentacionAlimento(IdPresentacion), -- Asignado desde el origen
    Nombre                  VARCHAR(100) NOT NULL,
    TipoAlimento            VARCHAR(50) NULL,
    UnidadMedida            VARCHAR(20) NULL,
    StockActual             DECIMAL(10,2) NOT NULL DEFAULT 0,
    CostoUnitario           DECIMAL(10,2) NULL,
    Observaciones           VARCHAR(500) NULL
);
GO


-- =====================================================================
-- 3. TABLAS TRANSACCIONALES DE GRANJA (Segundo Nivel)
-- =====================================================================

CREATE TABLE InventarioPez (
    IdInventario         INT IDENTITY(1,1) PRIMARY KEY,
    IdCorral             INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    IdEspecie            INT NOT NULL FOREIGN KEY REFERENCES Especie(IdEspecie),
    Cantidad             INT NOT NULL DEFAULT 0,
    PesoPromedioKg       DECIMAL(10,2) NULL,
    Estado               VARCHAR(50) NULL,
    FechaRegistro        DATE NOT NULL DEFAULT GETDATE(),
    Observaciones        VARCHAR(500) NULL
);
GO

-- AQUÍ ESTÁ EL CAMBIO CLAVE: IdPresentacion nace con la tabla
CREATE TABLE RegistroAlimentacion (
    IdRegistro           INT IDENTITY(1,1) PRIMARY KEY,
    IdCorral             INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    IdAlimento           INT NOT NULL FOREIGN KEY REFERENCES Alimento(IdAlimento),
    IdPresentacion       INT NULL FOREIGN KEY REFERENCES PresentacionAlimento(IdPresentacion),
    Fecha                DATE NOT NULL DEFAULT GETDATE(),
    CantidadKg           DECIMAL(10,2) NOT NULL,
    Responsable          VARCHAR(100) NULL,
    Mortalidad           INT NOT NULL DEFAULT 0,
    Observaciones        VARCHAR(500) NULL
);
GO

CREATE TABLE DistribucionAlimento (
    IdDistribucion       INT IDENTITY(1,1) PRIMARY KEY,
    IdBarco              INT NOT NULL FOREIGN KEY REFERENCES Barco(IdBarco),
    IdAlimento           INT NOT NULL FOREIGN KEY REFERENCES Alimento(IdAlimento),
    IdCorral             INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    Fecha                DATETIME NOT NULL DEFAULT GETDATE(),
    CantidadToneladas    DECIMAL(10,2) NOT NULL,
    FormulaAplicada      VARCHAR(200) NULL,
    Observaciones        VARCHAR(500) NULL
);
GO


-- =====================================================================
-- 4. TABLAS TRANSACCIONALES DE CALIDAD DE AGUA Y ALIMENTACIÓN v2
-- =====================================================================

CREATE TABLE Sensor (
    IdSensor                INT IDENTITY(1,1) PRIMARY KEY,
    NumeroSensor            VARCHAR(20) NOT NULL, 
    Marca                   VARCHAR(50) NOT NULL, 
    IdCorral                INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    ProfundidadInstalacion  DECIMAL(5,2) NOT NULL, 
    Activo                  BIT NOT NULL DEFAULT 1
);
GO

CREATE TABLE LecturaSensorCorral (
    IdLectura               INT IDENTITY(1,1) PRIMARY KEY,
    IdCorral                INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    Fecha                   DATE NOT NULL,
    Profundidad             DECIMAL(5,2) NOT NULL,
    MetodoCaptura           VARCHAR(20) NOT NULL CHECK (MetodoCaptura IN ('Sensor', 'Manual')),
    IdSensor                INT NULL FOREIGN KEY REFERENCES Sensor(IdSensor),
    Temperatura             FLOAT NULL,
    OxigenoMgL              FLOAT NULL,
    SaturacionOxigenoPct    FLOAT NULL,
    CONSTRAINT UQ_LecturaSensor UNIQUE (IdCorral, Fecha, Profundidad, MetodoCaptura)
);
GO

CREATE TABLE MuestreoAbiotico (
    IdMuestreo          INT IDENTITY(1,1) PRIMARY KEY,
    IdSitio             INT NOT NULL FOREIGN KEY REFERENCES Sitio(IdSitio),
    Fecha               DATE NOT NULL,
    Turno               VARCHAR(10) NOT NULL CHECK (Turno IN ('Mañana', 'Tarde', 'Noche')),
    OxigenoDisueltoMgL  DECIMAL(6,2) NULL,
    TurbidezM           DECIMAL(6,2) NULL,
    Observaciones       VARCHAR(300) NULL,
    CONSTRAINT UQ_MuestreoAbiotico UNIQUE (IdSitio, Fecha, Turno)
);
GO

CREATE TABLE MuestreoNutriente (
    IdMuestreo      INT NOT NULL FOREIGN KEY REFERENCES MuestreoAbiotico(IdMuestreo),
    IdNutriente     INT NOT NULL FOREIGN KEY REFERENCES Nutriente(IdNutriente),
    Valor           DECIMAL(8,3) NULL,
    PRIMARY KEY (IdMuestreo, IdNutriente)
);
GO

CREATE TABLE MuestreoFitoplancton (
    IdMuestreoFito              INT IDENTITY(1,1) PRIMARY KEY,
    IdSitio                     INT NOT NULL FOREIGN KEY REFERENCES Sitio(IdSitio),
    IdEstacion                  INT NOT NULL FOREIGN KEY REFERENCES EstacionMonitoreo(IdEstacion),
    Fecha                       DATE NOT NULL,
    AbundanciaTotalCelulasL     DECIMAL(14,2) NULL,
    Especie                     VARCHAR(100) NULL,
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

CREATE TABLE CalculoCargaBarcoCorral (
    IdCalculo               INT NOT NULL FOREIGN KEY REFERENCES CalculoCargaBarco(IdCalculo),
    IdCorral                INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    ToneladasAsignadas      DECIMAL(10,3) NOT NULL,
    ReporteBuceoAjuste      VARCHAR(20) NULL CHECK (ReporteBuceoAjuste IN ('Subir', 'Bajar', 'Sin cambio')),
    Observaciones           VARCHAR(300) NULL,
    PRIMARY KEY (IdCalculo, IdCorral)
);
GO

CREATE TABLE HistorialCorral (
    IdHistorial          INT IDENTITY(1,1) PRIMARY KEY,
    IdCorral             INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    Fecha                DATETIME NOT NULL DEFAULT GETDATE(),
    CantidadPeces        INT NULL,
    EstadoGeneral        VARCHAR(50) NULL,
    ResumenCalidadAgua   VARCHAR(200) NULL,
    ResumenNutrientes    VARCHAR(200) NULL,
    Observaciones        VARCHAR(500) NULL
);
GO

-- Índices de apoyo 
CREATE INDEX IX_RegistroAlimentacion_Corral_Fecha ON RegistroAlimentacion(IdCorral, Fecha);
CREATE INDEX idx_LecturaSensor_Corral_Fecha ON LecturaSensorCorral(IdCorral, Fecha);
GO

-- =====================================================================
-- 5. VISTAS
-- =====================================================================

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

CREATE VIEW vw_AbioticoFitoplancton AS
SELECT
    s.Nombre AS Sitio,
    a.Fecha,
    AVG(a.OxigenoDisueltoMgL) AS OxigenoPromedioDia,
    AVG(a.TurbidezM) AS TurbidezPromedioDia,
    f.AbundanciaTotalCelulasL,
    e.Nombre AS Estacion
FROM MuestreoAbiotico a
JOIN Sitio s ON s.IdSitio = a.IdSitio
LEFT JOIN MuestreoFitoplancton f ON f.IdSitio = a.IdSitio AND f.Fecha = a.Fecha
LEFT JOIN EstacionMonitoreo e ON e.IdEstacion = f.IdEstacion
GROUP BY s.Nombre, a.Fecha, f.AbundanciaTotalCelulasL, e.Nombre;
GO

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
GO-- Ver Docs/archivo/ para las versiones previas (schema_ServaxBleu.sql, schema_CalidadAgua_Alimentacion_v2.sql, migration_EstacionMonitoreo.sql), conservadas como historial.
