/*
USE [X_FamilyDental_DB_v01];
EXEC dbo.usp_MigrateLegacyClinicData @Commit = 1, @RebuildTeeth = 1;
*/

USE [X_FamilyDental_DB_v01]
GO
/****** Object:  StoredProcedure [dbo].[usp_MigrateLegacyClinicData]    Script Date: 2026-10-31 09:20:28 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[usp_MigrateLegacyClinicData]
    @Commit       BIT = 0,
    @RebuildTeeth BIT = 0   -- 1 = يمسح ويعيد بناء الأسنان للزيارات المهاجَرة (يتشغّل مرة واحدة)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @@TRANCOUNT > 0
        THROW 50300, N'There is an open transaction in this session. COMMIT or ROLLBACK it first.', 1;

    DECLARE @Checks TABLE
    (
        Seq        INT IDENTITY(1,1),
        Check_     NVARCHAR(200) NOT NULL,
        Cnt        DECIMAL(18,2) NULL,
        IsCritical BIT           NOT NULL
    );

    DECLARE @nDiagFromLegacy INT = 0,
            @nDiagFromVisits INT = 0,
            @nContacts       INT = 0,
            @nDiagLinks      INT = 0,
            @nPayments       INT = 0,
            @nChart          INT = 0,
            @nAppts          INT = 0,
            @nOrthoTotals    INT = 0,
            @nMedicalHistory INT = 0,
            @nDiagTeethBackfilled INT = 0,
            @nGenderBackfilled    INT = 0,
            @nRebuildChartDel     INT = 0,
            @nRebuildDiagDel      INT = 0,
            @nOrthoCases          INT = 0,   -- ⭐ Ortho: حالات اتعملت في الـ run ده
            @nOrthoEntries        INT = 0,   -- ⭐ Ortho: سجلات زيارات اتعملت
            @nOrthoPayMoved       INT = 0,   -- ⭐ Ortho: دفعات اتنقلت للـ Anchor
            @nOrthoAnchorFixed    INT = 0,   -- ⭐ Ortho: Anchor اتظبط إجماليها
            @nOrthoAdopted        INT = 0;   -- ⭐ Ortho: حالات اتفتحت من السيشن واتربطت بالقديم

    -- ⭐ Ortho enums (لازم تطابق الـ C#):
    --    OrthoArch: None = 0, Upper = 1, Lower = 2, Both = 3
    --    OrthoCaseStatus.Active: ⚠️ اتأكد من القيمة في OrthoModels.cs (لو أول عضو في الـ enum ومن غير رقم = 0)
    DECLARE @OrthoArchBoth     INT = 3,
            @OrthoStatusActive INT = 1;

    DECLARE @msg NVARCHAR(200), @tStart DATETIME2 = SYSDATETIME();

    BEGIN TRANSACTION;

    SET @msg = N'>> started  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;


    /* =======================================================
       0) تنظيف الـ Mapping القديم
       ======================================================= */
    DELETE map
    FROM dbo.Migration_LegacyVisitMap map
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Visits v WHERE v.Id = map.NewVisitId);

    DELETE map
    FROM dbo.Migration_LegacyDiagnosisMap map
    WHERE NOT EXISTS (SELECT 1 FROM dbo.DiagnosisCatalogs c WHERE c.Id = map.NewDiagnosisCatalogId);

    /* =======================================================
       A) DIAGNOSES
       ======================================================= */

    -- A1) Diagonsis_T -> DiagnosisCatalogs (dedup by code + name, rerun-safe)
    SELECT
        d.Id, d.dia_Name, d.dia_Cost, d.dia_Code,
        ISNULL(CAST(d.dia_Code AS NVARCHAR(20)), N'') AS KeyCode,
        LTRIM(RTRIM(ISNULL(d.dia_Name, N'')))          AS KeyName,
        ROW_NUMBER() OVER (
            PARTITION BY ISNULL(CAST(d.dia_Code AS NVARCHAR(20)), N''),
                         LTRIM(RTRIM(ISNULL(d.dia_Name, N'')))
            ORDER BY d.Id DESC
        ) AS rn
    INTO #DiagLegacy
    FROM [FAMILYDB].dbo.Diagonsis_T d;

    SELECT
        l.*,
        ROW_NUMBER() OVER (PARTITION BY l.KeyCode ORDER BY l.Id DESC) AS rc
    INTO #DiagToInsert
    FROM #DiagLegacy l
    WHERE l.rn = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM #DiagLegacy l2
          JOIN dbo.Migration_LegacyDiagnosisMap m ON m.LegacyDiagnosisId = l2.Id
          WHERE l2.KeyCode = l.KeyCode
            AND l2.KeyName = l.KeyName
      );

    DECLARE @DiagMap TABLE (LegacyDiagnosisId INT PRIMARY KEY, NewDiagnosisCatalogId INT);

    MERGE dbo.DiagnosisCatalogs AS tgt
    USING #DiagToInsert AS src
        ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (Code, NameAr, NameEn, Category, Description, IsActive,
                CreatedDate, IsDeleted, Notes, Price, IsLaboratoryRelated)
        VALUES
        (
            CASE
                WHEN src.dia_Code IS NULL THEN N'LEGACY-' + CAST(src.Id AS NVARCHAR(20))
                WHEN src.rc = 1           THEN CAST(src.dia_Code AS NVARCHAR(20))
                ELSE CAST(src.dia_Code AS NVARCHAR(20)) + N'-' + CAST(src.Id AS NVARCHAR(20))
            END,
            ISNULL(NULLIF(src.KeyName, N''), N'غير مسمى'),
            ISNULL(NULLIF(src.KeyName, N''), N'Unnamed'),
            1,                                          -- DiagnosisCategory.Caries (مؤقتًا)
            N'مرحّل من النظام القديم (Diagonsis_T)',
            1,                                          -- IsActive
            GETDATE(), 0,
            NULL,
            ISNULL(src.dia_Cost, 0),
            0                                           -- IsLaboratoryRelated
        )
    OUTPUT src.Id, inserted.Id INTO @DiagMap (LegacyDiagnosisId, NewDiagnosisCatalogId);

    SET @nDiagFromLegacy = (SELECT COUNT(*) FROM @DiagMap);

    INSERT INTO dbo.Migration_LegacyDiagnosisMap (LegacyDiagnosisId, NewDiagnosisCatalogId)
    SELECT LegacyDiagnosisId, NewDiagnosisCatalogId
    FROM @DiagMap;

    -- باقي الصفوف القديمة (المكررة) تتسجل على نفس التشخيص بتاع مجموعتها
    INSERT INTO dbo.Migration_LegacyDiagnosisMap (LegacyDiagnosisId, NewDiagnosisCatalogId)
    SELECT l.Id, g.NewId
    FROM #DiagLegacy l
    CROSS APPLY
    (
        SELECT TOP (1) m.NewDiagnosisCatalogId AS NewId
        FROM #DiagLegacy l2
        JOIN dbo.Migration_LegacyDiagnosisMap m ON m.LegacyDiagnosisId = l2.Id
        WHERE l2.KeyCode = l.KeyCode
          AND l2.KeyName = l.KeyName
        ORDER BY l2.Id DESC
    ) g
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Migration_LegacyDiagnosisMap m3
        WHERE m3.LegacyDiagnosisId = l.Id
    );

    -- A2) أسماء تشخيصات موجودة في زيارات Patient_T ومش موجودة في الكتالوج
    DECLARE @VisitNameBase INT =
        (SELECT COUNT(*) FROM dbo.DiagnosisCatalogs WHERE Code LIKE N'LEGACY-V%');

    ;WITH VisitNames AS
    (
        SELECT DISTINCT LTRIM(RTRIM(pt.patient_DiagonsisName)) COLLATE DATABASE_DEFAULT AS Nm
        FROM [FAMILYDB].dbo.Patient_T pt
        WHERE NULLIF(LTRIM(RTRIM(pt.patient_DiagonsisName)), N'') IS NOT NULL
    )
    INSERT INTO dbo.DiagnosisCatalogs
        (Code, NameAr, NameEn, Category, Description, IsActive,
         CreatedDate, IsDeleted, Notes, Price, IsLaboratoryRelated)
    SELECT
        N'LEGACY-V' + CAST(@VisitNameBase + ROW_NUMBER() OVER (ORDER BY vn.Nm) AS NVARCHAR(20)),
        vn.Nm,
        vn.Nm,
        1,
        N'مرحّل من زيارات النظام القديم (Patient_T)',
        1,
        GETDATE(), 0,
        NULL,
        0,
        0
    FROM VisitNames vn
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.DiagnosisCatalogs dc WHERE dc.NameAr = vn.Nm
    );

    SET @nDiagFromVisits = @@ROWCOUNT;

    SET @msg = N'>> A done (diagnoses)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    /* =======================================================
       B) PATIENTS  (Patient_T => PatientType 1 , Orthodontics_T => PatientType 2)
       ======================================================= */

    -- Staging: مريض واحد لكل كود (آخر جلسة هي الـ master row) + تقسيم أرقام التليفون مرة واحدة
    SELECT
        s.SourceTable,
        s.LegacyCode,
        s.NewPatientCode,
        s.PatientType,
        s.FullName,
        s.Age,
        s.Address,
        gn.GenderId,
        LEFT(
            ISNULL(
                NULLIF(
                    LTRIM(RTRIM(
                        CASE WHEN sp.SepPos IS NULL THEN c.CleanPhone
                             ELSE LEFT(c.CleanPhone, sp.SepPos - 1) END
                    )),
                    N''
                ),
                N'N/A'
            ),
            20
        ) AS Mobile,
        LEFT(
            CASE WHEN sp.SepPos IS NULL THEN NULL
                 ELSE NULLIF(LTRIM(RTRIM(SUBSTRING(c.CleanPhone, sp.SepPos + 1, 50))), N'') END,
            20
        ) AS SecondPhone
    INTO #SrcPatients
    FROM
    (
        SELECT
            N'Patient_T'                                        AS SourceTable,
            p.patient_Code                                      AS LegacyCode,
            N'GD-' + CAST(p.patient_Code AS NVARCHAR(15))       AS NewPatientCode,
            1                                                   AS PatientType,
            p.patient_Name                                      AS FullName,
            p.patient_Age                                       AS Age,
            LTRIM(RTRIM(p.patient_Gender)) COLLATE DATABASE_DEFAULT AS Gender,
            p.patient_Adress                                    AS Address,
            p.patient_Phone                                     AS Phone,
            ROW_NUMBER() OVER (PARTITION BY p.patient_Code
                               ORDER BY p.patient_SessionDate DESC, p.patient_Id DESC) AS rn
        FROM [FAMILYDB].dbo.Patient_T p
        WHERE p.patient_Code IS NOT NULL

        UNION ALL

        SELECT
            N'Orthodontics_T',
            o.orth_Code,
            N'ORT-' + CAST(o.orth_Code AS NVARCHAR(15)),
            2,
            o.orth_Name,
            o.orth_Age,
            LTRIM(RTRIM(o.orth_Gender)) COLLATE DATABASE_DEFAULT,
            o.orth_Adress,
            o.orth_Phone,
            ROW_NUMBER() OVER (PARTITION BY o.orth_Code
                               ORDER BY o.orth_SessionDate DESC, o.orth_Id DESC)
        FROM [FAMILYDB].dbo.Orthodontics_T o
        WHERE o.orth_Code IS NOT NULL
    ) s
    CROSS APPLY (SELECT REPLACE(s.Phone, N' ', N'') AS CleanPhone) c
    CROSS APPLY
    (
        SELECT MIN(v.pos) AS SepPos
        FROM (VALUES
            (NULLIF(CHARINDEX(N'/', c.CleanPhone), 0)),
            (NULLIF(CHARINDEX(N',', c.CleanPhone), 0)),
            (NULLIF(CHARINDEX(N'-', c.CleanPhone), 0))
        ) AS v(pos)
    ) sp
    -- Gender: تنضيف (مسافات / أحرف مخفية) ثم الربط بأول حرف: M -> Male , F -> Female
    -- (مع fallback على المقارنة المباشرة بالاسم العربي/الإنجليزي/الكود)
    OUTER APPLY
    (
        SELECT UPPER(LEFT(
                   LTRIM(RTRIM(REPLACE(REPLACE(REPLACE(REPLACE(
                       ISNULL(s.Gender, N''),
                       NCHAR(160), N''), NCHAR(9), N''), NCHAR(10), N''), NCHAR(13), N''))),
                   1)) AS G1
    ) gc
    OUTER APPLY
    (
        SELECT TOP (1) g.Id AS GenderId
        FROM dbo.ERP_Genders g
        WHERE (gc.G1 = N'M' AND g.NameEn = N'Male')
           OR (gc.G1 = N'F' AND g.NameEn = N'Female')
           OR g.NameEn = s.Gender OR g.NameAr = s.Gender OR g.Code = s.Gender
        ORDER BY g.Id
    ) gn
    WHERE s.rn = 1;

    DECLARE @NewPatients TABLE
    (
        SourceTable  NVARCHAR(20) NOT NULL,
        LegacyCode   INT          NOT NULL,
        NewPatientId INT          NOT NULL,
        PRIMARY KEY (SourceTable, LegacyCode)
    );

    MERGE dbo.ERP_Patients AS tgt
    USING #SrcPatients AS src
        ON tgt.PatientCode = src.NewPatientCode
    WHEN NOT MATCHED THEN
        INSERT (PatientCode, FullNameAr, GenderId, DateOfBirth, Mobile, Address,
                IsActive, PatientType, CreatedDate, IsDeleted)
        VALUES
        (
            src.NewPatientCode,
            LEFT(ISNULL(NULLIF(LTRIM(RTRIM(src.FullName)), N''), N'غير معروف'), 100),
            src.GenderId,
            CASE WHEN src.Age BETWEEN 1 AND 119
                 THEN DATEADD(YEAR, -src.Age, CAST(GETDATE() AS DATE))
                 ELSE NULL END,
            src.Mobile,
            LEFT(NULLIF(LTRIM(RTRIM(src.Address)), N''), 300),
            1,                 -- IsActive
            src.PatientType,   -- 1 = Patient_T , 2 = Orthodontics_T
            GETDATE(), 0
        )
    OUTPUT src.SourceTable, src.LegacyCode, inserted.Id
        INTO @NewPatients (SourceTable, LegacyCode, NewPatientId);

    -- كل المرضى (الجداد + اللي كانوا موجودين) عشان ربط الزيارات
    DECLARE @PatientMap TABLE
    (
        SourceTable  NVARCHAR(20) NOT NULL,
        LegacyCode   INT          NOT NULL,
        NewPatientId INT          NOT NULL,
        PRIMARY KEY (SourceTable, LegacyCode)
    );

    INSERT INTO @PatientMap (SourceTable, LegacyCode, NewPatientId)
    SELECT s.SourceTable, s.LegacyCode, ep.Id
    FROM #SrcPatients s
    JOIN dbo.ERP_Patients ep ON ep.PatientCode = s.NewPatientCode;

    -- B.2) تصحيح رجعي للجنس: المرضى الموجودين قبل كده (اتحطوا بـ GenderId = NULL) والـ MERGE
    --      بيتخطاهم. بنحدّث بس اللي GenderId عندهم NULL عشان ما ندهسش أي تعديل يدوي.
    UPDATE ep
    SET ep.GenderId = s.GenderId
    FROM dbo.ERP_Patients ep
    JOIN #SrcPatients s
        ON s.NewPatientCode = ep.PatientCode
    WHERE ep.GenderId IS NULL
      AND s.GenderId IS NOT NULL;

    SET @nGenderBackfilled = @@ROWCOUNT;

    -- رقم التليفون التاني -> ERP_PatientContacts (للمرضى الجداد بس)
    -- (ContactTypeId = 1: تأكد إنها قيمة "تليفون" في الـ enum بتاعك)
    INSERT INTO dbo.ERP_PatientContacts
        (PatientId, ContactTypeId, Phone, IsPrimary, IsEmergency, CreatedDate, IsDeleted)
    SELECT np.NewPatientId, 1, s.SecondPhone, 0, 0, GETDATE(), 0
    FROM @NewPatients np
    JOIN #SrcPatients s
        ON s.SourceTable = np.SourceTable
       AND s.LegacyCode  = np.LegacyCode
    WHERE s.SecondPhone IS NOT NULL;

    SET @nContacts = @@ROWCOUNT;

    INSERT INTO dbo.ERP_PatientMedicalInfo (PatientId, IsPregnant, CreatedDate, IsDeleted)
    SELECT NewPatientId, 0, GETDATE(), 0
    FROM @NewPatients;

    -- B.1) ERP_Patients.Notes <- كل قيم patient_MedicalHistory المختلفة (PatientType = 1 بس)،
    --      مرتبة بترتيب أول ظهور ومضمومة في نص واحد، وبس لو Notes لسه فاضي.
    ;WITH DistinctMedicalHistory AS
    (
        SELECT
            pt.patient_Code,
            LTRIM(RTRIM(pt.patient_MedicalHistory)) AS HistoryText,
            MIN(pt.patient_SessionDate) AS FirstSeenDate
        FROM [FAMILYDB].dbo.Patient_T pt
        WHERE pt.patient_Code IS NOT NULL
          AND NULLIF(LTRIM(RTRIM(pt.patient_MedicalHistory)), N'') IS NOT NULL
        GROUP BY pt.patient_Code, LTRIM(RTRIM(pt.patient_MedicalHistory))
    ),
    AggregatedMedicalHistory AS
    (
        SELECT
            patient_Code,
            STRING_AGG(HistoryText, N' | ') WITHIN GROUP (ORDER BY FirstSeenDate ASC) AS FullHistory
        FROM DistinctMedicalHistory
        GROUP BY patient_Code
    )
    UPDATE ep
    SET ep.Notes = amh.FullHistory
    FROM dbo.ERP_Patients ep
    JOIN @PatientMap m
        ON m.SourceTable = N'Patient_T'
       AND m.NewPatientId = ep.Id
    JOIN AggregatedMedicalHistory amh
        ON amh.patient_Code = m.LegacyCode
    WHERE NULLIF(LTRIM(RTRIM(ep.Notes)), N'') IS NULL;

    SET @nMedicalHistory = @@ROWCOUNT;

    SET @msg = N'>> B done (patients)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    /* =======================================================
       C) PATIENT_T VISITS   (Category = 1)
       ======================================================= */

    DECLARE @VisitMap TABLE (LegacyPatientRowId INT PRIMARY KEY, NewVisitId INT);

    ;WITH SourceVisits AS
    (
        SELECT
            pt.patient_Id, m.NewPatientId, pt.patient_SessionDate, pt.patient_SessionNextDate,
            pt.patient_MedicalHistory, pt.patient_TeethChart, pt.patient_DiagonsisName,
            pt.patient_DiagonsisDetails, pt.patient_AmountRequired, pt.patient_AmountPaid,
            pt.patient_Discount, pt.patient_Follow
        FROM [FAMILYDB].dbo.Patient_T pt
        JOIN @PatientMap m
            ON m.SourceTable = N'Patient_T'
           AND m.LegacyCode  = pt.patient_Code
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.Migration_LegacyVisitMap map
            WHERE map.SourceTable = N'Patient_T'
              AND map.LegacyRowId = pt.patient_Id
        )
    )
    MERGE dbo.Visits AS tgt
    USING SourceVisits AS src
        ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (PatientId, Category, VisitDate, FollowUpDate, Examination, PaidAmount,
                Discount, FollowUpType, Notes, ChiefComplaint, TotalAmount, Status,
                IsActive, CreatedDate, IsDeleted)
        VALUES
        (
            src.NewPatientId,
            1,   -- VisitCategory.General
            ISNULL(src.patient_SessionDate, CAST(GETDATE() AS DATE)),
            src.patient_SessionNextDate,
            NULLIF(LTRIM(RTRIM(src.patient_DiagonsisDetails)), N''),
            ISNULL(src.patient_AmountPaid, 0),
            ISNULL(src.patient_Discount, 0),
            CASE WHEN src.patient_Follow = N'Long' THEN 2 ELSE 1 END,
            CASE WHEN NULLIF(LTRIM(RTRIM(src.patient_MedicalHistory)), N'') IS NULL
                  AND NULLIF(LTRIM(RTRIM(src.patient_TeethChart)),     N'') IS NULL
                 THEN NULL
                 ELSE LTRIM(RTRIM(CONCAT(N'تاريخ مرضي: ', src.patient_MedicalHistory,
                                         N' | تشارت الأسنان (خام): ', src.patient_TeethChart)))
            END,
            NULLIF(LTRIM(RTRIM(src.patient_DiagonsisName)), N''),
            ISNULL(src.patient_AmountRequired, 0),
            2,   -- VisitStatus.Completed (زيارات تاريخية)
            1, GETDATE(), 0
        )
    OUTPUT src.patient_Id, inserted.Id INTO @VisitMap (LegacyPatientRowId, NewVisitId);

    INSERT INTO dbo.Migration_LegacyVisitMap (SourceTable, LegacyRowId, NewVisitId)
    SELECT N'Patient_T', vm.LegacyPatientRowId, vm.NewVisitId
    FROM @VisitMap vm
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Migration_LegacyVisitMap map
        WHERE map.SourceTable = N'Patient_T'
          AND map.LegacyRowId = vm.LegacyPatientRowId
    );

    SET @msg = N'>> C done (general visits merged)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    -- C.0) Staging لتسريع C.1 و C.3: بدل ما كل زيارة تعمل lookup على Patient_T (عبر قاعدة بيانات
    --      تانية) وتشغّل دالة التحليل لكل صف، بنعمل: نسخة مفهرسة من Patient_T مرة واحدة،
    --      شارت صالح لكل زيارة، وتحليل كل نص شارت مختلف (DISTINCT) مرة واحدة بس.
    SELECT p.patient_Id, p.patient_Code, p.patient_SessionDate,
           NULLIF(LTRIM(RTRIM(p.patient_TeethChart)), N'') AS TeethChart
    INTO #PT
    FROM [FAMILYDB].dbo.Patient_T p
    WHERE p.patient_Code IS NOT NULL;

    CREATE CLUSTERED INDEX IX_PT_Code ON #PT (patient_Code, patient_SessionDate, patient_Id);

    -- ⭐ (تعديل 11) شارت الزيارة نفسها الأول. لو فاضي بس -> أقرب شارت قبلها من صف تاني
    --    (p2.patient_Id <> pt.patient_Id) من غير ما ياخد صف أحدث منها في نفس اليوم.
    SELECT map.NewVisitId,
           COALESCE(pt.TeethChart, fwd.LastChart) AS LastChart
    INTO #VisitChart0
    FROM #PT pt
    JOIN dbo.Migration_LegacyVisitMap map
        ON map.SourceTable = N'Patient_T'
       AND map.LegacyRowId = pt.patient_Id
    OUTER APPLY
    (
        SELECT TOP (1) p2.TeethChart AS LastChart
        FROM #PT p2
        WHERE pt.TeethChart IS NULL
          AND p2.patient_Code = pt.patient_Code
          AND p2.patient_Id <> pt.patient_Id
          AND ISNULL(p2.patient_SessionDate, '1900-01-01') <= ISNULL(pt.patient_SessionDate, '9999-12-31')
          AND p2.TeethChart IS NOT NULL
        ORDER BY p2.patient_SessionDate DESC, p2.patient_Id DESC
    ) fwd;

    SELECT IDENTITY(INT, 1, 1) AS ChartId, x.LastChart
    INTO #Charts
    FROM (SELECT DISTINCT LastChart FROM #VisitChart0 WHERE LastChart IS NOT NULL) x;

    SELECT v0.NewVisitId, c.ChartId
    INTO #VisitChart
    FROM #VisitChart0 v0
    LEFT JOIN #Charts c ON c.LastChart = v0.LastChart;

    CREATE CLUSTERED INDEX IX_VisitChart ON #VisitChart (NewVisitId);

    -- تحليل نص الشارت لأرقام أسنان (FDI)، مرة واحدة لكل نص مختلف. الربع: UL=1 UR=2 LL=3 LR=4 (LR#8 = 48)
    --   بيتعامل مع: U.L / U. L / UL / Upper Left / Upper-Left ، أي مسافات أو نقط حوالين الـ # ،
    --   أحرف مخفية (NBSP/Tab/CR/LF)، فواصل , ; / - . مسافة ، وقوائم زي LR#6,7 -> 46,47
    --   (الرقم المفرد 1-8 بعد بادئة ربع بيورث نفس الربع). الأرقام العادية بتفضل زي ما هي،
    --   والقيم زي "No Tooth" / "0" بتتفلتر.
    SELECT TOP (1000) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS n
    INTO #Nums
    FROM (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) a(x)
    CROSS JOIN (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) b(x)
    CROSS JOIN (VALUES (0),(1),(2),(3),(4),(5),(6),(7),(8),(9)) c(x);

    CREATE UNIQUE CLUSTERED INDEX IX_Nums ON #Nums (n);

    SELECT c.ChartId, s7.V
    INTO #ChartNorm
    FROM #Charts c
    CROSS APPLY (SELECT UPPER(LEFT(c.LastChart, 900)) AS V) s0
    CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(s0.V,
                    NCHAR(160), N' '), NCHAR(9), N' '), NCHAR(10), N' '), NCHAR(13), N' ') AS V) s1
    CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(s1.V,
                    N'  ', N' '), N'  ', N' '), N'  ', N' '), N'  ', N' '), N'  ', N' ') AS V) s2
    CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(s2.V,
                    N'UPPER LEFT',  N'UL'), N'UPPERLEFT',  N'UL'), N'UPPER-LEFT',  N'UL'), N'UPPER_LEFT',  N'UL'),
                    N'UPPER RIGHT', N'UR'), N'UPPERRIGHT', N'UR'), N'UPPER-RIGHT', N'UR'), N'UPPER_RIGHT', N'UR') AS V) s3a
    CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(s3a.V,
                    N'LOWER LEFT',  N'LL'), N'LOWERLEFT',  N'LL'), N'LOWER-LEFT',  N'LL'), N'LOWER_LEFT',  N'LL'),
                    N'LOWER RIGHT', N'LR'), N'LOWERRIGHT', N'LR'), N'LOWER-RIGHT', N'LR'), N'LOWER_RIGHT', N'LR') AS V) s3b
    CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(s3b.V,
                    N'U . L', N'UL'), N'U. L', N'UL'), N'U .L', N'UL'), N'U.L', N'UL'),
                    N'U . R', N'UR'), N'U. R', N'UR'), N'U .R', N'UR'), N'U.R', N'UR') AS V) s4a
    CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(s4a.V,
                    N'L . L', N'LL'), N'L. L', N'LL'), N'L .L', N'LL'), N'L.L', N'LL'),
                    N'L . R', N'LR'), N'L. R', N'LR'), N'L .R', N'LR'), N'L.R', N'LR') AS V) s4b
    CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(s4b.V,
                    N' #', N'#'), N'.#', N'#'), N' #', N'#'), N'# ', N'#'), N'#.', N'#') AS V) s5
    CROSS APPLY (SELECT REPLACE(REPLACE(REPLACE(REPLACE(s5.V,
                    N'UL#', N',Q1:'), N'UR#', N',Q2:'), N'LL#', N',Q3:'), N'LR#', N',Q4:') AS V) s6
    CROSS APPLY (SELECT N',' + REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(s6.V,
                    N' ', N','), N'/', N','), N'-', N','), N';', N','), N'.', N','),
                    N'\', N','), N'+', N','), N'#', N','), N'_', N',') + N',' AS V) s7;

    SELECT cn.ChartId, nm.n AS Pos,
           SUBSTRING(cn.V, nm.n + 1, CHARINDEX(N',', cn.V, nm.n + 1) - nm.n - 1) AS Tk
    INTO #ChartTok
    FROM #ChartNorm cn
    JOIN #Nums nm
      ON nm.n < LEN(cn.V)
     AND SUBSTRING(cn.V, nm.n, 1) = N',';

    SELECT t.ChartId, t.Pos,
           CASE WHEN t.Tk LIKE N'Q[1-4]:%' THEN CAST(SUBSTRING(t.Tk, 2, 1) AS INT) END AS Q,
           TRY_CAST(NULLIF(LTRIM(RTRIM(
               CASE WHEN t.Tk LIKE N'Q[1-4]:%' THEN SUBSTRING(t.Tk, 4, 50) ELSE t.Tk END)), N'') AS INT) AS Num
    INTO #ChartParsed
    FROM #ChartTok t;

    ;WITH Carry AS
    (
        SELECT p.ChartId, p.Pos, p.Q, p.Num,
               MAX(CASE WHEN p.Q IS NOT NULL THEN p.Pos END)
                   OVER (PARTITION BY p.ChartId ORDER BY p.Pos
                         ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS QPos
        FROM #ChartParsed p
    ),
    Calc AS
    (
        SELECT c.ChartId,
               CASE
                   WHEN c.Num IS NULL THEN NULL
                   WHEN c.Q IS NOT NULL
                        THEN CASE WHEN c.Num BETWEEN 1 AND 8 THEN c.Q * 10 + c.Num END
                   WHEN q.Q IS NOT NULL AND c.Num BETWEEN 1 AND 8
                        THEN q.Q * 10 + c.Num
                   ELSE c.Num
               END AS ToothNumber
        FROM Carry c
        LEFT JOIN #ChartParsed q ON q.ChartId = c.ChartId AND q.Pos = c.QPos
    )
    SELECT DISTINCT ChartId, ToothNumber
    INTO #ChartTeeth
    FROM Calc
    WHERE ToothNumber IS NOT NULL
      AND ToothNumber > 0;

    CREATE CLUSTERED INDEX IX_ChartTeeth ON #ChartTeeth (ChartId);

    SET @msg = N'>> C.0 done (staging + teeth parsing)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    -- ⭐ (تعديل 12) إعادة بناء الأسنان للزيارات المهاجَرة من Patient_T بس (تشتغل مرة واحدة)
    --    زيارات الأبلكيشن (اللي مش في الـ Map) مش بتتلمس.
    --    بعدها C.1 (أ) و C.3 بيعيدوا البناء تلقائيًا لأن مفيش صفوف باقية للزيارات دي.
    IF @RebuildTeeth = 1
    BEGIN
        DELETE dce
        FROM dbo.DentalChartEntries dce
        JOIN dbo.Migration_LegacyVisitMap m
            ON m.NewVisitId = dce.VisitId
           AND m.SourceTable = N'Patient_T';

        SET @nRebuildChartDel = @@ROWCOUNT;

        DELETE vd
        FROM dbo.VisitDiagnoses vd
        JOIN dbo.Migration_LegacyVisitMap m
            ON m.NewVisitId = vd.VisitId
           AND m.SourceTable = N'Patient_T';

        SET @nRebuildDiagDel = @@ROWCOUNT;

        SET @msg = N'>> teeth rebuild: old migrated chart/diagnosis rows removed  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
        RAISERROR(@msg, 0, 1) WITH NOWAIT;
    END

    -- C.1) VisitDiagnoses: التشخيص من patient_DiagonsisName، و ToothNumber بنفس منطق تطبيع شارت
    --      الأسنان المستخدم في C.3. بيشتغل على *كل* زيارات Patient_T المهاجَرة.
    SELECT
        v.NewVisitId,
        v.patient_DiagonsisDetails,
        dc.Id AS DiagnosisCatalogId,
        teeth.ToothNumber,
        ROW_NUMBER() OVER (PARTITION BY v.NewVisitId ORDER BY teeth.ToothNumber) AS rn
    INTO #DiagnosisTeeth
    FROM
    (
        SELECT pt.patient_Id, map.NewVisitId, pt.patient_SessionDate, pt.patient_Code,
               pt.patient_DiagonsisName, pt.patient_DiagonsisDetails
        FROM [FAMILYDB].dbo.Patient_T pt
        JOIN dbo.Migration_LegacyVisitMap map
            ON map.SourceTable = N'Patient_T'
           AND map.LegacyRowId = pt.patient_Id
    ) v
    CROSS APPLY
    (
        SELECT TOP (1) c.Id
        FROM dbo.DiagnosisCatalogs c
        WHERE c.IsDeleted = 0
          AND c.NameAr = (LTRIM(RTRIM(v.patient_DiagonsisName)) COLLATE DATABASE_DEFAULT)
        ORDER BY c.Id DESC
    ) dc
    LEFT JOIN #VisitChart vc ON vc.NewVisitId = v.NewVisitId
    -- أرقام الأسنان الجاهزة من #ChartTeeth (OUTER APPLY: لو مفيش سن صالح -> صف واحد بـ NULL)
    OUTER APPLY
    (
        SELECT ct.ToothNumber
        FROM #ChartTeeth ct
        WHERE ct.ChartId = vc.ChartId
    ) teeth
    WHERE NULLIF(LTRIM(RTRIM(v.patient_DiagonsisName)), N'') IS NOT NULL;

    -- (أ) زيارات معندهاش أي صف VisitDiagnoses خالص -> إدراج كل صفوفها
    INSERT INTO dbo.VisitDiagnoses
        (VisitId, DiagnosisCatalogId, ToothNumber, Surface, Notes, IsPrimary,
         CreatedDate, IsDeleted, LabPriceSnapshot)
    SELECT
        dt.NewVisitId, dt.DiagnosisCatalogId, dt.ToothNumber, NULL,
        NULLIF(LTRIM(RTRIM(dt.patient_DiagonsisDetails)), N''), 1, GETDATE(), 0, NULL
    FROM #DiagnosisTeeth dt
    WHERE NOT EXISTS (SELECT 1 FROM dbo.VisitDiagnoses vd WHERE vd.VisitId = dt.NewVisitId);

    SET @nDiagLinks = @@ROWCOUNT;

    -- (ب) تصحيح رجعي: زيارات عندها صف قديم بـ ToothNumber = NULL -> نحدّث السن الأول عليه
    ;WITH ExistingNullRow AS
    (
        SELECT vd.Id AS VisitDiagnosisId, vd.VisitId,
               ROW_NUMBER() OVER (PARTITION BY vd.VisitId ORDER BY vd.Id) AS rn
        FROM dbo.VisitDiagnoses vd
        WHERE vd.ToothNumber IS NULL
    )
    UPDATE vd
    SET vd.ToothNumber = dt.ToothNumber
    FROM dbo.VisitDiagnoses vd
    JOIN ExistingNullRow enr ON enr.VisitDiagnosisId = vd.Id AND enr.rn = 1
    JOIN #DiagnosisTeeth dt ON dt.NewVisitId = vd.VisitId AND dt.rn = 1
    WHERE dt.ToothNumber IS NOT NULL;

    SET @nDiagTeethBackfilled = @@ROWCOUNT;

    -- وإدراج أي أسنان زيادة (rn > 1)
    INSERT INTO dbo.VisitDiagnoses
        (VisitId, DiagnosisCatalogId, ToothNumber, Surface, Notes, IsPrimary,
         CreatedDate, IsDeleted, LabPriceSnapshot)
    SELECT
        dt.NewVisitId, dt.DiagnosisCatalogId, dt.ToothNumber, NULL,
        NULLIF(LTRIM(RTRIM(dt.patient_DiagonsisDetails)), N''), 1, GETDATE(), 0, NULL
    FROM #DiagnosisTeeth dt
    WHERE dt.rn > 1
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.VisitDiagnoses vd2
          WHERE vd2.VisitId = dt.NewVisitId AND vd2.ToothNumber = dt.ToothNumber
      );

    SET @nDiagLinks = @nDiagLinks + @@ROWCOUNT;

    SET @msg = N'>> C.1 done (visit diagnoses)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    -- C.2) Payments
    INSERT INTO dbo.VisitPayments
        (VisitId, Amount, Method, Description, PaymentDate, CreatedDate, IsDeleted, Discount)
    SELECT
        vm.NewVisitId,
        pt.patient_AmountPaid,
        0,
        N'مرحّل من النظام القديم (Patient_T)',
        ISNULL(pt.patient_SessionDate, CAST(GETDATE() AS DATE)),
        GETDATE(), 0,
        ISNULL(pt.patient_Discount, 0)
    FROM [FAMILYDB].dbo.Patient_T pt
    JOIN @VisitMap vm ON vm.LegacyPatientRowId = pt.patient_Id
    WHERE ISNULL(pt.patient_AmountPaid, 0) > 0;

    SET @nPayments = @@ROWCOUNT;

    SET @msg = N'>> C.2 done (payments)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    -- C.3) DentalChartEntries: سن لكل صف، Status = 2 (Decayed)، Notes = اسم التشخيص
    INSERT INTO dbo.DentalChartEntries
        (PatientId, VisitId, ToothNumber, Status, Notes, [Date], CreatedDate, IsDeleted)
    SELECT DISTINCT
        v.PatientId,
        vm.NewVisitId,
        teeth.ToothNumber,
        2,   -- ToothStatus.Decayed
        dc.NameAr,
        v.VisitDate,
        GETDATE(),
        0
    FROM [FAMILYDB].dbo.Patient_T pt
    JOIN dbo.Migration_LegacyVisitMap vm
        ON vm.SourceTable = N'Patient_T'
       AND vm.LegacyRowId = pt.patient_Id
    JOIN dbo.Visits v ON v.Id = vm.NewVisitId
    JOIN #VisitChart vc ON vc.NewVisitId = vm.NewVisitId
    -- أرقام الأسنان الجاهزة من #ChartTeeth (CROSS APPLY: مفيش سن صالح -> مفيش صف)
    CROSS APPLY
    (
        SELECT ct.ToothNumber
        FROM #ChartTeeth ct
        WHERE ct.ChartId = vc.ChartId
    ) teeth
    OUTER APPLY
    (
        SELECT TOP (1) c.NameAr
        FROM dbo.DiagnosisCatalogs c
        WHERE c.IsDeleted = 0
          AND c.NameAr = (LTRIM(RTRIM(pt.patient_DiagonsisName)) COLLATE DATABASE_DEFAULT)
        ORDER BY c.Id DESC
    ) dc
    WHERE teeth.ToothNumber IS NOT NULL
      AND teeth.ToothNumber > 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.DentalChartEntries dce
          WHERE dce.VisitId = vm.NewVisitId
            AND dce.ToothNumber = teeth.ToothNumber
      );

    SET @nChart = @@ROWCOUNT;

    SET @msg = N'>> C.3 done (dental chart)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    -- C.4) Next Session: واحدة لكل مريض = أحدث تاريخ مستقبلي
    ;WITH RankedNext AS
    (
        SELECT
            pt.patient_Code,
            pt.patient_Id,
            pt.patient_SessionNextDate,
            ROW_NUMBER() OVER (PARTITION BY pt.patient_Code
                               ORDER BY pt.patient_SessionNextDate DESC, pt.patient_Id DESC) AS rn
        FROM [FAMILYDB].dbo.Patient_T pt
        WHERE pt.patient_Code IS NOT NULL
          AND pt.patient_SessionNextDate >= CAST(GETDATE() AS DATE)
    )
    INSERT INTO dbo.Appointments
        (PatientId, AppointmentDate, Duration, Reason, Status, VisitId, IsReminderSent, CreatedDate, IsDeleted)
    SELECT
        m.NewPatientId,
        CAST(r.patient_SessionNextDate AS DATETIME2),
        '00:30:00',
        N'متابعة (مرحّلة من النظام القديم)',
        1,   -- AppointmentStatus.Scheduled
        vm.NewVisitId,
        0, GETDATE(), 0
    FROM RankedNext r
    JOIN @PatientMap m
        ON m.SourceTable = N'Patient_T'
       AND m.LegacyCode  = r.patient_Code
    JOIN @VisitMap vm ON vm.LegacyPatientRowId = r.patient_Id
    WHERE r.rn = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Appointments a
          WHERE a.PatientId = m.NewPatientId
            AND a.Reason = N'متابعة (مرحّلة من النظام القديم)'
      );

    SET @nAppts = @@ROWCOUNT;

    SET @msg = N'>> C.4 done (general next sessions)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    /* =======================================================
       D) ORTHODONTICS_T VISITS   (Category = 2)
          + ⭐ موديول التقويم الجديد:
            OrthoCases (حالة لكل مريض) / OrthoVisitEntries (سجل لكل زيارة)
            والاتفاق + كل المدفوعات على الـ Anchor Visit (أول زيارة)
       ======================================================= */

    -- D.0) ⭐ Staging: نسخة مفهرسة من Orthodontics_T مرة واحدة بدل القراءة من قاعدة تانية في كل خطوة
    SELECT o.orth_Id, o.orth_Code, o.orth_SessionDate, o.orth_NextSessionDate,
           o.orth_Comments, o.orth_RequiredAmount, o.orth_PaidAmount,
           o.orth_Discount, o.orth_Follow,
           o.orth_TotalAmount
    INTO #OT
    FROM [FAMILYDB].dbo.Orthodontics_T o
    WHERE o.orth_Code IS NOT NULL;

    CREATE UNIQUE CLUSTERED INDEX IX_OT_Id ON #OT (orth_Id);

    DECLARE @VisitMapOrtho TABLE (LegacyRowId INT PRIMARY KEY, NewVisitId INT);

    ;WITH SourceOrthoVisits AS
    (
        SELECT
            ot.orth_Id, m.NewPatientId, ot.orth_SessionDate, ot.orth_NextSessionDate,
            ot.orth_Comments, ot.orth_RequiredAmount, ot.orth_PaidAmount,
            ot.orth_Discount, ot.orth_Follow
        FROM #OT ot
        JOIN @PatientMap m
            ON m.SourceTable = N'Orthodontics_T'
           AND m.LegacyCode  = ot.orth_Code
        WHERE NOT EXISTS
        (
            SELECT 1
            FROM dbo.Migration_LegacyVisitMap map
            WHERE map.SourceTable = N'Orthodontics_T'
              AND map.LegacyRowId = ot.orth_Id
        )
    )
    MERGE dbo.Visits AS tgt
    USING SourceOrthoVisits AS src
        ON 1 = 0
    WHEN NOT MATCHED THEN
        INSERT (PatientId, Category, VisitDate, FollowUpDate, Examination, PaidAmount,
                Discount, FollowUpType, Notes, ChiefComplaint, TotalAmount, Status,
                IsActive, CreatedDate, IsDeleted)
        VALUES
        (
            src.NewPatientId,
            2,   -- VisitCategory.Orthodontics
            ISNULL(src.orth_SessionDate, CAST(GETDATE() AS DATE)),
            src.orth_NextSessionDate,
            NULLIF(LTRIM(RTRIM(src.orth_Comments)), N''),
            ISNULL(src.orth_PaidAmount, 0),
            ISNULL(src.orth_Discount, 0),
            CASE WHEN src.orth_Follow = N'Long' THEN 2 ELSE 1 END,
            NULL,
            N'تقويم أسنان',
            ISNULL(src.orth_RequiredAmount, 0),
            2,   -- VisitStatus.Completed
            1, GETDATE(), 0
        )
    OUTPUT src.orth_Id, inserted.Id INTO @VisitMapOrtho (LegacyRowId, NewVisitId);

    INSERT INTO dbo.Migration_LegacyVisitMap (SourceTable, LegacyRowId, NewVisitId)
    SELECT N'Orthodontics_T', vm.LegacyRowId, vm.NewVisitId
    FROM @VisitMapOrtho vm
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Migration_LegacyVisitMap map
        WHERE map.SourceTable = N'Orthodontics_T'
          AND map.LegacyRowId = vm.LegacyRowId
    );

    -- الدفعات بتتسجل على زيارتها الأول (بتاريخها الحقيقي)، وبعدين D.4 بينقلها للـ Anchor
    INSERT INTO dbo.VisitPayments
        (VisitId, Amount, Method, Description, PaymentDate, CreatedDate, IsDeleted, Discount)
    SELECT
        vm.NewVisitId,
        ot.orth_PaidAmount,
        0,
        N'مرحّل من النظام القديم (Orthodontics_T)',
        ISNULL(ot.orth_SessionDate, CAST(GETDATE() AS DATE)),
        GETDATE(), 0,
        ISNULL(ot.orth_Discount, 0)
    FROM #OT ot
    JOIN @VisitMapOrtho vm ON vm.LegacyRowId = ot.orth_Id
    WHERE ISNULL(ot.orth_PaidAmount, 0) > 0;

    SET @nPayments = @nPayments + @@ROWCOUNT;

    -- Next Session للتقويم: واحدة لكل مريض (أحدث تاريخ مستقبلي)
    ;WITH RankedNextOrtho AS
    (
        SELECT
            ot.orth_Code,
            ot.orth_Id,
            ot.orth_NextSessionDate,
            ROW_NUMBER() OVER (PARTITION BY ot.orth_Code
                               ORDER BY ot.orth_NextSessionDate DESC, ot.orth_Id DESC) AS rn
        FROM #OT ot
        WHERE ot.orth_NextSessionDate >= CAST(GETDATE() AS DATE)
    )
    INSERT INTO dbo.Appointments
        (PatientId, AppointmentDate, Duration, Reason, Status, VisitId, IsReminderSent, CreatedDate, IsDeleted)
    SELECT
        m.NewPatientId,
        CAST(r.orth_NextSessionDate AS DATETIME2),
        '00:30:00',
        N'متابعة تقويم (مرحّلة من النظام القديم)',
        1,   -- AppointmentStatus.Scheduled
        vm.NewVisitId,
        0, GETDATE(), 0
    FROM RankedNextOrtho r
    JOIN @PatientMap m
        ON m.SourceTable = N'Orthodontics_T'
       AND m.LegacyCode  = r.orth_Code
    JOIN @VisitMapOrtho vm ON vm.LegacyRowId = r.orth_Id
    WHERE r.rn = 1
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.Appointments a
          WHERE a.PatientId = m.NewPatientId
            AND a.Reason = N'متابعة تقويم (مرحّلة من النظام القديم)'
      );

    SET @nAppts = @nAppts + @@ROWCOUNT;

    SET @msg = N'>> D done (ortho visits)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    -- D.1) ⭐ كل زيارات التقويم المهاجَرة (كل الـ runs) مرتبة لكل مريض — أول زيارة = Anchor
    SELECT
        v.Id        AS VisitId,
        v.PatientId,
        v.VisitDate,
        ot.orth_Comments,
        -- ⭐ الإجمالي من orth_TotalAmount، ولو صفر/NULL بنرجع للمطلوب القديم (سلوك الـ SP الحالي)
        CAST(COALESCE(NULLIF(ot.orth_TotalAmount, 0), ot.orth_RequiredAmount, 0) AS DECIMAL(18,2)) AS RequiredAmount,
        ROW_NUMBER() OVER (PARTITION BY v.PatientId ORDER BY v.VisitDate ASC, v.Id ASC) AS rn
    INTO #OrthoVis
    FROM dbo.Migration_LegacyVisitMap map
    JOIN #OT ot        ON ot.orth_Id = map.LegacyRowId
    JOIN dbo.Visits v  ON v.Id = map.NewVisitId
    WHERE map.SourceTable = N'Orthodontics_T';

    CREATE UNIQUE CLUSTERED INDEX IX_OrthoVis ON #OrthoVis (PatientId, rn);
    CREATE UNIQUE NONCLUSTERED INDEX IX_OrthoVis_Visit ON #OrthoVis (VisitId);

    -- D.2) ⭐ OrthoCases: حالة واحدة لكل مريض تقويم مهاجَر (لو مالوش حالة أصلاً)
    --      ArchType = Both ، Total = أول قيمة مطلوب > 0 بترتيب التاريخ ، Anchor = أول زيارة
    INSERT INTO dbo.OrthoCases
    (PatientId, ArchType, TotalRequired, AnchorVisitId, StartDate, Status, Notes, IsDeleted, CreatedDate)
SELECT
    a.PatientId,
    @OrthoArchBoth,
    ISNULL(ft.FirstTotal, 0),
    a.VisitId,
    a.VisitDate,
    @OrthoStatusActive,
    N'مرحّلة من النظام القديم (Orthodontics_T)',
    0,
    GETDATE()
FROM #OrthoVis a
    OUTER APPLY
    (
        SELECT TOP (1) x.RequiredAmount AS FirstTotal
        FROM #OrthoVis x
        WHERE x.PatientId = a.PatientId
          AND x.RequiredAmount > 0
        ORDER BY x.rn
    ) ft
    WHERE a.rn = 1
      AND NOT EXISTS
      (
          SELECT 1 FROM dbo.OrthoCases c
          WHERE c.PatientId = a.PatientId
            AND c.IsDeleted = 0
      );

    SET @nOrthoCases = @@ROWCOUNT;

    -- D.2b) ⭐ حالة اتفتحت من شاشة السيشن (Anchor = زيارة جديدة) لمريض عنده تاريخ قديم:
    --       الحالة بتتربط بالقديم → Anchor = أول زيارة قديمة ، الإجمالي = الإجمالي القديم
    --       (لو القديم ملوش مبلغ مطلوب → بيفضل الإجمالي اللي اتكتب). التغيير بيتسجل في OrthoTotalChanges.
    SELECT c.Id AS CaseId, f.VisitId, f.VisitDate,
           c.TotalRequired AS OldTotal,
           ISNULL(ft.FirstTotal, c.TotalRequired) AS NewTotal
    INTO #Adopt
    FROM dbo.OrthoCases c
    JOIN #OrthoVis f    ON f.PatientId = c.PatientId AND f.rn = 1
    JOIN dbo.Visits av  ON av.Id = c.AnchorVisitId
    OUTER APPLY
    (
        SELECT TOP (1) x.RequiredAmount AS FirstTotal
        FROM #OrthoVis x
        WHERE x.PatientId = c.PatientId
          AND x.RequiredAmount > 0
        ORDER BY x.rn
    ) ft
    WHERE c.IsDeleted = 0
      AND NOT EXISTS (SELECT 1 FROM #OrthoVis ov WHERE ov.VisitId = c.AnchorVisitId)
      AND f.VisitDate <= av.VisitDate;

    INSERT INTO dbo.OrthoTotalChanges (OrthoCaseId, OldTotal, NewTotal, Reason, ChangedAt)
    SELECT a.CaseId, a.OldTotal, a.NewTotal, N'ربط الحالة بسجلات النظام القديم', GETDATE()
    FROM #Adopt a
    WHERE a.OldTotal <> a.NewTotal;

    UPDATE c
    SET c.AnchorVisitId = a.VisitId,
        c.StartDate     = a.VisitDate,
        c.TotalRequired = a.NewTotal
    FROM dbo.OrthoCases c
    JOIN #Adopt a ON a.CaseId = c.Id;

    SET @nOrthoAdopted = @@ROWCOUNT;

    -- النطاق: الحالات اللي الـ Anchor بتاعها زيارة مهاجَرة
    -- (حالة اتفتحت من الأبلكيشن على زيارة جديدة مش بتتلمس)
    SELECT c.Id AS CaseId, c.PatientId, c.AnchorVisitId, c.TotalRequired
    INTO #CaseScope
    FROM dbo.OrthoCases c
    WHERE c.IsDeleted = 0
      AND EXISTS (SELECT 1 FROM #OrthoVis ov WHERE ov.VisitId = c.AnchorVisitId);

    CREATE UNIQUE CLUSTERED INDEX IX_CaseScope ON #CaseScope (PatientId);

    -- D.3) ⭐ الإجمالي على الـ Anchor بس (= TotalRequired) وباقي زيارات التقويم المهاجَرة = 0
    UPDATE v
    SET v.TotalAmount = cs.TotalRequired
    FROM dbo.Visits v
    JOIN #CaseScope cs ON cs.AnchorVisitId = v.Id
    WHERE v.TotalAmount <> cs.TotalRequired;

    SET @nOrthoAnchorFixed = @@ROWCOUNT;

    UPDATE v
    SET v.TotalAmount = 0
    FROM dbo.Visits v
    JOIN #CaseScope cs  ON cs.PatientId = v.PatientId
    WHERE v.Category = 2          -- ⭐ كل زيارات التقويم (مهاجَرة + من الأبلكيشن)
      AND v.IsDeleted = 0
      AND v.Id <> cs.AnchorVisitId
      AND v.TotalAmount <> 0;

    SET @nOrthoTotals = @@ROWCOUNT;

    -- D.4) ⭐ المدفوعات والخصم على الـ Anchor (تاريخ الدفع الحقيقي PaymentDate مش بيتغير)
    --      rerun-safe: بعد أول run الزيارات التانية بتبقى 0 ومفيش عليها دفعات → مفيش حاجة تتنقل
    SELECT v.Id AS VisitId, cs.AnchorVisitId, v.PaidAmount, v.Discount
    INTO #OrthoMove
    FROM dbo.Visits v
    JOIN #CaseScope cs  ON cs.PatientId = v.PatientId
    WHERE v.Category = 2          -- ⭐ كل زيارات التقويم (مهاجَرة + من الأبلكيشن)
      AND v.IsDeleted = 0
      AND v.Id <> cs.AnchorVisitId
      AND (   v.PaidAmount <> 0
           OR v.Discount   <> 0
           OR EXISTS (SELECT 1 FROM dbo.VisitPayments vp WHERE vp.VisitId = v.Id));

    CREATE UNIQUE CLUSTERED INDEX IX_OrthoMove ON #OrthoMove (VisitId);

    UPDATE vp
    SET vp.VisitId = m.AnchorVisitId
    FROM dbo.VisitPayments vp
    JOIN #OrthoMove m ON m.VisitId = vp.VisitId;

    SET @nOrthoPayMoved = @@ROWCOUNT;

    UPDATE a
    SET a.PaidAmount = a.PaidAmount + s.Paid,
        a.Discount   = a.Discount   + s.Disc
    FROM dbo.Visits a
    JOIN
    (
        SELECT AnchorVisitId, SUM(PaidAmount) AS Paid, SUM(Discount) AS Disc
        FROM #OrthoMove
        GROUP BY AnchorVisitId
    ) s ON s.AnchorVisitId = a.Id;

    UPDATE v
    SET v.PaidAmount = 0,
        v.Discount   = 0
    FROM dbo.Visits v
    JOIN #OrthoMove m ON m.VisitId = v.Id;

    -- D.5) ⭐ OrthoVisitEntries: سجل لكل زيارة تقويم مهاجَرة (Arch = Both، الملاحظات = orth_Comments)
    INSERT INTO dbo.OrthoVisitEntries
        (OrthoCaseId, VisitId, Arch, OrthoProcedureId, Comment, [Date], IsDeleted)
    SELECT
        cs.CaseId,
        ov.VisitId,
        @OrthoArchBoth,
        NULL,
        ISNULL(NULLIF(LTRIM(RTRIM(ov.orth_Comments)), N''), N'زيارة تقويم (مرحّلة)'),
        ov.VisitDate,
        0
    FROM #OrthoVis ov
    JOIN #CaseScope cs ON cs.PatientId = ov.PatientId
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.OrthoVisitEntries e
        WHERE e.VisitId = ov.VisitId
          AND e.IsDeleted = 0
    );

    SET @nOrthoEntries = @@ROWCOUNT;

    SET @msg = N'>> D.1-D.5 done (ortho cases / anchor / entries)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    /* =======================================================
       E) VALIDATION
       ======================================================= */

    DECLARE @LegacyPaid DECIMAL(18,2) =
          ISNULL((SELECT SUM(CASE WHEN pt.patient_AmountPaid > 0
                                  THEN CAST(pt.patient_AmountPaid AS DECIMAL(18,2)) ELSE 0 END)
                  FROM [FAMILYDB].dbo.Patient_T pt
                  WHERE pt.patient_Code IS NOT NULL), 0)
        + ISNULL((SELECT SUM(CASE WHEN ot.orth_PaidAmount > 0
                                  THEN CAST(ot.orth_PaidAmount AS DECIMAL(18,2)) ELSE 0 END)
                  FROM #OT ot), 0);   -- ⭐ #OT = Orthodontics_T WHERE orth_Code IS NOT NULL

    DECLARE @NewPaid DECIMAL(18,2) =
          ISNULL((SELECT SUM(vp.Amount)
                  FROM dbo.VisitPayments vp
                  JOIN dbo.Migration_LegacyVisitMap map ON map.NewVisitId = vp.VisitId
                  -- ⭐ الدفعات المرحّلة بس: دفعات الأبلكيشن الجديدة ممكن تتسجل على الـ Anchor (زيارة مهاجَرة)
                  WHERE vp.Description LIKE N'مرحّل من النظام القديم%'), 0);

    -- ---- Critical checks (لازم كلها = 0) ----
    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Diagonsis_T rows NOT mapped to a catalog entry', COUNT(*), 1
    FROM #DiagLegacy l
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Migration_LegacyDiagnosisMap m WHERE m.LegacyDiagnosisId = l.Id);

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Duplicate DiagnosisCatalogs.Code values', COUNT(*), 1
    FROM (SELECT Code FROM dbo.DiagnosisCatalogs GROUP BY Code HAVING COUNT(*) > 1) d;

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Patient_T rows NOT migrated (no visit mapping)', COUNT(*), 1
    FROM [FAMILYDB].dbo.Patient_T pt
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Migration_LegacyVisitMap map
        WHERE map.SourceTable = N'Patient_T' AND map.LegacyRowId = pt.patient_Id
    );

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Orthodontics_T rows NOT migrated (no visit mapping)', COUNT(*), 1
    FROM [FAMILYDB].dbo.Orthodontics_T ot
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Migration_LegacyVisitMap map
        WHERE map.SourceTable = N'Orthodontics_T' AND map.LegacyRowId = ot.orth_Id
    );

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'General patients: last visit date differs from legacy', COUNT(*), 1
    FROM
    (
        SELECT patient_Code, MAX(patient_SessionDate) AS LastLegacy
        FROM [FAMILYDB].dbo.Patient_T
        WHERE patient_Code IS NOT NULL
        GROUP BY patient_Code
    ) l
    JOIN
    (
        SELECT ep.PatientCode, MAX(v.VisitDate) AS LastNew
        FROM dbo.Visits v
        JOIN dbo.ERP_Patients ep ON ep.Id = v.PatientId
        JOIN dbo.Migration_LegacyVisitMap map
            ON map.SourceTable = N'Patient_T'
           AND map.NewVisitId = v.Id
        WHERE ep.PatientType = 1
        GROUP BY ep.PatientCode
    ) n ON n.PatientCode = N'GD-' + CAST(l.patient_Code AS NVARCHAR(15))
    WHERE CAST(l.LastLegacy AS DATE) <> CAST(n.LastNew AS DATE);

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Ortho patients: last visit date differs from legacy', COUNT(*), 1
    FROM
    (
        SELECT orth_Code, MAX(orth_SessionDate) AS LastLegacy
        FROM #OT
        GROUP BY orth_Code
    ) l
    JOIN
    (
        SELECT ep.PatientCode, MAX(v.VisitDate) AS LastNew
        FROM dbo.Visits v
        JOIN dbo.ERP_Patients ep ON ep.Id = v.PatientId
        JOIN dbo.Migration_LegacyVisitMap map
            ON map.SourceTable = N'Orthodontics_T'
           AND map.NewVisitId = v.Id
        WHERE ep.PatientType = 2
        GROUP BY ep.PatientCode
    ) n ON n.PatientCode = N'ORT-' + CAST(l.orth_Code AS NVARCHAR(15))
    WHERE CAST(l.LastLegacy AS DATE) <> CAST(n.LastNew AS DATE);

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Migrated patients without any visit', COUNT(*), 1
    FROM @PatientMap m
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Visits v WHERE v.PatientId = m.NewPatientId);

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Patients with more than one migrated Next Session', COUNT(*), 1
    FROM
    (
        SELECT a.PatientId
        FROM dbo.Appointments a
        JOIN dbo.Migration_LegacyVisitMap map ON map.NewVisitId = a.VisitId
        GROUP BY a.PatientId
        HAVING COUNT(*) > 1
    ) x;

    -- (تعديل 12) لما @RebuildTeeth = 1 الفحص ده بيغطي كل الزيارات المهاجَرة مش بس اللي اتعملت في الـ run ده
    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Visits with a diagnosis name but no VisitDiagnoses row', COUNT(*), 1
    FROM [FAMILYDB].dbo.Patient_T pt
    JOIN dbo.Migration_LegacyVisitMap vm
        ON vm.SourceTable = N'Patient_T'
       AND vm.LegacyRowId = pt.patient_Id
    WHERE NULLIF(LTRIM(RTRIM(pt.patient_DiagonsisName)), N'') IS NOT NULL
      AND EXISTS (SELECT 1 FROM dbo.DiagnosisCatalogs c
                  WHERE c.IsDeleted = 0
                    AND c.NameAr = (LTRIM(RTRIM(pt.patient_DiagonsisName)) COLLATE DATABASE_DEFAULT))
      AND NOT EXISTS (SELECT 1 FROM dbo.VisitDiagnoses vd WHERE vd.VisitId = vm.NewVisitId);

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Payments total mismatch (legacy vs migrated): 1 = mismatch',
           CASE WHEN @LegacyPaid <> @NewPaid THEN 1 ELSE 0 END, 1;

    -- ⭐ Ortho module checks
    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Ortho patients (migrated) without an orthodontic case', COUNT(DISTINCT ov.PatientId), 1
    FROM #OrthoVis ov
    WHERE NOT EXISTS (SELECT 1 FROM dbo.OrthoCases c WHERE c.PatientId = ov.PatientId AND c.IsDeleted = 0);

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Ortho visits (migrated) without an OrthoVisitEntries row', COUNT(*), 1
    FROM #OrthoVis ov
    JOIN #CaseScope cs ON cs.PatientId = ov.PatientId
    WHERE NOT EXISTS (SELECT 1 FROM dbo.OrthoVisitEntries e WHERE e.VisitId = ov.VisitId AND e.IsDeleted = 0);

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Ortho cases: anchor TotalAmount differs from case TotalRequired', COUNT(*), 1
    FROM #CaseScope cs
    JOIN dbo.OrthoCases c ON c.Id = cs.CaseId
    JOIN dbo.Visits v     ON v.Id = cs.AnchorVisitId
    WHERE v.TotalAmount <> c.TotalRequired;

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Ortho non-anchor visits still holding money (total/paid/discount/payments)', COUNT(*), 1
    FROM #OrthoVis ov
    JOIN #CaseScope cs ON cs.PatientId = ov.PatientId
    JOIN dbo.Visits v  ON v.Id = ov.VisitId
    WHERE v.Id <> cs.AnchorVisitId
      AND (   v.TotalAmount <> 0 OR v.PaidAmount <> 0 OR v.Discount <> 0
           OR EXISTS (SELECT 1 FROM dbo.VisitPayments vp WHERE vp.VisitId = v.Id));

    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Ortho patients: TotalAmount assigned on more than one visit', COUNT(*), 1
    FROM
    (
        SELECT ep.Id
        FROM dbo.Visits v
        JOIN dbo.ERP_Patients ep ON ep.Id = v.PatientId
        WHERE ep.PatientType = 2
          AND v.TotalAmount > 0
        GROUP BY ep.Id
        HAVING COUNT(*) > 1
    ) x;

    -- مريض GenderId = NULL رغم إن الجنس في المصدر معروف (Male/Female)
    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'Patients with GenderId NULL although legacy gender is known', COUNT(*), 1
    FROM #SrcPatients s
    JOIN dbo.ERP_Patients ep ON ep.PatientCode = s.NewPatientCode
    WHERE ep.GenderId IS NULL
      AND s.GenderId IS NOT NULL;

    -- ---- Info ----
    INSERT INTO @Checks (Check_, Cnt, IsCritical)
    SELECT N'INFO legacy Patient_T rows', COUNT(*), 0 FROM [FAMILYDB].dbo.Patient_T
    UNION ALL
    SELECT N'INFO legacy Orthodontics_T rows', COUNT(*), 0 FROM [FAMILYDB].dbo.Orthodontics_T
    UNION ALL
    SELECT N'INFO legacy Patient_T rows with NULL patient_Code (skipped)', COUNT(*), 0
    FROM [FAMILYDB].dbo.Patient_T WHERE patient_Code IS NULL
    UNION ALL
    SELECT N'INFO legacy Orthodontics_T rows with NULL orth_Code (skipped)', COUNT(*), 0
    FROM [FAMILYDB].dbo.Orthodontics_T WHERE orth_Code IS NULL
    UNION ALL
    SELECT N'INFO legacy total paid', @LegacyPaid, 0
    UNION ALL
    SELECT N'INFO migrated total paid (all runs)', @NewPaid, 0
    UNION ALL
    SELECT N'THIS RUN: new patients (type 1 = general)', COUNT(*), 0
    FROM @NewPatients WHERE SourceTable = N'Patient_T'
    UNION ALL
    SELECT N'THIS RUN: new patients (type 2 = ortho)', COUNT(*), 0
    FROM @NewPatients WHERE SourceTable = N'Orthodontics_T'
    UNION ALL
    SELECT N'THIS RUN: existing patients with GenderId backfilled', @nGenderBackfilled, 0
    UNION ALL
    SELECT N'THIS RUN: secondary phone contacts', @nContacts, 0
    UNION ALL
    SELECT N'THIS RUN: ERP_Patients.Notes filled from patient_MedicalHistory', @nMedicalHistory, 0
    UNION ALL
    SELECT N'THIS RUN: catalog diagnoses from Diagonsis_T', @nDiagFromLegacy, 0
    UNION ALL
    SELECT N'THIS RUN: catalog diagnoses added from visit names', @nDiagFromVisits, 0
    UNION ALL
    SELECT N'THIS RUN: general visits', COUNT(*), 0 FROM @VisitMap
    UNION ALL
    SELECT N'THIS RUN: ortho visits', COUNT(*), 0 FROM @VisitMapOrtho
    UNION ALL
    SELECT N'THIS RUN: ortho TotalAmount duplicates corrected (zeroed)', @nOrthoTotals, 0
    UNION ALL
    SELECT N'THIS RUN: ortho cases created (OrthoCases)', @nOrthoCases, 0
    UNION ALL
    SELECT N'THIS RUN: ortho visit entries created (OrthoVisitEntries)', @nOrthoEntries, 0
    UNION ALL
    SELECT N'THIS RUN: ortho payments moved to anchor visit', @nOrthoPayMoved, 0
    UNION ALL
    SELECT N'THIS RUN: ortho anchor TotalAmount set from case', @nOrthoAnchorFixed, 0
    UNION ALL
    SELECT N'THIS RUN: app-created ortho cases linked to legacy history', @nOrthoAdopted, 0
    UNION ALL
    SELECT N'INFO ortho cases with TotalRequired = 0 (no legacy required amount)', COUNT(*), 0
    FROM #CaseScope cs WHERE cs.TotalRequired = 0
    UNION ALL
    SELECT N'THIS RUN: REBUILD removed DentalChartEntries rows', @nRebuildChartDel, 0
    UNION ALL
    SELECT N'THIS RUN: REBUILD removed VisitDiagnoses rows', @nRebuildDiagDel, 0
    UNION ALL
    SELECT N'THIS RUN: visit diagnoses linked', @nDiagLinks, 0
    UNION ALL
    SELECT N'THIS RUN: visit diagnoses backfilled with tooth number (old rows)', @nDiagTeethBackfilled, 0
    UNION ALL
    SELECT N'THIS RUN: payments', @nPayments, 0
    UNION ALL
    SELECT N'THIS RUN: dental chart tooth rows (Status = Decayed)', @nChart, 0
    UNION ALL
    SELECT N'THIS RUN: next-session appointments', @nAppts, 0
    UNION ALL
    SELECT N'INFO visits with a tooth chart but no tooth number parsed (all migrated)', COUNT(*), 0
    FROM [FAMILYDB].dbo.Patient_T pt
    JOIN dbo.Migration_LegacyVisitMap vm ON vm.SourceTable = N'Patient_T' AND vm.LegacyRowId = pt.patient_Id
    WHERE NULLIF(LTRIM(RTRIM(pt.patient_TeethChart)), N'') IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.DentalChartEntries dce WHERE dce.VisitId = vm.NewVisitId)
    UNION ALL
    SELECT N'INFO migrated patients with GenderId NULL (all)', COUNT(*), 0
    FROM #SrcPatients s
    JOIN dbo.ERP_Patients ep ON ep.PatientCode = s.NewPatientCode
    WHERE ep.GenderId IS NULL
    UNION ALL
    SELECT N'INFO new patients with Mobile = N/A', COUNT(*), 0
    FROM @NewPatients np
    JOIN dbo.ERP_Patients ep ON ep.Id = np.NewPatientId
    WHERE ep.Mobile = N'N/A'
    UNION ALL
    SELECT N'INFO new patients with a mobile that is not 11 digits', COUNT(*), 0
    FROM @NewPatients np
    JOIN dbo.ERP_Patients ep ON ep.Id = np.NewPatientId
    WHERE ep.Mobile <> N'N/A'
      AND (LEN(ep.Mobile) <> 11 OR ep.Mobile LIKE N'%[^0-9]%');

    DECLARE @Failed INT =
        (SELECT COUNT(*) FROM @Checks WHERE IsCritical = 1 AND ISNULL(Cnt, 0) <> 0);

    -- Result set 1: الفحوصات
    SELECT
        Check_,
        Cnt,
        CASE WHEN IsCritical = 1 AND ISNULL(Cnt, 0) <> 0 THEN N'FAIL'
             WHEN IsCritical = 1                          THEN N'OK'
             ELSE N'info' END AS Result
    FROM @Checks
    ORDER BY Seq;

    -- Result set 2 (بيظهر بس لو فيه صفوف قديمة ما اتنقلتش): تفاصيلها
    IF EXISTS (SELECT 1 FROM @Checks WHERE Check_ LIKE N'%NOT migrated%' AND ISNULL(Cnt, 0) <> 0)
    BEGIN
        SELECT N'Patient_T' AS SourceTable, pt.patient_Id AS LegacyRowId,
               pt.patient_Code AS LegacyCode, pt.patient_Name AS LegacyName
        FROM [FAMILYDB].dbo.Patient_T pt
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.Migration_LegacyVisitMap map
            WHERE map.SourceTable = N'Patient_T' AND map.LegacyRowId = pt.patient_Id
        )
        UNION ALL
        SELECT N'Orthodontics_T', ot.orth_Id, ot.orth_Code, ot.orth_Name
        FROM [FAMILYDB].dbo.Orthodontics_T ot
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.Migration_LegacyVisitMap map
            WHERE map.SourceTable = N'Orthodontics_T' AND map.LegacyRowId = ot.orth_Id
        );
    END

    SET @msg = N'>> E done (validation checks)  (' + CAST(DATEDIFF(SECOND, @tStart, SYSDATETIME()) AS NVARCHAR(20)) + N's)';
    RAISERROR(@msg, 0, 1) WITH NOWAIT;

    /* =======================================================
       F) COMMIT / ROLLBACK
       ======================================================= */
    IF @Commit = 1 AND @Failed = 0
    BEGIN
        COMMIT TRANSACTION;
        SELECT N'COMMITTED' AS Outcome;
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        SELECT CASE WHEN @Commit = 1
                    THEN N'DRY RUN - rolled back, nothing was saved'
                    ELSE N'ROLLED BACK - critical checks failed, nothing was saved'
               END AS Outcome;
    END
END


/* =======================================================
Ortho Diagnoses
======================================================= */
/*
DECLARE @base INT = ISNULL((SELECT MAX([SortOrder]) FROM [dbo].[OrthoProcedureCatalogs]), 0);

INSERT INTO [dbo].[OrthoProcedureCatalogs] ([NameAr], [NameEn], [IsActive], [SortOrder])
SELECT v.[Name], v.[Name], 1, @base + v.[Ord]
FROM (VALUES
    (1,  N'Low ni ti 12'),
    (2,  N'Low ni ti 14'),
    (3,  N'Low ni ti 16'),
    (4,  N'Low ni ti 18'),
    (5,  N'Low ni ti 16*22'),
    (6,  N'Low ni ti 17*25'),
    (7,  N'Low St.St 16*22'),
    (8,  N'Low St.st 17*25'),
    (9,  N'Low St.St 16*22 with power chain'),
    (10, N'Upper St.St 17-25 with power chain'),
    (11, N'Other Diagnoses')
) AS v([Ord], [Name])
ORDER BY v.[Ord];
GO
*/