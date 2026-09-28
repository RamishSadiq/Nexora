# Test CRM, Portal and CMS in VS Code

Open **D:\Projects new\Nexora** as the VS Code folder. In **Run and Debug**, choose Company Dash, CRM, Portal, CMS or All applications and press **F5**:

| Option | Opens | What to test |
| --- | --- | --- |
| **Company Dash** | Its existing dashboard URL | Separate Company Dash application |
| **CRM** | http://localhost:3017/relationships/contacts | Contacts/Accounts, connected activity, search, import/export and documents |
| **Portal** | http://localhost:3018/portal | Member profile, personal/company applications, evidence, receipts and inbox |
| **CMS** | http://localhost:3019/site/26b20458-a580-40a8-8345-369ad954a54d/home | Public association website |

Each Nexora frontend first runs `local-ensure-api`. It builds and starts the shared API if port 5097 is free, or waits for the existing API to be ready. Concurrent launches serialize API startup. CRM, Portal and CMS use ports **3017**, **3018** and **3019**, isolated Next output, and separate explicit **Microsoft Edge** debug profiles. You can launch Portal and CMS together without manually starting the API.

The shared API uses **SQL Express `.\SQLEXPRESS` / `NexoraDev`** and remains running when a frontend stops. Logs are in `artifacts/local-shared-api.log` and `artifacts/local-shared-api-error.log`; its PID is in `artifacts/local-shared-api.pid`. For C# breakpoints, choose **Attach shared API** and select the Nexora API process. To rebuild backend edits, stop that specific API process, then launch a frontend again. The manual API launch configuration is hidden to avoid starting a duplicate API. Each frontend has its own preparation task; a mutex serializes only shared API creation, not application launch order.

## Test accounts and data

Staff: `admin@nexora.local`. Use the local seed password already configured in `.vscode/launch.json` under `Identity__SeedAdminPassword`. Existing staff passwords are not reset automatically.

The local test-data helper creates only named demonstration records and leaves existing website pages intact:

- Contact: **Nexora Local Test Member**.
- Account: **Nexora Local Test Organisation**, linked to that Contact.
- Portal login: **local.member@nexora.local**, using the same configured local seed password when first provisioned.
- Product: **Nexora Local Test Membership**, zero cost, with personal/company applications enabled.
- A nine-page association website: Home, About, Membership, Events, Resources, Contact, FAQs, Member guide and Organisation guide. Its public path is recorded in `artifacts/local-test-data.json` after preparation.

To prepare missing demonstration data with the debugger running: `node scripts/Prepare-LocalTestData.mjs`. It is restricted to the configured localhost debugger and `NexoraDev`; it does not reset existing passwords or automatically restore revoked personal access.

Website content is stored in CMS page revisions, not hardcoded in the public routes. Staff can edit it at `/website`, save drafts and publish. **Open public website** in the editor opens the visitor view. Setup upgrades only the unchanged original demo homepage, adds missing pages, and preserves all other existing pages and unpublished drafts. Re-running it does not reset content edits. The Events and Contact pages explicitly identify their demonstration limits; no live event booking, enquiry delivery or payment service is implied.

For an integrated check, sign into Portal, edit the profile or submit a test application, then open its CRM Contact and refresh connected activity. Staff decisions and submitted evidence are managed through Membership. CMS public links can open `/portal`; anonymous page views do not create CRM contacts or credentials.

## Important local behavior

The Development API applies pending migrations at startup when the seed configuration is present. Existing CRM files are retained but quarantined by the file-safety migration; retry their scans after configuring the shared ClamAV service. An unavailable scanner deliberately keeps files quarantined. The clean-scan path is covered by controlled tests; no live antivirus service, production identity or payment provider is configured by this setup.

Install the recommended C# extension if VS Code requests it. The configured Node runtime exists on this workstation. For another workstation, adjust its `runtimeExecutable` and SQL instance in `.vscode/launch.json`.

## Automated checks

`node scripts/Test-Website.mjs --all --production` runs CRM, Portal and CMS journeys against an isolated production frontend and disposable SQL database. Set `NEXORA_TEST_SQLSERVER_INSTANCE=.\SQLEXPRESS` on this workstation. This runner uses 5095/3015 and cleans up only its own processes/database.

The local launch commands, CRM/CMS routes, portal login and repeatable test-data setup passed verification on 24 September. Debug ports were released for F5. The outcome is recorded in [Stage 17](stages/STAGE-17-CRM-INTEGRATION.md).

CMS launches the published public homepage. Staff CMS management remains at http://localhost:3019/website. Company Dash launches its existing separate source directory and does not use the Nexora API.


28 September verification: LocalDebug API build passed with no warnings; concurrent API readiness tasks passed; Edge member login and authenticated profile requests on 3018 passed alongside repeated CMS reloads on 3019. Frontend lint passed. VS Code F5 browser dispatch itself was not automated.
Backend solution tests also passed: 115 API tests and 2 architecture tests; 7 API tests skipped. The reported API project reference resolved without MSB9008 during this build.

## Member navigation

CRM now includes **Portal** at `http://localhost:3017/portal-management`. Use it to configure branding, menus, dashboard sections and custom member information pages. Save a draft, preview it and publish to apply it to signed-in members. Existing defaults remain until publication. See [Stage 19](stages/STAGE-19-PORTAL-CONFIGURATION.md) for scope and verification.

The signed-in landing page at `/portal` is **Dashboard**, with live membership/application totals, current membership, recent applications and quick links. Each menu has its own page: `/portal/memberships`, `/portal/applications`, `/portal/organisations`, `/portal/profile` and `/portal/notifications`. Only the selected section is rendered; refresh and browser Back preserve the selected page. Application receipts remain at their existing URLs.

The local navigation check is `npx playwright test tests/portal-navigation.spec.ts` from `apps/web`, with `NEXORA_BROWSER_URL`, `NEXORA_PORTAL_EMAIL` and `NEXORA_PORTAL_PASSWORD` set for a provisioned local member. It checks menu isolation, active navigation, reload, Back, dashboard links, sign-out and mobile overflow.

Dashboard/navigation verification on 28 September: Edge menu isolation/reload/Back/sign-out/mobile check passed; all three production portal journeys passed against a disposable SQL Express database; production build, TypeScript and lint passed. The backend test build emitted ASP.NET analyzer AD0001 warnings but no build errors.

## Any-order and simultaneous startup

There is no CRM-before-Portal-before-CMS dependency. `presentation.order` only orders the VS Code dropdown. Choose any app first, or choose **All applications** to launch all four. Each Nexora frontend independently ensures the shared API is ready. Company Dash starts its own Node server (UI and API on 8000) and does not require Nexora. All four use separate Edge debug profiles. Stopping a frontend does not stop another frontend or the shared API.

The CMS scheduled publishing service handles cancellation both during dispatch and while waiting for its timer. A normal API shutdown no longer leaves the timer cancellation unhandled in user code. If the debugger is explicitly set to break on every thrown CLR exception, it can still pause on handled cancellation; resume execution or use unhandled-exception breakpoints.

`node scripts/Verify-LaunchOrder.mjs` requires clear debug ports. It starts Company Dash alone, then tests CMS-first, Portal-first and CRM-first cold API launches with all four apps running, checks each frontend proxy, and tests concurrent API preparation. It stops only its own processes. VS Code UI/F5 dispatch is a separate manual check.

Verified launch repair: five targeted CMS tests passed, including idle scheduler shutdown completing successfully. The configured launch commands passed CMS → Portal → CRM, Portal → CRM → CMS and CRM → CMS → Portal from a cold API, with Company Dash running alongside and successful API proxy calls through all three frontends. Concurrent cold API preparation passed, and Company Dash shutdown left the shared API healthy. Verification processes were stopped afterward. Actual VS Code F5 UI dispatch was not automated.
