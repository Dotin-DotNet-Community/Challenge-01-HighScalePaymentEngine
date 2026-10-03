# چالش فنی شماره 1: موتور پردازش تراکنش‌های پرترافیک (High-Scale Payment Engine)

## 📌 سناریوی کسب‌وکار
در درگاه‌های پرداخت با لود بالا، ثانیه‌ها سرنوشت‌سازند. سامانه‌ای که پیش‌رو دارید مسئولیت پردازش، مانیتورینگ سلامت، کنترل ریسک و ثبت لاگ‌های حسابرسی هزاران تراکنش همزمان را بر عهده دارد.

هدف این است که با درک عمیق از رفتار همروندی (Concurrency) و مدیریت بهینه منابع، کلاس `TransactionEngine` را بر اساس اینترفیس `ITransactionEngine` پیاده‌سازی کنید.

---

## 🎯 شرح وظایف و نیازمندی‌های سیستم

### ۱. دیده‌بان سلامت سامانه (`StartHealthWatchdog`)
* سیستم نیاز به یک ناظر فعال دارد که در بازه‌های زمانی مشخص (`checkInterval`) وضعیت سلامت را بررسی و از طریق `alertLogger` گزارش کند.
* این پردازش نباید مانع از خاموش شدن عادی برنامه در پایان کار سیستم شود.
* با صدا زدن متد `Dispose` در کلاس، این مانیتور باید در کسری از ثانیه و بدون معطل کردن پروسس، به شکلی امن و تمیز متوقف شود.

---

### ۲. ارسال دسته‌ای با کنترل سقف همزمانی (`DispatchTransactionsWithThrottleAsync`)
* درگاه‌های بیرونی ظرفیت محدودی دارند. شما باید لیستی از تراکنش‌ها را به درگاه ارسال کنید، اما در هیچ لحظه‌ای تعداد درخواست‌های همزمان در حال پردازش نباید از `maxConcurrentGatewayCalls` فراتر برود.
* ترتیب نتایج در خروجی نهایی باید دقیقاً متناظر با ترتیب درخواست‌های ورودی باشد.
* آزادسازی ظرفیت پردازش باید حتی در صورت بروز استثنا (Exception) در هر تراکنش تضمین شود.

---

### ۳. استعلام چندگانه ریسک (`EvaluateRiskRulesAsync`)
* برای هر تراکنش، چندین ارزیابی امنیتی مستقل (مانند بررسی لیست سیاه، اعتبارسنجی احراز هویت و کشف تقلب) باید انجام شود.
* تمامی این قواعد باید به شکل غیرهمزمان و در سریع‌ترین زمان ممکن به صورت موازی استعلام شوند و پس از کامل شدن تمام آن‌ها، آرایه‌ای از ارزیابی‌ها برگردانده شود.

---

### ۴. دریافت سریع‌ترین تاییدیه تراکنش (`GetFastestConfirmationAsync`)
* برای جلوگیری از تاخیر، استعلام وضعیت تراکنش همزمان به چندین نود زیرساخت ارسال می‌شود.
* به محض دریافت اولین پاسخ معتبر و بدون خطا از هر یک از نودها، نتیجه باید فوراً بازگردانده شود.
* **نکته کلیدی:** به منظور جلوگیری از هدررفت منابع شبکه و سرور، بلافاصله پس از مشخص شدن اولین پاسخ موفق، اجرای سایر استعلام‌های در حال انجام روی دیگر نودها باید لغو (Cancel) شود.
* در صورتی که یک نود خطا دهد، نباید کل فرآیند متوقف شود، مگر اینکه همه نودها با خطا مواجه شوند.

---

### ۵. پردازش سنگین و موازی لاگ‌های حسابرسی (`EncryptAuditLogsInParallel`)
* پیش از ذخیره‌سازی، لاگ‌های خام باید رمزنگاری شوند. این عملیات سنگین محاسباتی (CPU-Bound) است.
* لاگ‌ها باید با توزیع بهینه روی هسته‌های پردازنده و با رعایت سقف پردازش موازی (`maxDegreeOfParallelism`) رمزنگاری شوند.
* عملیات جمع‌آوری نتایج باید کاملاً Thread-Safe بوده و تداخل حافظه‌ای ایجاد نکند.

---

## ⚙️ راهنمای پیاده‌سازی

1. مخزن را کلون کنید.
2. منطق مورد نظر را در `TransactionEngine.cs` پیاده‌سازی کنید.
3. کلاس شما باید اینترفیس `ITransactionEngine` را به طور کامل پیاده‌سازی کند:
```csharp
public interface ITransactionEngine : IDisposable
{
void StartHealthWatchdog(
Action<string> alertLogger, 
TimeSpan checkInterval);

Task<IReadOnlyList<TransactionResult>> DispatchTransactionsWithThrottleAsync(
IEnumerable<TransactionRequest> requests,
Func<TransactionRequest, CancellationToken, Task<TransactionResult>> gatewayCaller,
int maxConcurrentGatewayCalls,
CancellationToken cancellationToken = default);

Task<RiskAssessment[]> EvaluateRiskRulesAsync(
IEnumerable<Func<CancellationToken, Task<RiskAssessment>>> riskRules,
CancellationToken cancellationToken = default);

Task<TransactionStatus> GetFastestConfirmationAsync(
IEnumerable<Func<CancellationToken, Task<TransactionStatus>>> nodes,
CancellationToken cancellationToken = default);

IReadOnlyList<EncryptedRecord> EncryptAuditLogsInParallel(
IEnumerable<RawAuditLog> logs,
Func<RawAuditLog, EncryptedRecord> encryptAlgorithm,
int maxDegreeOfParallelism,
CancellationToken cancellationToken = default);
}
