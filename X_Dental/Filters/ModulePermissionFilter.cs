using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dental.Filters
{
    /// <summary>
    /// 🔒 A3 — فلتر عام بيحدد الصلاحية المطلوبة لكل action من اسم الـ controller والـ action
    /// بدل ما نحط [Authorize] على أكتر من 100 action.
    /// القاعدة: القراءة (GET أو Get*/Check*/Search) = View، الكتابة = Manage،
    /// مع استثناءات واضحة للمرضى والمدفوعات.
    /// الـ Admin بيعدّي على كل حاجة (PermissionAuthorizationHandler).
    /// </summary>
    public class ModulePermissionFilter : IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor cad) return;
            if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) return;

            var user = context.HttpContext.User;
            if (user?.Identity?.IsAuthenticated != true) return; // الـ fallback policy بتتعامل معاه

            var permission = Resolve(cad.ControllerName, cad.ActionName, context.HttpContext.Request.Method);
            if (permission == null) return;

            var auth = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();
            if ((await auth.AuthorizeAsync(user, permission)).Succeeded) return;

            var req = context.HttpContext.Request;
            var wantsHtml = HttpMethods.IsGet(req.Method) &&
                            req.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase) &&
                            req.Headers["X-Requested-With"] != "XMLHttpRequest";

            context.Result = wantsHtml
                ? new ForbidResult()
                : new JsonResult(new { success = false, message = "ليس لديك صلاحية لتنفيذ هذا الإجراء" })
                { StatusCode = StatusCodes.Status403Forbidden };
        }

        public static string? Resolve(string controller, string action, string method)
        {
            bool read = HttpMethods.IsGet(method) || HttpMethods.IsHead(method) ||
                        action.StartsWith("Get", StringComparison.OrdinalIgnoreCase) ||
                        action.StartsWith("Check", StringComparison.OrdinalIgnoreCase) ||
                        action.Equals("Search", StringComparison.OrdinalIgnoreCase) ||
                        action.Equals("Modal", StringComparison.OrdinalIgnoreCase);

            switch (controller)
            {
                case "Patients":
                    return action switch
                    {
                        "Details" => Permissions.Patients.Details_Patient,
                        "Create" => Permissions.Patients.Add_Patient,
                        "Edit" or "UploadDocuments" or "DeleteDocument" => Permissions.Patients.Edit_Patient,
                        "Delete" => Permissions.Patients.Delete_Patient,
                        "NewSession" => Permissions.Clinical.Manage,
                        "GetPrescriptionsData" => Permissions.Clinical.View,
                        _ => Permissions.Patients.View_Patients
                    };

                case "Timeline":   // 🕒 F2: الخط الزمني جزء من ملف المريض
                    return Permissions.Patients.Details_Patient;

                case "Clinical":
                case "DentalRecords":
                case "Ortho":
                case "TreatmentPlans":
                    if (action == "AddPayment") return Permissions.Payments.Add;
                    if (action == "PrintPrescription") return Permissions.Clinical.View;
                    return read ? Permissions.Clinical.View : Permissions.Clinical.Manage;

                case "Appointments":
                case "Reminders":   // 💬 D3: تذكيرات واتساب = جزء من المواعيد
                    return read ? Permissions.Appointments.View : Permissions.Appointments.Manage;

                case "Payments":
                    return action switch
                    {
                        "Index" or "GetDailyReport" => Permissions.Payments.Reports,
                        "AddPayment" => Permissions.Payments.Add,
                        "VoidPayment" => Permissions.Payments.Void,
                        _ => Permissions.Payments.View
                    };

                case "Diagnosis":
                    return read ? Permissions.Diagnosis.View : Permissions.Diagnosis.Manage;
                case "Medications":
                    return read ? Permissions.Medications.View : Permissions.Medications.Manage;
                case "Laboratory":
                    return read ? Permissions.Laboratory.View : Permissions.Laboratory.Manage;
                case "Inventory":
                    return read ? Permissions.Inventory.View : Permissions.Inventory.Manage;

                default:
                    // Home, Users, Roles, Settings: ليهم حمايتهم الخاصة (Users/Roles/Settings = Admin فقط)
                    return null;
            }
        }
    }
}
