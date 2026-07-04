> **RecoAPI .NET 6 → .NET 10 upgrade guide — part 8 of 10.** Start at the
> [README](README.md) and execute files in order; do not skip [Ground rules](00-ground-rules.md).
> Section numbers (§) are global across the guide — the README maps each § to its file.
> Previous: [Phase 5 — Verification](06-verification.md) · Next: [Appendix A — verbatim mapping files](appendix-a-entity-mappings.md)

## 7. Troubleshooting — error → cause → fix

| Symptom (build/test output) | Cause | Fix |
|---|---|---|
| `CS0234: The type or namespace name 'Models' does not exist in the namespace 'Microsoft.OpenApi'` | Swashbuckle 10 namespace change | §2.5 |
| `NU1902: Package 'MailKit' 2.6.0 … vulnerability` (or MimeKit) | Serilog.Sinks.Email still at 2.4.0 | §2.3 (Email 4.2.1) + §2.6 |
| `CS0029: Cannot implicitly convert type 'string' to 'Serilog.Formatting.ITextFormatter'` | Email sink `Subject` needs a formatter | Use `new MessageTemplateTextFormatter(EmailSubject)` exactly as §2.6 |
| `NU1510: PackageReference Microsoft.Extensions.DependencyInjection will not be pruned` | Redundant reference in the Web SDK project | §2.3 (delete it) |
| `CS0311/CS0314: … cannot be used as type parameter … IDualMapped` | An entity in `[ENTITIES]` has no mapping partial, or its file declares the wrong interface | §4 — create/fix `<Entity>Mapping.cs`; the class must be `partial` and in the DB-model's exact namespace |
| `CS0260: Missing partial modifier` | Mapping file's class name/namespace doesn't match the existing DB model | Match the DB model's namespace and class name exactly; keep `partial` |
| `CS8920/CS8929: static abstract member` errors | `LangVersion` too old in a consuming project | The interfaces live in RECO.Mapping (LangVersion latest); consuming projects only *implement* them — ensure the mapping partial is in the Persistence project, not somewhere else |
| `CS0535: does not implement interface member 'MapFrom'` | `MapFrom` missing or not `public static` | Signature must be exactly `public static <E> MapFrom(DataModels.<E> source)` |
| `SqlException … certificate` or `… encryption` at runtime | A connection string missed in §2.4 | Re-run the `[CONNSTRINGS]` grep; fix every hit |
| `MappingVerificationException: 'X' was not populated` | Mapping file forgot property X (often a property that exists only in your copy) | Add the assignment per §4.2 RULE A/B/C — or, if the old MapperFactory ignored X, add X to the test's ignore list instead |
| `MappingVerificationException: 'X' is declared unmapped-by-design but received the source value` | Test ignore list disagrees with the mapping | Read the old MapperFactory: if X was `Ignore()`d, remove the assignment from the mapping; if it was not, remove X from the test's ignore list |
| Test discovery finds 0 tests | NUnit3TestAdapter/Test SDK version drift | Pin exactly the versions in §2.3 / §6.1 |
| Existing integration tests fail with `SqlException: server not found` | Environmental (hardcoded live server) | Apply the classification rule in GATE 5 item 3 — document, don't "fix" |

---

