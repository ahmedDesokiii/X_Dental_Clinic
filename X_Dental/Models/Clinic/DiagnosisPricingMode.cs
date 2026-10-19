namespace Dental.Models.Clinic
{
    // ⚠️ PerUnit = 0 عن قصد: كل التشخيصات الموجودة هتاخد القيمة دي تلقائيًا بعد الـ Migration
    public enum DiagnosisPricingMode
    {
        PerUnit = 0,   // السعر × عدد الأسنان (Crown, Bridge, Filling...)
        PerCase = 1    // سعر ثابت للحالة مهما كان عدد الأسنان
    }
}