# Upgrade notes: .NET 8 → .NET 10 (LTS)

October 2026. .NET 8 and .NET 9 reach end of support on November 10, 2026. .NET 10 is the current LTS release, supported until November 2028.

## Summary

| | Before (.NET 8) | After (.NET 10) |
|---|---|---|
| Target framework | `net8.0` | `net10.0` |
| Packages with known vulnerabilities (incl. transitive) | 3, all rated High: `System.Security.Cryptography.Xml`, `System.Security.Cryptography.Pkcs`, `System.Text.Json` | **0** |
| Deprecated packages | 0 | 0 |
| Build errors / compiler warnings | 0 / 31 | 0 / 31 (the same nullable warnings in existing code) |
| API surface (OpenAPI) | 63 operations, 74 schemas | 63 operations, 74 schemas; 1 cosmetic schema difference |
| EF Core model vs. migrations | drift ignored by EF Core 8 | in sync |

## Changes

1. **Target framework** `net10.0`; Docker base images `aspnet:10.0` and `sdk:10.0`.
2. **Packages:** EF Core 8.0.12 → 10.0.13; JwtBearer and Identity.EntityFrameworkCore → 10.0.13; Swashbuckle 7.2.0 → 10.3.1; AWSSDK.S3 3.7.415 → 3.7.511.8; Containers.Tools.Targets 1.21.0 → 1.23.0.
3. **Removed `Microsoft.AspNetCore.Identity` 2.3.1**, a package from the ASP.NET Core 2.x era. Identity now ships with the shared framework.
4. **Removed `Microsoft.VisualStudio.Web.CodeGeneration.Design`.** It is only used for Visual Studio scaffolding, and even its latest version pulls in `NuGet.Packaging`/`NuGet.Protocol` 6.12.1, which have a known vulnerability. Add it back temporarily when scaffolding is needed.
5. **Pinned `System.Security.Cryptography.Pkcs` 10.0.13.** `Microsoft.Data.SqlClient` 6.1.7 brings in 9.0.11, which is affected by CVE-2026-84008.
6. **Swagger:** Swashbuckle 10 is built on Microsoft.OpenApi 2.x. The model namespace changed, and the security requirement now references the scheme through `OpenApiSecuritySchemeReference` inside a document-aware callback.
7. **OpenAPI output fix.** Swashbuckle 10 writes `[Range(0, double.MaxValue)]` on `Course.Price` as `"maximum": 1.7976931348623157E+308`. NSwag cannot parse that number, so the frontend client could not be generated. A small schema filter drops this meaningless upper bound, as Swashbuckle 7 did. Server-side validation is unchanged.
8. **EF Core seed fix.** The admin seed used `PasswordHasher` (random salt), a new `ConcurrencyStamp` and `CreatedAt = DateTime.UtcNow`, so the model changed on every build. EF Core 9 and later report this as `PendingModelChangesWarning` and stop `dotnet ef database update`. The seed now uses the fixed values already stored in the last migration, so no new migration is needed and existing databases are not affected.

## Verification

- Built with .NET SDK 10.0.401. `dotnet list package --vulnerable --include-transitive` reports no vulnerable packages.
- `dotnet ef migrations has-pending-model-changes` reports no changes, with the same result on repeated runs.
- Created a fresh SQL Server (LocalDB) database from the existing migrations with EF Core 10, and generated an idempotent migration script.
- Ran the .NET 8 and .NET 10 builds against the same fresh database and compared 18 requests:
  - login with valid and wrong passwords;
  - JWT-protected profile, with and without a token;
  - refresh token, user list, statistics validation, progress;
  - create course, public course list, search, lecturer's courses, course by id, course details.

  Status codes and response bodies were identical, ignoring tokens, IDs and timestamps.
- OpenAPI: compared operations, parameters, request and response bodies, schemas and validation keywords. All 63 operations and 74 schemas match. The only remaining difference is that `ObjectResult.data` no longer has `nullable: true`, because Swashbuckle 10 writes an untyped `object` without it. The generated TypeScript client types this field as `any`, so it makes no difference to the frontend.
- Frontend client: regenerating with the same NSwag version (14.2.0) from the .NET 8 document reproduced the committed `api-client.ts` exactly. From the new document, only the user types changed (see below). The frontend builds with the new client.

## Security fix found during testing

While comparing responses, I found that `GET /api/Users`, `GET /api/Users/profile` and `PUT /api/Users/profile` returned the full Identity user entity, including `passwordHash`, `securityStamp` and `concurrencyStamp`. They now return `UserDto`, which has only the 9 fields the frontend uses. This changes the API schema (`UserResult` becomes `UserDtoResult`), so the frontend's API client was regenerated in the same release.

## Not covered by these checks

- Video upload → FFmpeg → HLS → S3, image upload through pre-signed URLs, and CloudFront signed URLs (these need AWS access).
- Email confirmation (needs SMTP).
- Exam, assignment and question endpoints beyond startup.
- Building the Docker image (Docker was not available on the build machine).

## Follow-ups

- Upgrade to AWS SDK for .NET v4 as a separate change; it has breaking changes and needs testing against S3.
- Clean up the 31 nullable reference warnings.
