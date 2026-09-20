-- =====================================================================
-- migration_EstacionMonitoreo.sql
-- Para una BD ServaxBleu que YA se creó con MuestreoFitoplancton.Estacion
-- (VARCHAR) y necesita pasar a la tabla propia EstacionMonitoreo.
-- Una BD nueva creada con unified_schema.sql ya trae esto: NO correr este script ahí.
--
-- Es re-ejecutable: cada paso revisa si ya se aplicó.
-- Motor: SQL Server (T-SQL).
--
-- ⚠️ Los valores viejos de Estacion (p. ej. 'Primavera') eran temporadas, no estaciones
--    físicas, así que NO tienen equivalente en el catálogo nuevo. Si la tabla ya tiene filas
--    el paso 3 las deja con IdEstacion NULL y el paso 4 NO borra la columna vieja hasta que
--    se asigne la estación real a cada una.
-- =====================================================================

-- 1. Catálogo de estaciones físicas (1 a 5 y "El Sauzal")
IF OBJECT_ID('EstacionMonitoreo', 'U') IS NULL
BEGIN
    CREATE TABLE EstacionMonitoreo (
        IdEstacion  INT IDENTITY(1,1) PRIMARY KEY,
        Nombre      VARCHAR(50) NOT NULL UNIQUE
    );
    INSERT INTO EstacionMonitoreo (Nombre) VALUES ('1'), ('2'), ('3'), ('4'), ('5'), ('El Sauzal');
END
GO

-- 2. Nueva FK (nullable mientras dure la migración)
IF COL_LENGTH('MuestreoFitoplancton', 'IdEstacion') IS NULL
BEGIN
    ALTER TABLE MuestreoFitoplancton ADD IdEstacion INT NULL
        CONSTRAINT FK_MuestreoFitoplancton_Estacion FOREIGN KEY REFERENCES EstacionMonitoreo(IdEstacion);
END
GO

-- 3. Si el texto viejo coincide exactamente con un nombre del catálogo ('1'..'5', 'El Sauzal'), se mapea solo.
--    (Dinámico para que el script no falle al recompilarse cuando la columna vieja ya no exista.)
IF COL_LENGTH('MuestreoFitoplancton', 'Estacion') IS NOT NULL
BEGIN
    EXEC('UPDATE f SET f.IdEstacion = e.IdEstacion
          FROM MuestreoFitoplancton f
          JOIN EstacionMonitoreo e ON e.Nombre = f.Estacion
          WHERE f.IdEstacion IS NULL;');
END
GO

-- 4. Cerrar la migración solo si no quedó ninguna fila sin estación
IF COL_LENGTH('MuestreoFitoplancton', 'Estacion') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM MuestreoFitoplancton WHERE IdEstacion IS NULL)
        PRINT 'ADVERTENCIA: hay filas de MuestreoFitoplancton sin IdEstacion. Asigna la estación real y vuelve a correr este script.';
    ELSE
    BEGIN
        ALTER TABLE MuestreoFitoplancton ALTER COLUMN IdEstacion INT NOT NULL;
        ALTER TABLE MuestreoFitoplancton DROP COLUMN Estacion;
    END
END
GO

-- 5. La vista vw_AbioticoFitoplancton leía f.Estacion: se recrea con el JOIN al catálogo
IF OBJECT_ID('vw_AbioticoFitoplancton', 'V') IS NOT NULL
    DROP VIEW vw_AbioticoFitoplancton;
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
