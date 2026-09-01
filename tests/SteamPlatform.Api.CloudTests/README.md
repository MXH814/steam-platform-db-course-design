# Cloud API Tests

This project contains read-only smoke tests for the deployed API. It never connects directly to Oracle and does not call business write endpoints.

Run against the shared Tencent Cloud API:

```powershell
$env:STEAM_API_TEST_BASE_URL='https://124.222.213.245'
dotnet test tests\SteamPlatform.Api.CloudTests\SteamPlatform.Api.CloudTests.csproj
```

The base URL must contain only the host. Do not append `/api`, because every test route already includes that prefix.

The tests perform health checks, demo-account login, `/api/auth/me`, and GET requests for Group A/B/C/D data. Tests that create or update cloud data intentionally remain excluded.
