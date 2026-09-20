-- =====================================================================
-- schema_ServaxBleu.sql
-- Sistema de gestión de granja de atún (ServaxBleu)
-- Motor: Microsoft SQL Server (T-SQL)
-- Esquema confirmado contra INFORMATION_SCHEMA.COLUMNS de la base real
-- y contra los modelos/controladores de la aplicación.
-- =====================================================================

-- Calidad es una tabla legacy y protegida: nunca se modifica su estructura.
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

CREATE TABLE Corral (
    IdCorral             INT IDENTITY(1,1) PRIMARY KEY,
    Nombre               VARCHAR(100) NOT NULL,
    Ubicacion            VARCHAR(150) NULL,
    CapacidadMaxima      DECIMAL(10,2) NULL,
    FechaInstalacion     DATE NULL,
    Estado               VARCHAR(50) NULL,
    Observaciones        VARCHAR(500) NULL
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

CREATE TABLE Alimento (
    IdAlimento           INT IDENTITY(1,1) PRIMARY KEY,
    Nombre               VARCHAR(100) NOT NULL,
    TipoAlimento         VARCHAR(50) NULL,
    UnidadMedida         VARCHAR(20) NULL,
    StockActual          DECIMAL(10,2) NOT NULL DEFAULT 0,
    CostoUnitario        DECIMAL(10,2) NULL,
    Observaciones        VARCHAR(500) NULL
);
GO

-- ---------------------------------------------------------------------
-- Tablas dependientes (FK hacia las anteriores)
-- ---------------------------------------------------------------------

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

CREATE TABLE MuestreoAgua (
    IdMuestreo           INT IDENTITY(1,1) PRIMARY KEY,
    IdCorral             INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    Fecha                DATE NOT NULL DEFAULT GETDATE(),
    Temperatura          FLOAT NULL,
    Oxigeno              FLOAT NULL,
    Profundidad          FLOAT NULL,
    PH                   FLOAT NULL,
    Salinidad            FLOAT NULL,
    Nutrientes           VARCHAR(200) NULL,
    Irregularidad        VARCHAR(200) NULL,
    Observaciones        VARCHAR(500) NULL
);
GO

CREATE TABLE CrecimientoAnual (
    IdCrecimiento        INT IDENTITY(1,1) PRIMARY KEY,
    IdCorral             INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    IdEspecie            INT NOT NULL FOREIGN KEY REFERENCES Especie(IdEspecie),
    Anio                 INT NOT NULL,
    PesoPromedioInicial  DECIMAL(10,2) NULL,
    PesoPromedioFinal    DECIMAL(10,2) NULL,
    TasaCrecimiento      FLOAT NULL,
    Observaciones        VARCHAR(500) NULL
);
GO

CREATE TABLE RegistroAlimentacion (
    IdRegistro           INT IDENTITY(1,1) PRIMARY KEY,
    IdCorral             INT NOT NULL FOREIGN KEY REFERENCES Corral(IdCorral),
    IdAlimento           INT NOT NULL FOREIGN KEY REFERENCES Alimento(IdAlimento),
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

-- Índices de apoyo para el dashboard (consultas frecuentes por corral/fecha)
CREATE INDEX IX_MuestreoAgua_Corral_Fecha ON MuestreoAgua(IdCorral, Fecha);
CREATE INDEX IX_RegistroAlimentacion_Corral_Fecha ON RegistroAlimentacion(IdCorral, Fecha);
CREATE INDEX IX_HistorialCorral_Corral_Fecha ON HistorialCorral(IdCorral, Fecha);
GO
