// Program.cs  — X-Ray Migration Tool (PatientRays_T + OrthoRays_T)
// ---------------------------------------------------------------------------
// ينقل صور الأشعة من النظام القديم إلى:
//   1) ملفات فعلية تحت wwwroot/uploads/xrays
//   2) صفوف في PatientXRays مربوطة بالمريض (والزيارة لو وُجدت بنفس التاريخ)
//   3) Migration_LegacyXRayMap لمنع التكرار (idempotent)
//
// ملاحظات مهمة من تحليل النظام القديم:
//   - paRay_Img  = thumbnail صغيرة (~70×95)
//   - paRay_SrcImg = الصورة الكاملة للعرض (~550×600)  ← دي اللي بنرحّلها كأساس
//   - لو SrcImg فاضي بنستخدم Img كبديل
//   - معظم الصور PNG (magic: 89 50 4E 47)
//
// قبل التشغيل:
//   1) تأكد إن جدول Migration_LegacyXRayMap موجود (من NewSchema)
//   2) عدّل ConnectionString و OutputRoot تحت
//   3) المرضى لازم يكونوا مترحّلين قبل كده (GD-{code} / ORT-{code})
//
// التشغيل:
//   dotnet run
// ---------------------------------------------------------------------------

using Microsoft.Data.SqlClient;

// =============================================================================
// إعدادات — عدّلها حسب بيئتك
// =============================================================================
const string ConnectionString =
    "Server=(localdb)\\mssqllocaldb;Database=X_FamilyDental_DB_v01;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

// المسار الفعلي على القرص = wwwroot/uploads/xrays بتاع الأبلكيشن
const string OutputRoot = @"D:\Projects\Dental\X_Dental\wwwroot\uploads\xrays";

// الجزء النسبي اللي بيتسجل في PatientXRays.FilePath (يبدأ من تحت wwwroot)
const string RelativeRoot = "uploads/xrays";

// true = رحّل الـ thumbnail كمان كصف منفصل (ImageKind=img)
// false = رحّل الصورة الكاملة بس (مُفضّل — أوفر وأوضح في الشاشة)
const bool ExportThumbnailToo = false;
// =============================================================================

Directory.CreateDirectory(OutputRoot);

await using var conn = new SqlConnection(ConnectionString);
await conn.OpenAsync();
await using var writeConn = new SqlConnection(ConnectionString);
await writeConn.OpenAsync();

Console.WriteLine("=== ترحيل أشعة المرضى العام (PatientRays_T) ===");
var general = await MigrateTableAsync(
    conn, writeConn,
    sourceTable: "PatientRays_T",
    idCol: "paRay_Id",
    codeCol: "patientt_Code",
    dateCol: "paRay_Date",
    fileNameCol: "paRay_FileName",
    notesCol: "paRay_Notes",
    imgCol: "paRay_Img",
    srcCol: "paRay_SrcImg",
    patientCodePrefix: "GD-",
    visitCategory: 1, // General
    mapPrefix: "P"    // في الـ Map: نميّز المصدر
);

Console.WriteLine();
Console.WriteLine("=== ترحيل أشعة التقويم (OrthoRays_T) ===");
var ortho = await MigrateTableAsync(
    conn, writeConn,
    sourceTable: "OrthoRays_T",
    idCol: "orRay_Id",
    codeCol: "ortho_Code",
    dateCol: "orRay_Date",
    fileNameCol: "orRay_FileName",
    notesCol: "orRay_Notes",
    imgCol: "orRay_Img",
    srcCol: "orRay_SrcImg",
    patientCodePrefix: "ORT-",
    visitCategory: 2, // Orthodontics
    mapPrefix: "O"
);

Console.WriteLine();
Console.WriteLine("=============================================");
Console.WriteLine($"[عام]  صفوف: {general.Processed} | صور جديدة: {general.Exported} | متخطّى: {general.Skipped} | مريض مش موجود: {general.Unmatched}");
Console.WriteLine($"[تقويم] صفوف: {ortho.Processed} | صور جديدة: {ortho.Exported} | متخطّى: {ortho.Skipped} | مريض مش موجود: {ortho.Unmatched}");
Console.WriteLine($"إجمالي صور اتنقلت: {general.Exported + ortho.Exported}");
Console.WriteLine($"مسار الملفات: {OutputRoot}");
Console.WriteLine("=============================================");

return;

// =============================================================================
// Core migration for one legacy table
// =============================================================================

static async Task<Stats> MigrateTableAsync(
    SqlConnection readConn,
    SqlConnection writeConn,
    string sourceTable,
    string idCol, string codeCol, string dateCol,
    string fileNameCol, string notesCol, string imgCol, string srcCol,
    string patientCodePrefix,
    int visitCategory,
    string mapPrefix)
{
    var stats = new Stats();

    int total = await ScalarIntAsync(readConn,
        $"SELECT COUNT(*) FROM [FamilyDB].dbo.[{sourceTable}]");
    int nullCode = await ScalarIntAsync(readConn,
        $"SELECT COUNT(*) FROM [FamilyDB].dbo.[{sourceTable}] WHERE [{codeCol}] IS NULL");

    Console.WriteLine($"إجمالي الصفوف: {total} | بدون كود مريض: {nullCode}");

    await using var cmd = new SqlCommand($"""
        SELECT [{idCol}], [{codeCol}], [{dateCol}], [{fileNameCol}], [{notesCol}], [{imgCol}], [{srcCol}]
        FROM [FamilyDB].dbo.[{sourceTable}]
        WHERE [{codeCol}] IS NOT NULL
        ORDER BY [{idCol}]
        """, readConn);

    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        int legacyId = reader.GetInt32(0);
        int patientCode = reader.GetInt32(1);
        DateTime? rayDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
        string? originalFileName = reader.IsDBNull(3) ? null : reader.GetString(3);
        string? notes = reader.IsDBNull(4) ? null : reader.GetString(4);
        byte[]? img = reader.IsDBNull(5) ? null : (byte[])reader[5];
        byte[]? srcImg = reader.IsDBNull(6) ? null : (byte[])reader[6];

        stats.Processed++;

        // الأولوية: الصورة الكاملة، ولو فاضية نستخدم الـ thumbnail
        var primary = (srcImg != null && srcImg.Length > 0) ? srcImg : img;
        if (primary == null || primary.Length == 0)
            continue;

        int? patientId = await FindPatientIdAsync(writeConn, patientCodePrefix, patientCode);
        if (patientId == null)
        {
            stats.Unmatched++;
            continue;
        }

        int? visitId = await FindVisitIdAsync(writeConn, patientId.Value, rayDate, visitCategory);

        // map key: نميّز المصدر في ImageKind عشان Ortho و General ما يتصادموش على نفس paRay_Id
        // ImageKind examples: "P-src", "P-img", "O-src", "O-img"
        // لكن جدول الـ Map PK = (paRay_Id, ImageKind) — paRay_Id لأورثو منفصل فـ OK
        // عشان نبقى آمنين مع Ortho اللي id ممكن يتكرر مع Patient:
        // نستخدم ImageKind = "{mapPrefix}-src" / "{mapPrefix}-img"

        // 1) الصورة الأساسية (Src أو Img)
        string primaryKind = (srcImg != null && srcImg.Length > 0)
            ? $"{mapPrefix}-src"
            : $"{mapPrefix}-img";

        if (await MigrateOneImageAsync(
                writeConn, legacyId, patientCode, patientId.Value, visitId,
                primaryKind, primary, rayDate, originalFileName, notes,
                patientCodePrefix))
            stats.Exported++;
        else
            stats.Skipped++;

        // 2) اختياري: الـ thumbnail كصف إضافي
        if (ExportThumbnailToo
            && srcImg != null && srcImg.Length > 0
            && img != null && img.Length > 0)
        {
            if (await MigrateOneImageAsync(
                    writeConn, legacyId, patientCode, patientId.Value, visitId,
                    $"{mapPrefix}-img", img, rayDate, originalFileName, notes,
                    patientCodePrefix))
                stats.Exported++;
            else
                stats.Skipped++;
        }

        if (stats.Processed % 100 == 0)
            Console.WriteLine($"  ... {stats.Processed} صف");
    }

    return stats;
}

// =============================================================================
// Helpers
// =============================================================================

static async Task<int> ScalarIntAsync(SqlConnection conn, string sql)
{
    await using var cmd = new SqlCommand(sql, conn);
    return (int)(await cmd.ExecuteScalarAsync())!;
}

static async Task<int?> FindPatientIdAsync(SqlConnection conn, string prefix, int code)
{
    await using var cmd = new SqlCommand(
        "SELECT TOP (1) Id FROM dbo.ERP_Patients WHERE PatientCode = @code", conn);
    cmd.Parameters.AddWithValue("@code", $"{prefix}{code}");
    var result = await cmd.ExecuteScalarAsync();
    return result == null || result == DBNull.Value ? null : (int)result;
}

static async Task<int?> FindVisitIdAsync(SqlConnection conn, int patientId, DateTime? rayDate, int category)
{
    if (rayDate == null) return null;

    await using var cmd = new SqlCommand("""
        SELECT TOP (1) Id
        FROM dbo.Visits
        WHERE PatientId = @patientId
          AND Category = @cat
          AND CAST(VisitDate AS DATE) = @rayDate
        ORDER BY Id ASC
        """, conn);
    cmd.Parameters.AddWithValue("@patientId", patientId);
    cmd.Parameters.AddWithValue("@cat", category);
    cmd.Parameters.AddWithValue("@rayDate", rayDate.Value.Date);
    var result = await cmd.ExecuteScalarAsync();
    return result == null || result == DBNull.Value ? null : (int)result;
}

static async Task<bool> AlreadyMigratedAsync(SqlConnection conn, int legacyId, string kind)
{
    // جدول الـ Map PK = (paRay_Id, ImageKind) — بنستخدمه لـ Ortho كمان بنفس الأعمدة
    await using var cmd = new SqlCommand(
        "SELECT 1 FROM dbo.Migration_LegacyXRayMap WHERE paRay_Id = @id AND ImageKind = @kind", conn);
    cmd.Parameters.AddWithValue("@id", legacyId);
    cmd.Parameters.AddWithValue("@kind", kind);
    return await cmd.ExecuteScalarAsync() != null;
}

static string DetectExt(byte[] data)
{
    if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
        return "png";
    if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
        return "jpg";
    if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
        return "bmp";
    if (data.Length >= 6 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
        return "gif";
    if (data.Length >= 4 && ((data[0] == 0x49 && data[1] == 0x49) || (data[0] == 0x4D && data[1] == 0x4D)))
        return "tif";
    return "png"; // من العينة الفعلية أغلب الصور PNG
}

static async Task<bool> MigrateOneImageAsync(
    SqlConnection conn,
    int legacyId, int patientCode, int patientId, int? visitId,
    string kind, byte[] data,
    DateTime? rayDate, string? originalFileName, string? notes,
    string patientCodePrefix)
{
    if (await AlreadyMigratedAsync(conn, legacyId, kind))
        return false;

    string ext = DetectExt(data);
    // تنظيم المجلدات: uploads/xrays/GD-1397/5_src.png
    string patientFolder = $"{patientCodePrefix}{patientCode}";
    string folderOnDisk = Path.Combine(OutputRoot, patientFolder);
    Directory.CreateDirectory(folderOnDisk);

    string generatedName = $"{legacyId}_{kind.Replace("-", "_")}.{ext}";
    string diskPath = Path.Combine(folderOnDisk, generatedName);
    await File.WriteAllBytesAsync(diskPath, data);

    // المسار النسبي اللي الأبلكيشن هيستخدمه في <img src="...">
    string relativePath = $"/{RelativeRoot}/{patientFolder}/{generatedName}";

    await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();
    try
    {
        int newXRayId;
        await using (var insertCmd = new SqlCommand("""
            INSERT INTO dbo.PatientXRays
                (PatientId, VisitId, FileName, FilePath, TakenDate, Notes, CreatedDate, IsDeleted, ToothNumber)
            OUTPUT INSERTED.Id
            VALUES (@patientId, @visitId, @fileName, @filePath, @takenDate, @notes, GETDATE(), 0, NULL)
            """, conn, tx))
        {
            insertCmd.Parameters.AddWithValue("@patientId", patientId);
            insertCmd.Parameters.AddWithValue("@visitId", (object?)visitId ?? DBNull.Value);
            insertCmd.Parameters.AddWithValue("@fileName",
                (object?)originalFileName ?? generatedName);
            insertCmd.Parameters.AddWithValue("@filePath", relativePath);
            insertCmd.Parameters.AddWithValue("@takenDate", (object?)rayDate ?? DateTime.Today);
            insertCmd.Parameters.AddWithValue("@notes", (object?)notes ?? DBNull.Value);
            newXRayId = (int)(await insertCmd.ExecuteScalarAsync())!;
        }

        await using (var mapCmd = new SqlCommand("""
            INSERT INTO dbo.Migration_LegacyXRayMap (paRay_Id, ImageKind, NewXRayId)
            VALUES (@legacyId, @kind, @newXRayId)
            """, conn, tx))
        {
            mapCmd.Parameters.AddWithValue("@legacyId", legacyId);
            mapCmd.Parameters.AddWithValue("@kind", kind);
            mapCmd.Parameters.AddWithValue("@newXRayId", newXRayId);
            await mapCmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return true;
    }
    catch
    {
        await tx.RollbackAsync();
        // لو الملف اتكتب والداتابيز فشلت، امسح الملف عشان متفضلش يتيمة
        if (File.Exists(diskPath))
        {
            try { File.Delete(diskPath); } catch { /* ignore */ }
        }
        throw;
    }
}

sealed class Stats
{
    public int Processed;
    public int Exported;
    public int Skipped;
    public int Unmatched;
}
