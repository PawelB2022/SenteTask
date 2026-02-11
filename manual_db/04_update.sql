-- 1) Dodaj nową domenę
CREATE DOMAIN D_STATUS VARCHAR(20);

-- 2) Zmodyfikuj tabelę: dodaj nową kolumnę
ALTER TABLE USERS ADD STATUS D_STATUS;

-- 3) Zmień procedurę w sposób bezpieczny dla aktualizacji
--    (CREATE OR ALTER jest wygodne do powtarzalnego update)
SET TERM ^ ;
CREATE OR ALTER PROCEDURE GET_USER_COUNT
RETURNS (
    CNT INTEGER
)
AS
BEGIN
    SELECT COUNT(*) FROM USERS INTO :CNT;
    SUSPEND;
END^
SET TERM ; ^
