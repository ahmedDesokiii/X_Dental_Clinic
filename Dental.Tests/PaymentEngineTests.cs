using Dental.Data;
using Dental.Models.Clinic;
using Dental.Models.Patient;
using Dental.Modules.Patients.DTOs;
using Dental.Modules.Patients.Enums;
using Dental.Modules.Payments.DTOs;
using Dental.Modules.Payments.ServicesPayments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Dental.Tests
{
    /// <summary>
    /// اختبارات محرك الدفع الموحّد (PaymentService.AllocateAsync / AddPaymentAsync / VoidPaymentAsync).
    /// بتشتغل على داتابيز في الميموري — مش بتلمس داتابيز العيادة.
    /// تشغيلها: Test → Run All Tests في Visual Studio، أو: dotnet test
    /// </summary>
    public class PaymentEngineTests
    {
        private static ApplicationDbContext NewDb() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options);

        private static Patient AddPatient(ApplicationDbContext db)
        {
            var p = new Patient { PatientCode = "P-1", FullNameAr = "مريض تجربة", Mobile = "01000000000" };
            db.Patients.Add(p);
            db.SaveChanges();
            return p;
        }

        private static Visit AddVisit(ApplicationDbContext db, Patient p, DateTime date, decimal total,
            decimal paid = 0, VisitCategory cat = VisitCategory.General)
        {
            var v = new Visit { PatientId = p.Id, VisitDate = date, TotalAmount = total, PaidAmount = paid, Category = cat };
            db.Visits.Add(v);
            db.SaveChanges();
            return v;
        }

        [Fact]
        public async Task Payment_within_due_goes_to_target_only()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var older = AddVisit(db, p, DateTime.Today.AddDays(-10), 500);
            var today = AddVisit(db, p, DateTime.Today, 1000);
            var svc = new PaymentService(db);

            var r = await svc.AllocateAsync(today, 400, 0, PaymentMethod.Cash, null);
            await db.SaveChangesAsync();

            Assert.Equal(400, today.PaidAmount);
            Assert.Equal(0, older.PaidAmount);
            Assert.Equal(0, r.SettledOld);
            Assert.Equal(0, r.Credit);
            Assert.StartsWith("REC-", r.ReceiptNo);
            Assert.Single(db.VisitPayments);
        }

        [Fact]
        public async Task Overpayment_settles_older_visits_oldest_first_same_category_only()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var oldest = AddVisit(db, p, DateTime.Today.AddDays(-30), 300);
            var middle = AddVisit(db, p, DateTime.Today.AddDays(-15), 300);
            var ortho = AddVisit(db, p, DateTime.Today.AddDays(-20), 1000, cat: VisitCategory.Orthodontics);
            var today = AddVisit(db, p, DateTime.Today, 200);
            var svc = new PaymentService(db);

            // 200 للنهارده + 300 للأقدم + 100 من اللي بعده
            var r = await svc.AllocateAsync(today, 600, 0, PaymentMethod.Cash, null);
            await db.SaveChangesAsync();

            Assert.Equal(200, today.PaidAmount);
            Assert.Equal(300, oldest.PaidAmount);
            Assert.Equal(100, middle.PaidAmount);
            Assert.Equal(0, ortho.PaidAmount);          // التقويم مايتلمسش من دفعة عامة
            Assert.Equal(400, r.SettledOld);
            Assert.Equal(0, r.Credit);

            // كل الحركات تحت نفس رقم الإيصال
            var receipts = db.VisitPayments.Select(x => x.ReceiptNo).Distinct().ToList();
            Assert.Single(receipts);
            Assert.Equal(3, db.VisitPayments.Count());
        }

        [Fact]
        public async Task Payment_beyond_all_dues_becomes_credit_on_target()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var today = AddVisit(db, p, DateTime.Today, 100);
            var svc = new PaymentService(db);

            var r = await svc.AllocateAsync(today, 150, 0, PaymentMethod.Cash, null);
            await db.SaveChangesAsync();

            Assert.Equal(150, today.PaidAmount);
            Assert.Equal(50, r.Credit);
            Assert.Equal(-50, today.Remaining);
        }

        [Fact]
        public async Task Discount_is_applied_to_target_and_recorded_on_the_payment_row()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var today = AddVisit(db, p, DateTime.Today, 1000);
            var svc = new PaymentService(db);

            await svc.AllocateAsync(today, 700, 300, PaymentMethod.Cash, null);
            await db.SaveChangesAsync();

            Assert.Equal(300, today.Discount);
            Assert.Equal(700, today.PaidAmount);
            Assert.Equal(0, today.Remaining);
            var row = Assert.Single(db.VisitPayments);
            Assert.Equal(300, row.Discount);
        }

        [Fact]
        public async Task Discount_only_payment_is_still_recorded_so_it_can_be_voided()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var today = AddVisit(db, p, DateTime.Today, 1000);
            var svc = new PaymentService(db);

            await svc.AllocateAsync(today, 0, 200, PaymentMethod.Cash, null);
            await db.SaveChangesAsync();

            var row = Assert.Single(db.VisitPayments);
            Assert.Equal(0, row.Amount);
            Assert.Equal(200, row.Discount);
        }

        [Fact]
        public async Task AddPayment_rejects_discount_larger_than_visit_due()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var today = AddVisit(db, p, DateTime.Today, 500, paid: 400);
            var svc = new PaymentService(db);

            var r = await svc.AddPaymentAsync(new AddPaymentDto { VisitId = today.Id, Amount = 0, Discount = 150 });

            Assert.False(r.Success);
            Assert.Empty(db.VisitPayments);
        }

        [Fact]
        public async Task AddPayment_rejects_zero_amount_and_zero_discount()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var today = AddVisit(db, p, DateTime.Today, 500);
            var svc = new PaymentService(db);

            var r = await svc.AddPaymentAsync(new AddPaymentDto { VisitId = today.Id, Amount = 0, Discount = 0 });

            Assert.False(r.Success);
        }

        [Fact]
        public async Task Void_reverts_the_whole_receipt_across_visits_including_discount()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var oldest = AddVisit(db, p, DateTime.Today.AddDays(-30), 300);
            var today = AddVisit(db, p, DateTime.Today, 500);
            var svc = new PaymentService(db);

            var add = await svc.AddPaymentAsync(new AddPaymentDto { VisitId = today.Id, Amount = 700, Discount = 100 });
            Assert.True(add.Success);
            Assert.Equal(400, today.PaidAmount);
            Assert.Equal(300, oldest.PaidAmount);
            Assert.Equal(100, today.Discount);

            var anyRow = db.VisitPayments.First();
            var v = await svc.VoidPaymentAsync(anyRow.Id, "تجربة");

            Assert.True(v.Success);
            Assert.Equal(0, today.PaidAmount);
            Assert.Equal(0, oldest.PaidAmount);
            Assert.Equal(0, today.Discount);
            Assert.All(db.VisitPayments.IgnoreQueryFilters(), x => Assert.True(x.IsDeleted));
        }

        [Fact]
        public async Task Receipt_numbers_are_sequential_within_the_same_day()
        {
            using var db = NewDb();
            var p = AddPatient(db);
            var today = AddVisit(db, p, DateTime.Today, 5000);
            var svc = new PaymentService(db);

            var r1 = await svc.AllocateAsync(today, 100, 0, PaymentMethod.Cash, null);
            var r2 = await svc.AllocateAsync(today, 100, 0, PaymentMethod.Cash, null);
            await db.SaveChangesAsync();

            Assert.NotEqual(r1.ReceiptNo, r2.ReceiptNo);
        }
    }
}
