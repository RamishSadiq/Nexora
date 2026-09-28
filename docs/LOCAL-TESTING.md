# Test CRM, Portal and CMS in VS Code

Open **D:\Projects new\Nexora** as the VS Code folder. In **Run and Debug**, choose CRM, Portal or CMS and press **F5**:

| Option | Opens | What to test |
| --- | --- | --- |
| **Company Dash** | Its existing dashboard URL | Separate Company Dash application |
| **CRM** | http://localhost:3017/relationships/contacts | Contacts/Accounts, connected activity, search, import/export and documents |
| **Portal** | http://localhost:3018/portal | Member profile, personal/company applications, evidence, receipts and inbox |
| **CMS** | http://localhost:3019/site/26b20458-a580-40a8-8345-369ad954a54d/home | Public association website |

Each Nexora frontend first runs `local-ensure-api`. It builds and starts the shared API if port 5097 is free, or waits for the existing API to be ready. Concurrent launches serialize API startup. CRM, Portal and CMS use ports **3017**, **3018** and **3019**, isolated Next output, and separate explicit **Microsoft Edge** debug profiles. You can launch Portal and CMS together without manually starting the API.

The shared API uses **SQL Express `.\SQLEXPRESS` / `NexoraDev`** and remains running when a frontend stops. Logs are in `artifacts/local-shared-api.log` and `artifacts/local-shared-api-error.log`; its PID is in `artifacts/local-shared-api.pid`. For C# breakpoints, choose **Attach shared API** and select the Nexora API process. To rebuild backend edits, stop that specific API process, then launch a frontend again. **Nexora shared API** is an optional direct debugger launch for an already-built API when 5097 is free; do not launch a second API on the same port.

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
