using System.Reflection;

namespace Dental.Constants
{
    public static class EnumHelper
    {
        // ========== Enums ==========
        public enum PatientType { General = 1, Orthodontics = 2, Both = 3 }
       // public enum VisitStatus { InProgress = 1, Completed = 2, Cancelled = 3 }
        //public enum ProcedureStatus { Planned = 1, InProgress = 2, Completed = 3 }
        public enum DiagnosisCategory { Caries = 1, Periodontal = 2, Endodontic = 3, Prosthodontic = 4, Orthodontic = 5, Other = 6 }


        public static List<SelectListItem> GetEnumSelectList<T>()
        {
            return Enum.GetValues(typeof(T))
                .Cast<T>()
                .Select(e => new SelectListItem
                {
                    Value = Convert.ToInt32(e).ToString(),
                    Text = GetDisplayName(e)
                }).ToList();
        }

        private static string GetDisplayName<T>(T enumValue)
        {
            var member = enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First();

            var displayAttr = member.GetCustomAttribute<DisplayAttribute>();

            return displayAttr?.Name ?? enumValue.ToString();
        }



            public enum MedicationCategory
            {
            ANALGESIC_SYRP = 1,      
            ANALGESIC_VIAL = 2,      
            ANALGESIC_TABLET = 3,    
            ANTIBIOTIC_SYRP = 4,     
            ANTIBIOTIC_VIAL = 5,      
            ANTIBIOTIC_TABLET = 6,
            Anti_Edematus = 7,
            MOUTH_WASH = 8,
            TOOTH_PASTE = 9,      
            Vitamin = 10,        
            LocalAnesthetic = 11, 
            AntiInflammatory = 12,
            Other = 99
            
        }

            public enum TreatmentPlanStatus
            {
                Draft = 1,        // مسودة — لسه ما تعرضتش على المريض
                Approved = 2,     // المريض وافق
                InProgress = 3,   // جاري التنفيذ
                Completed = 4,    // اكتملت كل المراحل
                Cancelled = 5     // اتلغت
            }

            public enum TreatmentStepStatus
            {
                Planned = 1,     // مجدولة
                InProgress = 2,  // شغال عليها دلوقتي
                Done = 3,        // خلصت
                Skipped = 4      // اتلغت/اتخطت
            }


        public enum MaterialCategory
        {
            Consumables = 1,      // مستهلكات عامة
            Restorative = 2,       // مواد حشو وترميم
            Anesthesia = 3,         // بنج
            Impression = 4,        // مواد طبعة
            Orthodontic = 5,        // تقويم
            Endodontic = 6,          // عصب
            Surgical = 7,             // جراحة
            PPE = 8,                  // أدوات وقاية
            Sterilization = 9,        // تعقيم
            Other = 99
        }

        public enum MaterialUnit
        {
            Piece = 1, Box = 2, Bottle = 3, Pack = 4,
            Gram = 5, Kilogram = 6, Milliliter = 7, Liter = 8, Roll = 9, Set = 10
        }

        public enum StockTransactionType
        {
            StockIn = 1,
            StockOut = 2
        }

        public enum MaterialHistoryAction
        {
            Created = 1,
            Edited = 2,
            StockIn = 3,
            StockOut = 4,
            PriceChanged = 5,
            QuantityAdjusted = 6,
            ExpiryUpdated = 7,
            Deleted = 8
        }


    }
}
