> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 3 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Phase 0 — Discovery](01-discovery.md) · Next: [Phase 2 — Mapping library](03-mapping-library.md)

## 2. Phase 1 — Retarget and upgrade packages

### 2.1 Retarget every project in [PROJECTS]

In **every** `.csproj` from your `[PROJECTS]` list (including any extra filter/subscriber
projects), replace:

```xml
<TargetFramework>net6.0</TargetFramework>
```
with
```xml
<TargetFramework>net10.0</TargetFramework>
```

One command from the solution root does all of them:

```bash
find src tests -name "*.csproj" -exec sed -i 's|<TargetFramework>net6.0</TargetFramework>|<TargetFramework>net10.0</TargetFramework>|' {} +
```

Verify none remain: `grep -rl "net6.0" --include="*.csproj" src tests` → empty output.

### 2.2 Remove every AutoMapper package reference

For **each** project in `[AUTOMAPPER-PROJECTS]`:

```bash
dotnet remove <path-to-csproj> package AutoMapper
```

### 2.3 Upgrade packages — version matrix

Apply to whichever of your projects contains each package (use
`grep -rn "<PackageReference" --include="*.csproj" src tests` to locate them). Projects
that contain none of these packages (typical for extra filter/subscriber projects) need
nothing beyond §2.1.

| Package | Set version to |
|---|---|
| Microsoft.EntityFrameworkCore.SqlServer | **10.0.9** |
| Microsoft.EntityFrameworkCore.Design | **10.0.9** |
| Microsoft.Extensions.Configuration | **10.0.9** |
| Microsoft.Extensions.Configuration.FileExtensions | **10.0.9** |
| Microsoft.Extensions.Configuration.Json | **10.0.9** |
| Microsoft.Extensions.Configuration.Abstractions | **10.0.9** |
| Microsoft.Extensions.Logging.Abstractions | **10.0.9** |
| Microsoft.Extensions.Options | **10.0.9** |
| Microsoft.Extensions.DependencyInjection | **DELETE the reference** — but only from the project whose csproj begins `<Project Sdk="Microsoft.NET.Sdk.Web">` (the Web SDK already provides it; keeping it causes warning NU1510). If a non-web project references it, set it to 10.0.9 instead. |
| Newtonsoft.Json | **13.0.4** |
| Swashbuckle.AspNetCore | **10.2.3** |
| Serilog | **4.3.1** |
| Serilog.AspNetCore | **10.0.0** |
| Serilog.Expressions | **5.0.0** |
| Serilog.Extensions.Hosting | **10.0.0** |
| Serilog.Extensions.Logging | **10.0.0** |
| Serilog.Settings.Configuration | **10.0.1** |
| Serilog.Sinks.Console | **6.1.1** |
| Serilog.Sinks.Email | **4.2.1** |
| Serilog.Sinks.File | **7.0.0** |
| Serilog.Sinks.MSSqlServer | **10.0.0** |
| Moq | **4.20.72** |
| NUnit | **3.14.0** (3.x — NOT 4.x) |
| NUnit3TestAdapter | **6.2.0** |
| Microsoft.NET.Test.Sdk | **18.7.0** |

If your copy contains a package not in this table, leave its version unchanged unless a
GATE fails because of it (then see §7).

### 2.4 Fix SQL connection strings ⚠️ CRITICAL, runtime-only failure

The upgraded `Microsoft.Data.SqlClient` defaults to `Encrypt=true`. Every connection
string that does not explicitly set `Encrypt` or `TrustServerCertificate` **will fail at
runtime** with `SqlException` while compiling clean.

For **every** entry in your `[CONNSTRINGS]` list (JSON config values **and** strings
hardcoded in C# — test files included): if the string contains neither `Encrypt=` nor
`TrustServerCertificate=`, append:

```
TrustServerCertificate=True;
```

Baseline sites (yours may have more): two app connection strings and one Serilog
`MSSqlServer` sink `connectionString` in the Service project's `appsettings.json`, and
one hardcoded string in the PersistentQueue test class.

### 2.5 Fix the Swashbuckle namespace change

Swashbuckle 10 depends on Microsoft.OpenApi 2.x, where `OpenApiInfo` moved namespaces.
Find every affected file: `grep -rln "Microsoft.OpenApi.Models" --include="*.cs" src`
(baseline: only `Startup.cs`). In each, replace:

```diff
-using Microsoft.OpenApi.Models;
+using Microsoft.OpenApi;
```

### 2.6 Replace the email sink wrapper files

Serilog.Sinks.Email 3.0+ removed the `EmailConnectionInfo` API the wrapper was built on.
Locate the two wrapper files (baseline:
`src/Common/RECO.API.Utils/EmailSink/EmailSinkSettings.cs` and
`.../EmailSinkExtensions.cs`; otherwise find them with
`grep -rln "EmailConnectionInfo" --include="*.cs" src`).

**Replace the entire contents of `EmailSinkSettings.cs` with:**

```csharp
using MailKit.Security;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.Email;
using System;
using System.Net;

namespace RECO.API.Utils.EmailSink
{
    public class EmailSinkSettings
    {
        public string? FromEmail { get; set; }
        public string? ToEmail { get; set; }
        public string? MailServer { get; set; }
        public LogEventLevel RestrictedToMinimumLevel { get; set; } = LogEventLevel.Information;
        public string? MailSubject { get; set; }
        public bool UseDefaultCredentials { get; set; } = true;
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;
        public bool IsBodyHtml { get; set; } = false;
        public string EmailSubject { get; set; } = "Log Email";
        public string? Username { get; set; }
        public string? Password { get; set; }
        public LogEventLevel MinimumLogLevel { get; set; } = LogEventLevel.Information;

        public EmailSinkOptions ToEmailSinkOptions()
        {
            var result = new EmailSinkOptions
            {
                From = FromEmail ?? throw new InvalidOperationException($"{nameof(FromEmail)} is required for the email sink."),
                To = [ToEmail ?? throw new InvalidOperationException($"{nameof(ToEmail)} is required for the email sink.")],
                Host = MailServer ?? throw new InvalidOperationException($"{nameof(MailServer)} is required for the email sink."),
                Port = Port,
                IsBodyHtml = IsBodyHtml,
                // EnableSsl=true previously meant STARTTLS via System.Net.Mail; false meant plain SMTP.
                ConnectionSecurity = EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                Subject = new MessageTemplateTextFormatter(EmailSubject),
            };
            if (!UseDefaultCredentials)
            {
                result.Credentials = new NetworkCredential(Username, Password);
            }
            return result;
        }
    }
}
```

*Variance rule:* if your copy's `EmailSinkSettings` has **additional** public properties,
keep them as extra properties on this class — they are configuration-bound and harmless.
Never rename the existing properties (the `appsettings.json` binding depends on them).

**Replace the entire contents of `EmailSinkExtensions.cs` with:**

```csharp
using Serilog;
using Serilog.Configuration;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace RECO.API.Utils.EmailSink
{
    public static class EmailSinkExtensions
    {
        public static LoggerConfiguration EmailSink(this LoggerSinkConfiguration loggerConfiguration, EmailSinkSettings emailSinkSettings)
        {
            if (emailSinkSettings == null) throw new ArgumentNullException(nameof(emailSinkSettings));
            var options = emailSinkSettings.ToEmailSinkOptions();
            options.ServerCertificateValidationCallback = ValidateServerCertificate;
            return loggerConfiguration.Email(
                options: options,
                restrictedToMinimumLevel: emailSinkSettings.MinimumLogLevel);
        }

        private static bool ValidateServerCertificate(object? sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
```

If your namespace differs from `RECO.API.Utils.EmailSink`, keep **your** namespace and
replace only the class bodies.

### 2.7 Fix the two known pre-existing nullability warnings

**File `Modules/ProviderBase.cs`** in the Application project (skip if absent):

```diff
-            _configuration = new ConfigurationBuilder()
-                .SetBasePath(System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location))
+            string assemblyDirectory = System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location)
+                ?? System.AppContext.BaseDirectory;
+            _configuration = new ConfigurationBuilder()
+                .SetBasePath(assemblyDirectory)
```

**File `Startup.cs`** in the Service project — the `Configuration` property is declared
but never assigned (warning CS8618). Inside the constructor, after the existing
`_configuration = configuration;` line, add:

```csharp
            Configuration = configuration;
```

### ✅ GATE 1

The solution will **not build yet** — Persistence/Processing still use AutoMapper types.
Verify only:

1. `grep -rl "net6.0" --include="*.csproj" src tests` → empty.
2. `grep -rn "AutoMapper" --include="*.csproj" src tests` → empty.
3. Every `[CONNSTRINGS]` entry now carries `TrustServerCertificate=True;` (or an explicit
   `Encrypt=` setting it already had).

---

