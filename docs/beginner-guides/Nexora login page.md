# Nexora login page

Document 1 of the Nexora learning guides  
Beginner guide to the staff CRM login  
Source snapshot date 13 October 2026

This document teaches you how the Nexora staff login page works, where its code lives, and how to make a small change in Visual Studio Code. You can start without knowing how to code. Begin with the pictures and the first practice exercise. Read the source code appendix when you want to understand the details.

The page covered here is **http://localhost:3017/login** in the local CRM. Staff use it to enter the workspace. The member portal uses a separate login flow and will have its own guide. CRM, Portal and CMS share the web project, but their pages and sign-in rules are not all the same.

## 1 How to use this document

For your first change, read sections 2 to 5, run CRM using section 6, and complete exercise A in section 10. You will change the welcome heading and check the result in your browser.

To investigate a problem, use the file map in section 4, the sign-in journey in section 7, and troubleshooting in section 12. To understand the code, read sections 8 and 9 alongside the source appendix.

The examples are instructions for changes you can try. They have not been applied to the application. Work on a local copy and record the original text before editing. The printed source is a dated reference; the files in VS Code are the live source you edit.

Contents

1. How to use this document
2. The words you need first
3. Recognise the page
4. Find the right file
5. Find your way around VS Code
6. Run the local CRM
7. Follow the login journey
8. Read the frontend code
9. Read the backend code and database map
10. Make your first changes
11. Check a change and put it back
12. Troubleshoot common problems
13. Understand the current limits
14. Understand how the pieces are assembled
15. Keep this guide useful
16. Source code appendix

## 2 The words you need first

| Word | Meaning in this guide |
| --- | --- |
| Repository | The Nexora project folder and its recorded change history. |
| Frontend | Code that draws the screen and handles browser interactions. |
| Backend | The server code that checks accounts, passwords and permissions. |
| Database | The stored records used by the backend, including users and organisations. |
| File and folder | A file contains text or code. Folders organise files. A path tells you where a file is. |
| Component | A reusable piece of a screen, such as the login form or logo. |
| Route | A web address handled by the application, such as /login. |
| API endpoint | An address that receives or returns application data, such as /api/v1/auth/login. |
| Session | The application's knowledge that this browser has signed in as a particular user. |
| Cookie | A small value the browser keeps and sends back to the server. The login cookie proves the session. |
| Tenant | One organisation's area within Nexora. Access must stay inside that organisation. |
| CSRF token | A security value checked with the request to help stop another site making a sign-in or sign-out request on your behalf. |
| Permission | A named right to use something, such as crm.read or identity.manage. |
| Terminal | A text panel where you can run commands and read their output. |
| Development | A local working environment used to learn and test changes. |
| Restart and refresh | Restart runs the server again. Refresh asks the browser to load the page again. |

File endings help you recognise the language. **.tsx** contains TypeScript and React screen markup. **.ts** contains TypeScript. **.css** controls appearance. **.cs** contains C# backend code. **.json** stores configuration. **.ps1** is a PowerShell script.

## 3 Recognise the page

![The local Nexora staff CRM login page](assets/login-page-desktop.png)

Figure 1 shows the current local page. The dark card, green accent and galaxy animation are built from code. The background is a canvas drawing rather than a photograph.

Use the following landmarks to decide where to look. File IDs such as F01 connect the pictures, the file map and the appendix.

| What you see or use | File that controls it |
| --- | --- |
| CRM Login, Welcome back, the introductory text and footer | F01 page.tsx |
| Work email, Password, the eye icon and the checkbox | F02 login-form.tsx |
| Sign in, Signing in and the visible error message | F02 login-form.tsx |
| Card width, spacing, colours and small-screen layout | F03 login.module.css |
| Galaxies, stars and the Pause motion button | F04 galaxy-background.tsx |
| The Nexora name and mark | F08 nexora-mark.tsx |
| Need help and the help instructions | The link is in F01 and the page is F10 |
| Whether local sign-in is available | B01 IdentityModule.cs and B02 LocalIdentityAdapter.cs |

The browser initially renders the local form disabled. F02 asks the backend whether local sign-in is available, then enables Sign in when the answer is true. A disabled button during initial loading is different from a permanently unavailable provider.

## 4 Find the right file

All paths below start from **D:\Projects new\Nexora**. Open this folder in VS Code. Keep Nexora work inside this repository.

![Map from the login screen through its frontend files to the backend](assets/login-code-map.png)

Figure 2 is the code map. Follow the arrows when you want to trace a sign-in. Use the file IDs in the list below to open the matching source.

### Frontend files

**F01 Page structure and wording**  
[apps/web/src/app/(auth)/login/page.tsx](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/page.tsx>)

Open this to change the greeting, introductory text, footer or help link. It also chooses a safe destination after sign-in. Search for `Welcome back` at line 18.

**F02 Form fields and browser behaviour**  
[apps/web/src/app/(auth)/login/login-form.tsx](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/login-form.tsx>)

Open this for labels, placeholders, showing the password, the checkbox, provider availability, request submission and error display. Search for `handleSubmit` at line 28.

**F03 Login appearance**  
[apps/web/src/app/(auth)/login/login.module.css](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/login.module.css>)

Open this for the login card, spacing, colour rules, motion button and mobile layout. Search for `.card {` at line 10. Some rules here override the utility classes in F02.

**F04 Animated background**  
[apps/web/src/app/(auth)/login/galaxy-background.tsx](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/galaxy-background.tsx>)

Open this for the canvas drawing and motion controls. Search for `GalaxyBackground` at line 6.

**F05 Browser requests forwarded to the backend**  
[apps/web/next.config.ts](<D:/Projects new/Nexora/apps/web/next.config.ts>)

Open this when tracing where `/api/...` requests go. Search for `async rewrites` at line 10. This is the bridge to the C# API; there is no separate login API route file in the web application.

**F06 Check the signed-in session**  
[apps/web/src/lib/auth/session.ts](<D:/Projects new/Nexora/apps/web/src/lib/auth/session.ts>)

Open this to understand why a protected page sends someone back to login. Search for `requireSession` at line 46.

**F07 Protect the staff workspace**  
[apps/web/src/app/(platform)/layout.tsx](<D:/Projects new/Nexora/apps/web/src/app/(platform)/layout.tsx>)

This layout calls F06 before rendering the staff workspace. The login page lives outside this protected layout.

**F08 Logo**  
[apps/web/src/components/brand/nexora-mark.tsx](<D:/Projects new/Nexora/apps/web/src/components/brand/nexora-mark.tsx>)

This shared component is also used elsewhere. A change here can affect other screens.

**F09 Shared button**  
[apps/web/src/components/ui/button.tsx](<D:/Projects new/Nexora/apps/web/src/components/ui/button.tsx>)

This supplies common button styles and passes through attributes such as `disabled`. F03 supplies additional login-specific button styles.

**F10 Help page**  
[apps/web/src/app/(auth)/login/help/page.tsx](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/help/page.tsx>)

This is the small user-facing guide at `/login/help`. It is separate from this developer learning document.

### Backend files

**B01 Authentication setup and endpoints**  
[backend/src/Modules/Nexora.Modules.Identity/IdentityModule.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/IdentityModule.cs>)

This defines password rules, cookies, rate limits, CSRF and the login, session and logout endpoints. Search for `LoginAsync` at line 210.

**B02 Check the credentials and record the outcome**  
[backend/src/Modules/Nexora.Modules.Identity/Security/LocalIdentityAdapter.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/LocalIdentityAdapter.cs>)

This finds the user, checks that the user and tenant are active, asks ASP.NET Core Identity to check the password, and records the outcome. Search for `PasswordSignInAsync` at line 43.

**B03 User record**  
[backend/src/Modules/Nexora.Modules.Identity/Domain/NexoraUser.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Domain/NexoraUser.cs>)

This adds Nexora fields to the standard Identity user, including TenantId, DisplayName and IsActive.

**B04 Roles and permissions added to the session identity**  
[backend/src/Modules/Nexora.Modules.Identity/Security/NexoraClaimsPrincipalFactory.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/NexoraClaimsPrincipalFactory.cs>)

This adds the tenant, display name and permissions to the signed-in identity. These values are called claims.

**B05 Active tenant check**  
[backend/src/Modules/Nexora.Modules.Identity/Security/ActiveTenantAuthorization.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/ActiveTenantAuthorization.cs>)

This checks that the signed-in user and tenant are still active when access is authorised.

**B06 API startup**  
[backend/src/Nexora.Api/Program.cs](<D:/Projects new/Nexora/backend/src/Nexora.Api/Program.cs>)

This connects the identity module to the running API. Search for `UseAuthentication` at line 61.

**B07 Identity database mapping**  
[backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/NexoraIdentityDbContext.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/NexoraIdentityDbContext.cs>)

This maps user and tenant records to SQL tables and applies tenant query filters. Treat database structure changes as a later learning step.

**B08 Local development account setup**  
[backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/IdentitySeeder.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/IdentitySeeder.cs>)

This creates the local demonstration administrator when configured. It does not normally reset an existing user's password.

### Supporting files

- S01 root layout loads the global stylesheet and page title settings. [apps/web/src/app/layout.tsx](<D:/Projects new/Nexora/apps/web/src/app/layout.tsx>)
- S02 global stylesheet imports the shared CRM theme. [apps/web/src/app/globals.css](<D:/Projects new/Nexora/apps/web/src/app/globals.css>)
- S03 shared CRM theme defines the default Nexora colour variables. [apps/web/src/components/app-shell/workspace-theme.css](<D:/Projects new/Nexora/apps/web/src/components/app-shell/workspace-theme.css>)
- S04 class helper merges shared button classes. [apps/web/src/lib/cn.ts](<D:/Projects new/Nexora/apps/web/src/lib/cn.ts>)
- S05 development configuration allows local sign-in. [backend/src/Nexora.Api/appsettings.Development.json](<D:/Projects new/Nexora/backend/src/Nexora.Api/appsettings.Development.json>)
- S06 provider option model holds configuration for the future Entra adapter. [backend/src/Modules/Nexora.Modules.Identity/Security/IdentityProviderOptions.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/IdentityProviderOptions.cs>)
- S07 security names and tenant context. [backend/src/Modules/Nexora.Modules.Identity/Security/NexoraSecurity.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/NexoraSecurity.cs>)
- S08 tenant record. [backend/src/Modules/Nexora.Modules.Identity/Domain/Tenant.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Domain/Tenant.cs>)
- S09 sign-in audit record. [backend/src/Modules/Nexora.Modules.Identity/Domain/AuditEvent.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Domain/AuditEvent.cs>)
- S10 tenant write boundary used by B07. [backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/TenantWriteBoundary.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/TenantWriteBoundary.cs>)
- S11 dashboard route calls the session helper again. [apps/web/src/app/(platform)/dashboard/page.tsx](<D:/Projects new/Nexora/apps/web/src/app/(platform)/dashboard/page.tsx>)
- S12 sign out handler in the shared staff shell. [apps/web/src/components/app-shell/app-shell.tsx](<D:/Projects new/Nexora/apps/web/src/components/app-shell/app-shell.tsx>)

The launch settings are in `.vscode/launch.json`; tasks are in `.vscode/tasks.json`; API preparation is in `scripts/Prepare-LocalDebug.ps1`. These are existing setup files rather than page-design files. Their relevant settings are explained in section 6.

## 5 Find your way around VS Code

![Illustrated VS Code map showing Explorer, editor, Run and Debug, and terminal](assets/vscode-learning-map.png)

Figure 3 is an illustration of the areas you will use. Your VS Code layout may look slightly different.

1. Open VS Code and choose **File > Open Folder**. Select `D:\Projects new\Nexora`.
2. The Explorer on the left shows folders and files. Expand `apps`, then `web`, then `src`, then `app`, then `(auth)`, then `login`.
3. Click `page.tsx`. The editor in the centre shows its contents.
4. Press **Ctrl+F**, type `Welcome back`, and press Enter. This finds text in the current file.
5. Press **Ctrl+P** to open a file quickly. Paste `apps/web/src/app/(auth)/login/page.tsx` and select it. This is often easier than expanding folders.
6. Press **Ctrl+G**, enter a line number from this guide, and press Enter to jump to that line. Line numbers change when the file changes, so use the search phrase if needed.
7. Save a change with **Ctrl+S**. A dot on the file tab indicates unsaved changes.
8. Open **View > Problems** to see reported code issues. An error does not always mean the file you just changed caused it; check the filename.
9. Open **Terminal > New Terminal** when a step needs a command. Check which folder the terminal prompt shows before running it.
10. Open **View > Run** to find Run and Debug. The dropdown there selects CRM, Portal or CMS.

`(auth)` and `(platform)` are organising folders called route groups. Their names do not appear in the web address. That is why `app/(auth)/login/page.tsx` creates `/login`, rather than `/(auth)/login`.

In an import, `@/` means the web application's `src` folder. For example, `@/lib/auth/session` points to `apps/web/src/lib/auth/session.ts`.

### Read the shape of a file

Imports at the top bring in existing components or functions. The function below the imports describes what the file does. `return` gives back a result; in a React screen it usually gives back the screen markup. Braces `{ }` group code or insert a value into markup. Quotes hold text.

Indentation helps you see which lines belong together. Do not remove a closing brace or quotation mark while changing a word. For your first exercise, replace only the text specified.

## 6 Run the local CRM

The local setup has two running parts. The web application draws the page on **3017**. The shared C# API checks identity on **5097**. They also need the configured SQL Server database.

| Setting | Current local configuration |
| --- | --- |
| VS Code launch choice | CRM |
| Staff login URL | http://localhost:3017/login |
| Shared backend API | http://localhost:5097 |
| API environment | Development |
| SQL instance and database | .\SQLEXPRESS and NexoraDev |
| Web backend setting | NEXORA_API_URL=http://localhost:5097 |
| Web build output | .next-local-debug-crm |
| Local staff account | admin@nexora.local unless SeedAdminEmail overrides it |

1. Open the Nexora root folder in VS Code.
2. Open Run and Debug, choose **CRM**, and press **F5**.
3. The preparation task starts or reuses the shared API and waits for it to be ready. Startup can take time when a build or database migrations are needed.
4. Edge is configured to open `/relationships/contacts`. An unsigned-in browser is sent to login. For this guide, type `http://localhost:3017/login` in the browser address bar.
5. Wait until the provider message says that a local development account can sign in.
6. Use the local administrator email above and the local seed password configured in `.vscode/launch.json` under `Identity__SeedAdminPassword`. Do not copy that password into screenshots or this guide. The launch configuration sets `Identity__ResetSeedAdminPassword` to false, so an existing password can differ.

The workstation's launch configuration uses a specific Node executable. On a different computer, the configured runtime and SQL instance may need updating. The backend targets .NET 10, and VS Code recommends the `ms-dotnettools.csharp` extension. The web dependencies are declared in `apps/web/package.json`. A newly cloned machine needs these prerequisites and restored packages before F5 can work; this guide's launch steps assume the existing prepared checkout.

Starting `next dev` directly uses defaults unless you supply the environment settings. Both F05 and F06 fall back to backend port **5080** when `NEXORA_API_URL` is missing. This differs from the VS Code shared API port. For learning, use the existing CRM launch profile.

### When a change needs a restart

Text, React markup and CSS often update after you save while development mode is running. Refresh the browser if needed. A C# backend change needs a rebuilt and restarted API. Stop the CRM frontend you launched, then launch CRM again; the preparation script detects newer backend source and rebuilds the API it manages. If the API was started by another debug session, stop that API session and relaunch. A change to `next.config.ts` or environment values needs the frontend to restart.

Stopping CRM does not normally stop the shared API. Portal and CMS may also use it. Do not stop another person's running services just to inspect a file. For C# debugging, use **Attach shared API** and select the Nexora API process. Reattach after the API restarts.

## 7 Follow the login journey

![Sequence of provider check, CSRF token, login request, cookie and session check](assets/login-request-journey.png)

Figure 4 follows the local password sign-in. The arrows represent requests and their results.

1. **Open the page.** F01 selects the page content and renders F02. F04 draws the animated background after the browser starts its JavaScript.
2. **Check availability.** F02 sends a GET request to `/api/v1/auth/providers`. F05 forwards it to the backend. B01 returns whether local development sign-in is enabled. The form enables Sign in only when it is enabled.
3. **Collect the fields.** You enter an email and password. F02 reads the fields when you select Sign in. Showing a password only changes its visibility in your browser.
4. **Get the security token.** F02 asks `/api/v1/auth/csrf` for a CSRF token. The backend stores an antiforgery cookie and returns the request token.
5. **Send the login request.** F02 sends email, password and rememberMe to `/api/v1/auth/login` as JSON. It puts the token in the `X-CSRF-TOKEN` header and includes cookies.
6. **Check on the server.** B01 checks required fields and the token. B02 checks provider availability, finds the account, checks the user and tenant status, and calls the Identity password sign-in method. The backend login rate limit can reject excess requests before this work.
7. **Create the session.** A successful Identity sign-in issues the session cookie. B04 supplies tenant and permission claims. B02 records the successful sign-in and returns `{ "authenticated": true }`.
8. **Open the destination.** F02 calls `router.replace(returnTo)` and `router.refresh()`. F01 has checked the requested destination. The default is `/dashboard`.
9. **Check protected access.** F07 calls F06, which sends the session cookie to `/api/v1/auth/session`. A valid session allows the workspace to render. If the session cannot be obtained, F06 redirects to `/login`.
10. **Show an error when needed.** If a login request fails, F02 displays the backend's problem title or a fallback message, and lets you try again when local sign-in remains available.

### The API addresses

| Request | Purpose | Normal result |
| --- | --- | --- |
| GET /api/v1/auth/providers | Ask which login providers are enabled | 200 and localDevelopmentEnabled plus entraEnabled |
| GET /api/v1/auth/csrf | Obtain the token used for a protected request | 200 and token |
| POST /api/v1/auth/login | Submit credentials with the token | 200 and authenticated true plus a session cookie |
| GET /api/v1/auth/session | Read the current signed-in user and tenant | 200 and session data, or an access failure |
| POST /api/v1/auth/logout | End the signed-in session with a fresh token | 204 with no response body |

**GET** asks for data. **POST** submits an action. **200** means a request succeeded. **204** means it succeeded with no response content. **400** means the request is invalid; **401** means authentication failed or is missing; **403** means access is forbidden; **429** means too many requests; **503** means a service or provider is unavailable.

Sign-out is started in `apps/web/src/components/app-shell/app-shell.tsx`. Its handler fetches a fresh CSRF token and posts to the logout endpoint. B01 then calls `SignOutAsync` to remove the staff session.

## 8 Read the frontend code

### F01 chooses the page and the destination

`metadata` supplies the page title. S01 adds the Nexora title suffix, so the browser tab is called `Sign in · Nexora`.

The page reads `returnTo` from the address. A path such as `/login?returnTo=/relationships/contacts` asks to open Contacts after login. F01 accepts a value only if it starts with `/`, does not start with `//`, and contains none of the backslash or control/space characters rejected by its regular expression. Otherwise it uses `/dashboard`. Do not remove this validation when changing the destination.

`<GalaxyBackground />` inserts F04. `<LoginForm returnTo={returnTo} />` inserts F02 and passes it the destination. A value passed from one component to another is called a prop.

### F02 remembers what is happening

The form uses React state. State is a value the component can update and then redraw.

| State value | Initial value | What changes when it updates |
| --- | --- | --- |
| showPassword | false | Chooses a masked password field or visible text |
| isSubmitting | false | Disables Sign in while a request is being submitted and shows Signing in |
| error | null | Displays the current error when there is one |
| localEnabled | false | Enables Sign in when the provider check succeeds and says true |

`useEffect` runs the provider request after the component is active in the browser. Its `active` flag prevents a late result updating a component that has already been removed from the page.

`handleSubmit` runs when the form is submitted. `event.preventDefault()` stops the browser's usual full-page form submission. `new FormData(event.currentTarget)` reads the form's named fields. `async` and `await` let the code wait for requests without treating an unfinished request as a result.

This is the actual shape of the data sent. These are variable values read from the form, not a saved password inside the source code.

```typescript
body: JSON.stringify({
  email: formData.get("email"),
  password: formData.get("password"),
  rememberMe: formData.get("rememberMe") === "on",
}),
```

`JSON.stringify` turns the object into the text format expected by the API. `credentials: "include"` tells fetch to include cookies. `response.ok` asks whether the request returned a successful status. The `catch` block handles a failed request and shows an error.

The input `name` values are part of this connection. Changing `name="email"` without also changing `formData.get("email")` would stop the email being read. You can change the visible label or placeholder without renaming the field.

### F03 decides the final appearance

F02 has utility classes that supply styles directly in its markup. F03 then adds login-specific rules such as `.card form input` and `.card form button[type=submit]`. Therefore, editing an input's utility colour in F02 may not change its appearance if F03 overrides it.

F03 uses variables such as `var(--nexora-accent)`. S03 supplies the shared defaults. Some other colours are literal values in F03 and F04. Changing one theme variable does not recolour every galaxy or every glow automatically.

The mobile rule begins with `@media(max-width:600px)`. It changes padding and text sizes on narrow screens. The reduced-motion rule removes the card's entrance animation for people who request reduced motion.

### F04 draws and controls motion

`useRef` holds the canvas element. The canvas context draws stars, glowing galaxies and orbital shapes. `requestAnimationFrame` schedules the drawing loop. The code limits updates to roughly 30 frames per second and stops active animation while the document is hidden.

The pause button updates `paused`. The component also reads the system's reduced-motion preference. When that preference is on, it draws a still background and hides the motion button. The pause choice is component state; it is not a saved CRM setting and does not persist through a fresh page load.

### F05 and F06 connect the servers

F05 rewrites browser `/api/...` requests to `NEXORA_API_URL`. The browser uses its current web origin; the web server forwards the request to the C# API. It is a forwarding rule, not the password-checking code.

F06 runs on the web server for protected pages. It reads incoming cookies and forwards them to the API's session endpoint with `cache: "no-store"`. Its React `cache` wrapper avoids repeated work within a server render; it is not a browser's persistent login storage. A network failure also produces a null session, so being redirected to login can mean the API is unavailable as well as being signed out.

## 9 Read the backend code and database map

### B01 sets the rules and receives requests

`AddNexoraIdentity` registers the identity services when the API starts. `MapNexoraIdentityEndpoints` attaches addresses to their handlers. A handler is the function that receives a request.

The frontend's JSON keys match this backend record. The standard JSON handling maps the browser's email, password and rememberMe fields to these properties.

```csharp
public sealed record LoginRequest(string Email, string Password, bool RememberMe);
```

`LoginAsync` first checks for missing email or password, validates the CSRF token, and then calls B02. The C# backend must make these checks even though the browser marks the fields as required. A caller can send an API request without using the form.

### B02 verifies the account

B02 allows the local adapter only when its settings are enabled. It trims and normalises the email to find the account, checks the user and tenant are active, and then calls the existing Identity library:

```csharp
var result = await signInManager.PasswordSignInAsync(
    user,
    request.Password,
    request.RememberMe,
    lockoutOnFailure: true);
```

Nexora passes password verification to ASP.NET Core Identity. The application stores a password hash through that library rather than a readable password. Changing the placeholder or the screen's password visibility does not change the stored password.

The temporary `BeginSystemAccess` scope lets this trusted server code find an account before it knows the signed-in tenant. It is not a tenant selected by the browser. S10 restores that scope afterwards. Normal authenticated work uses tenant boundaries.

### The current rules

| Rule | Value in the current source | Where it comes from |
| --- | --- | --- |
| Local adapter available | Development environment AND LocalDevelopmentEnabled true | B01 |
| New-password minimum | 12 characters, digit, lowercase, uppercase and non-alphanumeric | B01 |
| Failed-password lockout | 5 failed attempts and a 15-minute lockout | B01 |
| Login request limiter | 8 requests per minute per server-observed IP, with no queue | B01 |
| Session lifetime setting | 8 hours with sliding expiration | B01 |
| Development cookie name | Nexora.Session | B01 |
| Production cookie name | __Host-Nexora.Session, with HTTPS required | B01 |
| CSRF header name | X-CSRF-TOKEN | B01 |
| Sign-in audit type | identity.login | B02 |

Password creation rules do not mean a short string is rejected by the login form itself. They are the backend policy used when creating or setting passwords. Lockout and request limiting are different controls: one concerns an account's failed password attempts, the other limits requests from an IP.

The session cookie is HttpOnly, so browser JavaScript cannot read it directly. It is sent with requests. `rememberMe` controls whether Identity makes the login persistent; the eight-hour ticket setting and sliding renewal still apply. Browser settings also affect persistence. It does not mean permanent access.

B01 refreshes the user's claims during cookie validation and rejects an inactive or invalid user. B05 also checks the active tenant boundary. Updating an account or its permissions must affect later access, even if the browser already has a cookie.

### Where the records are stored

B07 configures the `identity` SQL schema. The local SQL database is selected by the launch configuration. The page does not connect directly to SQL.

| Table | What it contributes to login |
| --- | --- |
| identity.Users | Email, PasswordHash, lockout fields, Nexora display name, tenant and active status |
| identity.Tenants | Organisation name, slug and active or suspended status |
| identity.Roles and identity.UserRoles | The user's assigned security roles |
| identity.RolePermissions | Named permissions for tenant roles |
| identity.AuditEvents | Sign-in outcome, subject, time, and available user, tenant and IP values |

The base IdentityUser supplies fields such as PasswordHash and lockout information. B03 adds Nexora-specific properties. B04 creates the claims used by the session. B01's session endpoint returns the user's display name, email, tenant, roles and permissions; it does not return a password hash.

B02 records successful sign-ins, denied credentials, and locked-account outcomes. Provider-disabled, CSRF-validation and rate-limit failures do not all pass through its audit-writing method. The visible audit wording on the page should not be read as proof that every rejected request is recorded there.

B08 creates demonstration records only in the configured Development startup path. Existing passwords are not reset unless `Identity:ResetSeedAdminPassword` is deliberately enabled. Do not delete user tables or enable resets as a routine fix for a forgotten local password.

## 10 Make your first changes

Start with A. Complete one exercise, save, check the browser, and then decide whether to keep it. These exercises change existing code; they do not add a new application.

### Exercise A Change the welcome heading

1. Press Ctrl+P and open `apps/web/src/app/(auth)/login/page.tsx`.
2. Press Ctrl+F and search for `Welcome back`.
3. Replace only the words inside the heading.

Before

```tsx
<h1 id="login-title">Welcome back</h1>
```

After

```tsx
<h1 id="login-title">Welcome to Nexora</h1>
```

4. Press Ctrl+S. Open or refresh `/login`.
5. Check that the heading changed and the form still appears. On a narrow screen, check that it wraps naturally.

What you learned: visible text can be changed without altering sign-in behaviour. Keep `id="login-title"` because the card's accessibility label points to it.

### Exercise B Change the email hint

Open F02 and find `placeholder="you@organisation.com"`. Replace it with:

```tsx
placeholder="name@yourcompany.com"
```

Save and refresh. The hint appears only when the field is empty. Keep `id="email"`, `name="email"`, `type="email"` and `required`. Those connect the label, data collection and browser validation.

### Exercise C Change the button wording

Open F02 and find `Sign in<ArrowRight`. Replace just the displayed `Sign in` with `Open my workspace`. Keep the ArrowRight component and the button attributes.

Check that the button shows the new wording. The busy text is separately written as `Signing in…`; change that only if you want a different loading message. Existing browser tests that locate the exact button name `Sign in` may need their locators updated. A wording change can affect automated checks even when the login request is unchanged.

### Exercise D Make the card slightly wider

Open F03 and search for `width:min(100%,450px)` inside `.card`. Change only 450 to 480:

```css
width:min(100%,480px);
```

Save and check the desktop page. Also check a narrow browser window: `100%` still caps the card to the available space. The media rule and outer padding also contribute to the mobile appearance.

### Exercise E Try a colour only on the login page

Open F03. Inside the `.page { ... }` rule, add this declaration:

```css
--nexora-accent: #7dd3fc;
```

The card and submit button inherit this login-specific value. Save and check text contrast, input focus and hover states. Some glow colours and F04's drawing colours are literal green and purple values, so they will remain those colours. To undo, remove only the new declaration.

Changing S03's root variable would also affect other screens. Use the local `.page` example when your intention is to experiment with the login alone.

### Exercise F Read and test the background controls

Use the existing Pause motion button before editing code. Watch the background stop, then use Play motion. Reload: the choice is not persisted. If your operating system requests reduced motion, the button is hidden and the drawing is still.

To change the number of background stars, open F04 and find `Array.from({ length: 220 }`. Replace 220 with 160, save, and reload. This changes the separate background stars. The 14500-point galaxy drawing is a different loop; it has not changed. Put 220 back when finished. Avoid increasing drawing counts without checking performance.

### Exercise G Change a backend error message with support

This is a later exercise because it changes server behaviour and requires an API restart. Open B02 and find the two occurrences of `Email or password is incorrect.`. One is for a missing, inactive or unavailable-tenant account; the other is for a failed password check.

You could replace both strings with `Unable to sign in with these details.`. Keep the 401 status and the same generic wording for both cases. Do not disclose whether an email exists. Preserve the distinct lockout message.

Rebuild and restart the managed API using section 6, then check one failed login in a local test account. Do not repeatedly submit wrong passwords. The existing `Login_rejects_invalid_credentials_without_disclosing_the_account` test expects part of the original wording, so its assertion must be reviewed with this change.

### Changes to leave until you understand the login

Security-sensitive changes include removing CSRF checks, changing cookie security, changing tenant filters, weakening password or lockout rules, allowing local login outside Development, adding SSO, adding account recovery, and changing database structure. These require implementation work and meaningful security checks. Editing a button label does not implement any of them.

## 11 Check a change and put it back

Before editing, write down the file, the phrase you found and the original value. Save a copy of the small section you plan to change. The repository may already contain someone else's unsaved or uncommitted work; preserve it.

After a text or CSS change, use these checks:

1. Open `/login`, then check your specific edit.
2. Make the browser narrow and check for clipped labels, overlapping controls and horizontal scrolling.
3. Press Tab through the page. The focused link or button should have a visible outline. Check the label remains attached to its input.
4. Enter a dummy password without submitting. Use the eye button to show and hide it, then clear the field.
5. Check the local provider message and Microsoft button still match the configured provider state.
6. For a change to the submission logic, use a valid local test account once and confirm protected access and sign-out. Keep real passwords out of screenshots and logs.

For frontend changes that affect code structure, the existing commands below provide additional checks. Run them from `D:\Projects new\Nexora\apps\web`. `npm` must be available in that terminal. These commands check the source; they do not replace viewing the page.

```powershell
npm run lint
npx tsc --noEmit
```

For backend changes, ask someone comfortable with .NET to run the existing IdentityEndpointTests and review failures. The targeted command from the repository root is:

```powershell
dotnet test backend/tests/Nexora.Api.Tests/Nexora.Api.Tests.csproj --filter FullyQualifiedName~IdentityEndpointTests
```

The four tests in that class cover anonymous session rejection, successful tenant-scoped login and logout, generic invalid-credential rejection, and missing-CSRF rejection. They use test-host accounts and are different from signing into your persistent local CRM. Read the test factory before changing test infrastructure, and check the current test results before relying on a change.

To undo your own edit, restore the exact original text and save. Ctrl+Z can undo recent editor changes while the edit history is available. If using Source Control, inspect the diff and revert only the lines you changed. **Discard Changes** can remove other work in that file; do not use it casually. Do not delete the database to undo a wording or colour change.

## 12 Troubleshoot common problems

### The page cannot be opened

Confirm you used `http://localhost:3017/login`. Check that the CRM debug session is running. A connection-refused message usually means no frontend is listening at that address. Check the VS Code terminal output before changing port settings.

### The sign in service is unavailable

Open `http://localhost:5097/api/v1/auth/providers` and `http://localhost:3017/api/v1/auth/providers` in a browser. Both should return JSON and, for the enabled local setup, show `localDevelopmentEnabled: true`.

If the direct API works but the frontend address fails, check F05 and `NEXORA_API_URL`, then restart the frontend. If both fail, check the shared API. Use `http://localhost:5097/health/ready` to check backend and database readiness. `/health` alone is not the same readiness check.

The provider check also depends on browser JavaScript starting successfully. If the API answers true but the button remains disabled, refresh and inspect the browser Console and Network tabs for failed scripts or requests.

### Sign in stays disabled

Wait for the provider request to finish. If it returns false, check that the API environment is Development and S05 enables local development sign-in. Do not remove `disabled` to bypass this state. The backend still rejects the disabled provider.

### The email or password is rejected

Check the account and password, including accidental spaces in the password. The email is trimmed on the backend; the password is passed as entered. The configured seed password may not match an existing account because automatic resetting is disabled. An inactive user or suspended tenant also receives the generic credential message.

### The account is temporarily locked

Stop submitting guesses and wait for the configured lockout to expire, or ask the Nexora administrator to investigate. Five failed password attempts can lock an account. Excess requests from the same IP can independently return 429; wait for its one-minute limiter window to pass rather than weakening the limiter.

### The security token is invalid or expired

Refresh the page and submit once again so it gets a fresh CSRF token. If the problem continues, check that the token request and login request use the same browser session and origin, and that `X-CSRF-TOKEN` is still sent. Do not remove the backend check.

### Login succeeds but the page returns to login

Check the session endpoint, cookie and API connection. F06 needs the same backend origin as F05. It returns no session when the API cannot be reached. Also check the user's active status and tenant status. A 403 and a 401 have different meanings; inspect the actual Network response instead of guessing from the page redirect alone.

### Saved code does not change the running page

Confirm you edited this checkout and saved the file. F03 may override a style from F02. Restart for configuration or backend changes. The API preparation script rebuilds backend source only when you launch its preparation flow; saving C# alone does not reload a currently running API.

### API preparation fails

Do not choose Debug Anyway when the API preparation task fails. Read `artifacts/local-shared-api-error.log` and `artifacts/local-shared-api.log` inside Nexora. A build problem, stopped SQL service, or database migration problem needs its own correction. A password or CSS edit will not resolve it. Local Development startup may apply pending migrations; have a suitable backup before experimenting with database changes.

## 13 Understand the current limits

The source currently implements the local development staff password flow, provider reporting, CSRF, cookie sessions, account lockout, login rate limiting, tenant checks, and sign-in outcome auditing in B02.

Microsoft Entra login is future scope in this implementation. B01 returns `entraEnabled = false`; F02 displays a disabled Microsoft button; S06 holds options for a forthcoming adapter. Filling in a tenant or client ID alone does not create a working SSO flow. The help page's general wording does not override this actual state.

The staff login page has no self-service registration, working forgot-password/reset journey, or MFA challenge. The help page directs people to an administrator. Adding a link alone would not implement account recovery.

The login's text and animation are currently controlled by these source files. F04's pause state is not a published CRM animation setting. Portal and website presentation controls elsewhere in Nexora do not automatically control this staff login. The session cookie's production settings do not establish that production identity is connected or a deployment is ready.

## 14 Understand how the pieces are assembled

The source shows a useful learning order for rebuilding your understanding of this feature. This is the dependency order of the existing pieces, rather than a dated history of who wrote each line.

1. Define users and organisations in B03 and S08, with table mapping in B07.
2. Register Identity, cookie rules, tenant checks, CSRF and request limiting in B01.
3. Verify credentials and record outcomes in B02. Add tenant and permission claims through B04.
4. Connect the module to API startup and its request pipeline in B06.
5. Build the screen in F01 and reusable form in F02.
6. Connect browser requests to the API with F05. Protect staff pages through F06 and F07.
7. Add the login appearance in F03, drawing in F04 and shared components in F08 and F09.
8. Configure the local runtime and database through the launch files and preparation script.
9. Check the page, API responses, tenant boundaries and logout behaviour.

This order explains why copying only page.tsx will not reproduce a working login. The page depends on imported components, packages, the API, a configured database and a valid account. The appendix provides the application code for this feature; framework library internals and the rest of Nexora remain in their existing dependencies and repository files.

## 15 Keep this guide useful

Each source listing has a file ID, repository path, line numbers and a SHA256 fingerprint of the file when the document was generated. The fingerprint helps a maintainer see whether the live file has changed. Use the search phrases in section 4 when line numbers have moved.

When you keep a code change, update the corresponding explanation, exercise or screenshot. Regenerate the source appendix and Word document so the printed code stays in step with the checkout. The maintainer command from the repository root is:

```powershell
& 'C:\Users\ramish\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe' scripts/docs/Build-LoginGuide.py
```

That path is specific to this workstation. The generator reads the source files and this Markdown guide, creates the diagram assets, refreshes the appendix, and writes `docs/beginner-guides/Nexora login page.docx`. It does not modify application source. Capture a new page screenshot and visually review the Word output after meaningful changes.

The same teaching structure can be used for later Nexora guides: recognise the screen, map the files, follow the request, explain the source, practise small changes, check them, and document the feature's actual limits.

## 16 Source code appendix

Read this after the walkthrough. Each numbered line comes from the file listed above it. **Line numbers are navigation aids and are not part of the code.** Make changes in the actual source file opened in VS Code, rather than pasting the entire printed listing over it.

The complete current files for the login page, form, appearance, animation, API forwarding, session checking and core backend identity flow are included below. Supporting files marked **excerpt** show only their relevant part, with original line numbers. Their remaining code supports other Nexora features. No secret configuration values have been copied into the appendix.

<!-- BEGIN GENERATED LOGIN SOURCE -->

### F01 Page structure and destination

[apps/web/src/app/(auth)/login/page.tsx](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/page.tsx>)

Complete file. 24 source lines shown.

SHA256 `bc6a2cbd040ad7482176aaaaaee81ffaea88ab76d3d93f50ca203ff0828429ab`

```tsx
0001  import Link from "next/link";
0002  import type { Metadata } from "next";
0003  import { NexoraMark } from "@/components/brand/nexora-mark";
0004  import { LoginForm } from "./login-form";
0005  import { GalaxyBackground } from "./galaxy-background";
0006  import styles from "./login.module.css";
0007  
0008  export const metadata: Metadata = { title: "Sign in" };
0009  
0010  export default async function LoginPage({ searchParams }: { searchParams: Promise<{ returnTo?: string }> }) {
0011    const requestedReturnTo = (await searchParams).returnTo;
0012    const returnTo = requestedReturnTo?.startsWith("/") && !requestedReturnTo.startsWith("//") && !/[\\\x00-\x20]/.test(requestedReturnTo) ? requestedReturnTo : "/dashboard";
0013    return <main className={styles.page}>
0014      <GalaxyBackground />
0015      <header className={styles.header}><div className={styles.brand}><NexoraMark inverse /><span>CRM Login</span></div><Link href="/login/help">Need help?</Link></header>
0016      <div className={styles.stage}>
0017        <section className={styles.card} aria-labelledby="login-title">
0018          <div className={styles.intro}><span className={styles.eyebrow}>Everything connected.</span><h1 id="login-title">Welcome back</h1><p>Relationships, operations and insight.<br />One connected workspace.</p></div>
0019          <LoginForm returnTo={returnTo} />
0020        </section>
0021      </div>
0022      <footer className={styles.footer}><span>© 2026 Nexora</span><span>Built for modern organisations</span></footer>
0023    </main>;
0024  }
```

### F02 Form and browser behaviour

[apps/web/src/app/(auth)/login/login-form.tsx](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/login-form.tsx>)

Complete file. 102 source lines shown.

SHA256 `b9c148735d30d2a6c05ec214cf8c7c08eec3a5f02c85fffccd72ea40aab7a9f0`

```tsx
0001  "use client";
0002  
0003  import { useEffect, useState, type FormEvent } from "react";
0004  import { useRouter } from "next/navigation";
0005  import type { Route } from "next";
0006  import { ArrowRight, Eye, EyeOff, LoaderCircle, LockKeyhole } from "lucide-react";
0007  import { Button } from "@/components/ui/button";
0008  
0009  export function LoginForm({ returnTo = "/dashboard" }: { returnTo?: string }) {
0010    const router = useRouter();
0011    const [showPassword, setShowPassword] = useState(false);
0012    const [isSubmitting, setIsSubmitting] = useState(false);
0013    const [error, setError] = useState<string | null>(null);
0014  
0015    const [localEnabled, setLocalEnabled] = useState(false);
0016    useEffect(() => {
0017      let active = true;
0018      fetch("/api/v1/auth/providers", { cache: "no-store" })
0019        .then(async (response) => {
0020          if (!response.ok) throw new Error("Sign-in service unavailable.");
0021          const provider = (await response.json()) as { localDevelopmentEnabled: boolean };
0022          if (active) setLocalEnabled(provider.localDevelopmentEnabled);
0023        })
0024        .catch(() => { if (active) setError("The sign-in service is currently unavailable."); });
0025      return () => { active = false; };
0026    }, []);
0027  
0028    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
0029      event.preventDefault();
0030      setIsSubmitting(true);
0031      setError(null);
0032  
0033      const formData = new FormData(event.currentTarget);
0034  
0035      try {
0036        const csrfResponse = await fetch("/api/v1/auth/csrf", {
0037          credentials: "include",
0038        });
0039        if (!csrfResponse.ok) {
0040          throw new Error("The sign-in service is currently unavailable.");
0041        }
0042  
0043        const { token } = (await csrfResponse.json()) as { token: string };
0044        const response = await fetch("/api/v1/auth/login", {
0045          method: "POST",
0046          credentials: "include",
0047          headers: {
0048            "Content-Type": "application/json",
0049            "X-CSRF-TOKEN": token,
0050          },
0051          body: JSON.stringify({
0052            email: formData.get("email"),
0053            password: formData.get("password"),
0054            rememberMe: formData.get("rememberMe") === "on",
0055          }),
0056        });
0057  
0058        if (!response.ok) {
0059          const problem = (await response.json().catch(() => null)) as
0060            | { title?: string }
0061            | null;
0062          throw new Error(problem?.title ?? "Unable to sign in.");
0063        }
0064  
0065        router.replace(returnTo as Route);
0066        router.refresh();
0067      } catch (caught) {
0068        setError(caught instanceof Error ? caught.message : "Unable to sign in.");
0069        setIsSubmitting(false);
0070      }
0071    }
0072  
0073    return (
0074      <form className="space-y-5" onSubmit={handleSubmit}>
0075        <p className="text-sm text-[#526174]">{localEnabled ? "Sign in with your local development account." : "Local sign-in is unavailable."} Microsoft Entra single sign-on will be available when connected.</p>
0076        <div>
0077          <label className="mb-2 block text-sm font-medium text-[#344054]" htmlFor="email">Work email</label>
0078          <input className="h-12 w-full rounded-xl border border-[#d9dfe8] bg-white px-3.5 text-[15px] text-[#182230] shadow-sm outline-none transition placeholder:text-[#98a2b3] hover:border-[#bdc7d6] focus:border-[#607cf2] focus:ring-4 focus:ring-[#607cf2]/10" id="email" name="email" type="email" autoComplete="email" placeholder="you@organisation.com" required />
0079        </div>
0080        <div>
0081          <div className="mb-2 flex items-center justify-between">
0082            <label className="text-sm font-medium text-[#344054]" htmlFor="password">Password</label>
0083  
0084          </div>
0085          <div className="relative">
0086            <input className="h-12 w-full rounded-xl border border-[#d9dfe8] bg-white px-3.5 pr-12 text-[15px] text-[#182230] shadow-sm outline-none transition placeholder:text-[#98a2b3] hover:border-[#bdc7d6] focus:border-[#607cf2] focus:ring-4 focus:ring-[#607cf2]/10" id="password" name="password" type={showPassword ? "text" : "password"} autoComplete="current-password" placeholder="Enter your password" required />
0087            <button className="absolute inset-y-0 right-0 grid w-12 place-items-center rounded-r-xl text-[#7c8798] hover:text-[#405bd8]" type="button" onClick={() => setShowPassword((current) => !current)} aria-label={showPassword ? "Hide password" : "Show password"}>
0088              {showPassword ? <EyeOff className="size-[18px]" /> : <Eye className="size-[18px]" />}
0089            </button>
0090          </div>
0091        </div>
0092        <label className="flex cursor-pointer items-center gap-2.5 text-sm text-[#526174]"><input className="size-4 rounded border-[#cfd6e0] accent-[#405bd8]" name="rememberMe" type="checkbox" defaultChecked />Keep me signed in on this device</label>
0093        {error && <p className="rounded-xl border border-[#f2c7ca] bg-[#fff5f5] px-3.5 py-3 text-sm text-[#a33a40]" role="alert">{error}</p>}
0094        <Button className="h-12 w-full text-[15px]" type="submit" disabled={isSubmitting || !localEnabled}>
0095          {isSubmitting ? <><LoaderCircle className="size-[18px] animate-spin" aria-hidden="true" />Signing in…</> : <>Sign in<ArrowRight className="size-[18px]" aria-hidden="true" /></>}
0096        </Button>
0097        <div className="relative py-1"><div className="absolute inset-0 flex items-center" aria-hidden="true"><div className="w-full border-t border-[#e6e9ef]" /></div><div className="relative flex justify-center"><span className="bg-white px-3 text-xs font-medium uppercase tracking-[0.08em] text-[#98a2b3]">or continue with</span></div></div>
0098        <Button className="h-12 w-full" variant="secondary" type="button" disabled><span className="grid size-6 place-items-center rounded-md bg-[#f2f5fb] text-xs font-bold text-[#335aa7]">M</span>Microsoft SSO · not connected</Button>
0099        <p className="flex items-center justify-center gap-2 rounded-xl bg-[#f7f8fb] px-3 py-2.5 text-center text-xs leading-5 text-[#778296]"><LockKeyhole className="size-3.5 shrink-0" aria-hidden="true" />Secure, tenant-isolated access with auditable sign-in activity.</p>
0100      </form>
0101    );
0102  }
```

### F03 Login appearance

[apps/web/src/app/(auth)/login/login.module.css](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/login.module.css>)

Complete file. 39 source lines shown.

SHA256 `5750f3aa186bb819515a2e95e282b22fa0f0c530cabf64655b7252da47f18634`

```css
0001  .page { position:relative; isolation:isolate; display:flex; flex-direction:column; min-height:100svh; color:var(--nexora-foreground); color-scheme:dark; background:radial-gradient(ellipse at 18% 28%,#50f5d612,transparent 48%),radial-gradient(ellipse at 80% 70%,#9255ff18,transparent 44%),var(--nexora-canvas); }
0002  .cosmos { position:fixed; inset:0; z-index:-1; width:100%; height:100%; pointer-events:none; opacity:.6; }
0003  .header { display:flex; align-items:center; justify-content:space-between; gap:16px; padding:24px 40px; }
0004  .header > a { color:#adaeba; font-size:12px; font-weight:500; padding:8px 12px; border:1px solid #30303b; border-radius:9px; background:#14141980; }
0005  .header > a:hover { color:var(--nexora-accent); border-color:#50f5d655; }
0006  .brand { display:flex; align-items:center; gap:16px; }
0007  .brand > span { font-size:12px; color:#adaeba; border-left:1px solid #30303b; padding-left:16px; }
0008  .brand [aria-label=Nexora] > span:first-child { color:var(--nexora-canvas); box-shadow:0 0 22px #50f5d626; }
0009  .stage { flex:1; display:grid; place-items:center; padding:12px 24px 28px; }
0010  .card { width:min(100%,450px); padding:28px 36px; border:1px solid #50f5d638; border-radius:24px; background:linear-gradient(145deg,#171e20ed,#15131ded); box-shadow:0 32px 110px #0006,-24px 0 65px -48px var(--nexora-accent),24px 0 65px -48px var(--nexora-secondary); backdrop-filter:blur(22px); animation:card-enter 500ms ease-out both; }
0011  .intro { text-align:center; margin-bottom:22px; }
0012  .eyebrow { display:block; margin-bottom:9px; color:var(--nexora-accent); font-size:10px; font-weight:600; letter-spacing:.15em; text-transform:uppercase; }
0013  .intro h1 { color:var(--nexora-foreground); font-size:32px; line-height:1.2; font-weight:500; letter-spacing:-.045em; }
0014  .intro > p { margin-top:10px; color:#adaeba; font-size:12px; line-height:1.7; }
0015  .card form { display:flex; flex-direction:column; gap:15px; }
0016  .card form > * { margin-block:0; }
0017  .card form > p:first-child { color:#adaeba; font-size:11px; line-height:1.6; }
0018  .card form label { color:#e4e4eb; font-size:12px; font-weight:500; }
0019  .card form input:not([type=checkbox]) { height:43px; border:1px solid #41414f; border-radius:9px; background:#09090b80; color:var(--nexora-foreground); font-size:13px; box-shadow:none; }
0020  .card form input::placeholder { color:#9193a4; }
0021  .card form input:focus { border-color:var(--nexora-accent); outline:2px solid #50f5d640; outline-offset:2px; box-shadow:none; }
0022  .card form input[type=checkbox] { accent-color:var(--nexora-accent); }
0023  .card form input:-webkit-autofill { -webkit-text-fill-color:var(--nexora-foreground); box-shadow:0 0 0 1000px #141419 inset; }
0024  .card form button { color:#adaeba; }
0025  .card form button[type=submit] { height:43px; border:1px solid var(--nexora-accent); border-radius:9px; background:var(--nexora-accent); color:var(--nexora-canvas); font-size:13px; box-shadow:0 0 22px #50f5d61a; }
0026  .card form button[type=submit]:hover:not(:disabled) { background:#88ffe5; box-shadow:0 0 28px #50f5d62b; }
0027  .card form > button[type=button] { height:40px; border:1px solid #30303b; border-radius:9px; background:#ffffff04; color:#adaeba; font-size:12px; box-shadow:none; }
0028  .card form > button[type=button] span { background:#241b38; color:#d7c4ff; }
0029  .card form > div:has(> div > span) span { background:#15161c; color:#adaeba; font-size:9px; }
0030  .card form > div:has(> div > span) :global(.border-t) { border-color:#30303b; }
0031  .card form > p:last-child { background:#ffffff05; color:#adaeba; font-size:10px; line-height:1.6; }
0032  .card form [role=alert] { background:#361d28; border-color:#ffb0c050; color:#ffb0c0; font-size:12px; }
0033  .footer { display:flex; justify-content:center; flex-wrap:wrap; gap:8px 24px; padding:12px 130px 20px; color:#adaeba; font-size:10px; }
0034  .motion { position:fixed; z-index:2; right:22px; bottom:16px; display:flex; align-items:center; gap:7px; padding:8px 10px; border:1px solid #30303b; border-radius:20px; color:#adaeba; background:#141419dd; font-size:10px; cursor:pointer; }
0035  .motion:hover { color:var(--nexora-accent); border-color:#50f5d655; }
0036  .page :is(button,a):focus-visible { outline:2px solid var(--nexora-accent); outline-offset:4px; }
0037  @keyframes card-enter { from { opacity:0; transform:translateY(8px); } to { opacity:1; transform:translateY(0); } }
0038  @media(max-width:600px) { .header { padding:20px; } .brand { gap:10px; } .brand > span { padding-left:10px; font-size:10px; } .stage { padding:8px 16px 24px; } .card { padding:26px 23px; border-radius:20px; } .intro h1 { font-size:29px; } .footer { justify-content:flex-start; padding:8px 120px 24px 22px; } .footer span:last-child { display:none; } }
0039  @media(prefers-reduced-motion:reduce) { .card { animation:none; } }
```

### F04 Animated background

[apps/web/src/app/(auth)/login/galaxy-background.tsx](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/galaxy-background.tsx>)

Complete file. 64 source lines shown.

SHA256 `43dc70eda2c0c806fc6a7f7f350df05b74a0f91e9cdf6ba1223b681375d78ffc`

```tsx
0001  "use client";
0002  import { useEffect, useRef, useState } from "react";
0003  import { Pause, Play } from "lucide-react";
0004  import styles from "./login.module.css";
0005  
0006  export function GalaxyBackground() {
0007    const canvas = useRef<HTMLCanvasElement>(null);
0008    const [paused, setPaused] = useState(false);
0009    const [reducedMotion, setReducedMotion] = useState(false);
0010    useEffect(() => {
0011      const preference = matchMedia("(prefers-reduced-motion: reduce)");
0012      const update = () => setReducedMotion(preference.matches);
0013      update(); preference.addEventListener("change", update);
0014      return () => preference.removeEventListener("change", update);
0015    }, []);
0016    useEffect(() => {
0017      const element = canvas.current;
0018      const ctx = element?.getContext("2d");
0019      if (!element || !ctx) return;
0020      let seed = 72031;
0021      const random = () => { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };
0022      const stars = Array.from({ length: 220 }, () => ({ x: random(), y: random(), radius: .35 + random() * 1.15, phase: random() * 6.28 }));
0023      const galaxy = document.createElement("canvas"); galaxy.width = galaxy.height = 800;
0024      const g = galaxy.getContext("2d");
0025      if (!g) return;
0026      const glow = g.createRadialGradient(400, 400, 0, 400, 400, 360);
0027      glow.addColorStop(0, "#d0fff3bb"); glow.addColorStop(.08, "#50f5d678"); glow.addColorStop(.3, "#9255ff2a"); glow.addColorStop(1, "#9255ff00");
0028      g.fillStyle = glow; g.fillRect(0, 0, 800, 800);
0029      for (let i = 0; i < 14500; i++) {
0030        const radius = Math.pow(random(), .7) * 350;
0031        const angle = radius / 74 + (i % 3) * Math.PI * 2 / 3 + (random() - .5) * (.45 + radius / 500);
0032        const x = 400 + Math.cos(angle) * radius + (random() - .5) * 16;
0033        const y = 400 + Math.sin(angle) * radius + (random() - .5) * 16;
0034        g.fillStyle = i % 5 === 0 ? "#50f5d69a" : i % 3 === 0 ? "#c3a4ff79" : "#9255ff4c";
0035        g.beginPath(); g.arc(x, y, random() * 1.2 + .25, 0, Math.PI * 2); g.fill();
0036      }
0037      let width = 0, height = 0, frame = 0, last = 0, time = 0;
0038      const resize = () => { width = innerWidth; height = innerHeight; const ratio = Math.min(devicePixelRatio || 1, 1.5); element.width = width * ratio; element.height = height * ratio; ctx.setTransform(ratio, 0, 0, ratio, 0, 0); draw(); };
0039      function draw() {
0040        if (!ctx) return;
0041        ctx.clearRect(0, 0, width, height);
0042        const paintGalaxy = (x: number, y: number, size: number, rotation: number, opacity: number) => {
0043          ctx.save(); ctx.translate(x, y); ctx.rotate(rotation); ctx.scale(1, .58); ctx.globalAlpha = opacity; ctx.drawImage(galaxy, -size / 2, -size / 2, size, size); ctx.restore();
0044        };
0045        paintGalaxy(width * .16, height * .32, Math.min(width * .8, 940), -.55 + time * .000012, .85);
0046        paintGalaxy(width * .86, height * .74, Math.min(width * .6, 680), .4 - time * .000016, .65);
0047        paintGalaxy(width * .81, height * .17, 280, -.5 + time * .000009, .45);
0048        paintGalaxy(width * .12, height * .83, 260, .2, .4);
0049        ctx.save(); ctx.translate(width * .83, height * .43); ctx.scale(1, .48);
0050        for (let orbit=1;orbit<=4;orbit++) { const radius=orbit*25; ctx.strokeStyle='#50f5d628'; ctx.beginPath(); ctx.arc(0,0,radius,0,Math.PI*2); ctx.stroke(); const angle=orbit*1.8+time*.00003/orbit; ctx.fillStyle=['','#50f5d6','#9255ff','#c3a4ff','#96edc5'][orbit]; ctx.beginPath(); ctx.arc(Math.cos(angle)*radius,Math.sin(angle)*radius,3+orbit,0,Math.PI*2); ctx.fill(); }
0051        ctx.fillStyle='#baffef'; ctx.shadowColor='#50f5d6'; ctx.shadowBlur=24; ctx.beginPath(); ctx.arc(0,0,10,0,Math.PI*2); ctx.fill(); ctx.restore();
0052        for (const star of stars) {
0053          const alpha = .35 + .45 * (.5 + .5 * Math.sin(time * .00065 + star.phase));
0054          ctx.fillStyle = `rgba(238,225,250,${alpha})`;
0055          ctx.beginPath(); ctx.arc((star.x * width + time * .0015) % width, star.y * height, star.radius, 0, Math.PI * 2); ctx.fill();
0056        }
0057      }
0058      const animate = (now: number) => { if (now - last >= 33) { time += last ? Math.min(now - last, 70) : 0; last = now; draw(); } frame = requestAnimationFrame(animate); };
0059      const start = () => { cancelAnimationFrame(frame); last = 0; if (!paused && !reducedMotion && !document.hidden) frame = requestAnimationFrame(animate); else draw(); };
0060      resize(); start(); addEventListener("resize", resize); document.addEventListener("visibilitychange", start);
0061      return () => { cancelAnimationFrame(frame); removeEventListener("resize", resize); document.removeEventListener("visibilitychange", start); };
0062    }, [paused, reducedMotion]);
0063    return <><canvas ref={canvas} className={styles.cosmos} aria-hidden="true" />{!reducedMotion && <button className={styles.motion} type="button" onClick={() => setPaused(value => !value)} aria-label={paused ? "Play background animation" : "Pause background animation"}>{paused ? <Play size={13} aria-hidden="true" /> : <Pause size={13} aria-hidden="true" />}<span>{paused ? "Play motion" : "Pause motion"}</span></button>}</>;
0064  }
```

### F05 API forwarding

[apps/web/next.config.ts](<D:/Projects new/Nexora/apps/web/next.config.ts>)

Complete file. 34 source lines shown.

SHA256 `074cd2ab26cf11ba363d3977df22c2ef390fb45b774bb9e5baede47883bbd438`

```typescript
0001  import type { NextConfig } from "next";
0002  import path from "node:path";
0003  
0004  const nextConfig: NextConfig = {
0005    turbopack: { root: path.resolve(__dirname, "../..") },
0006    distDir: process.env.NEXORA_NEXT_DIST_DIR ?? ".next",
0007    poweredByHeader: false,
0008    reactStrictMode: true,
0009    typedRoutes: true,
0010    async rewrites() {
0011      const apiOrigin = process.env.NEXORA_API_URL ?? "http://localhost:5080";
0012  
0013      return [
0014        {
0015          source: "/api/:path*",
0016          destination: `${apiOrigin}/api/:path*`,
0017        },
0018      ];
0019    },
0020    async headers() {
0021      return [
0022        {
0023          source: "/(.*)",
0024          headers: [
0025            { key: "X-Content-Type-Options", value: "nosniff" },
0026            { key: "Referrer-Policy", value: "strict-origin-when-cross-origin" },
0027            { key: "Permissions-Policy", value: "camera=(), microphone=(), geolocation=()" },
0028          ],
0029        },
0030      ];
0031    },
0032  };
0033  
0034  export default nextConfig;
```

### F06 Session checking

[apps/web/src/lib/auth/session.ts](<D:/Projects new/Nexora/apps/web/src/lib/auth/session.ts>)

Complete file. 61 source lines shown.

SHA256 `5f4a2dcc97f5cafa31fc67b3f3dc5d33cd829338628a003bb928bd5523d8d71a`

```typescript
0001  import { cache } from "react";
0002  import { cookies } from "next/headers";
0003  import { redirect } from "next/navigation";
0004  
0005  export type Session = {
0006    userId: string;
0007    displayName: string;
0008    email: string;
0009    tenantId: string;
0010    tenantName: string;
0011    tenantSlug: string;
0012    roles: string[];
0013    permissions: string[];
0014  };
0015  
0016  export type AccessOverview = {
0017    users: Array<{ id: string; displayName: string; email: string; isActive: boolean }>;
0018    roles: Array<{ id: string; name: string; description: string | null; isSystem: boolean }>;
0019    teams: Array<{ id: string; name: string; memberCount: number }>;
0020  };
0021  
0022  const apiOrigin = process.env.NEXORA_API_URL ?? "http://localhost:5080";
0023  
0024  async function authenticatedApiFetch(path: string) {
0025    const cookieStore = await cookies();
0026    const cookieHeader = cookieStore
0027      .getAll()
0028      .map(({ name, value }) => `${name}=${value}`)
0029      .join("; ");
0030  
0031    return fetch(`${apiOrigin}${path}`, {
0032      headers: { cookie: cookieHeader },
0033      cache: "no-store",
0034    });
0035  }
0036  
0037  export const getSession = cache(async (): Promise<Session | null> => {
0038    try {
0039      const response = await authenticatedApiFetch("/api/v1/auth/session");
0040      return response.ok ? ((await response.json()) as Session) : null;
0041    } catch {
0042      return null;
0043    }
0044  });
0045  
0046  export async function requireSession() {
0047    const session = await getSession();
0048    if (!session) {
0049      redirect("/login");
0050    }
0051    return session;
0052  }
0053  
0054  export async function getAccessOverview(): Promise<AccessOverview | null> {
0055    try {
0056      const response = await authenticatedApiFetch("/api/v1/admin/access-overview");
0057      return response.ok ? ((await response.json()) as AccessOverview) : null;
0058    } catch {
0059      return null;
0060    }
0061  }
```

### F07 Protected workspace layout

[apps/web/src/app/(platform)/layout.tsx](<D:/Projects new/Nexora/apps/web/src/app/(platform)/layout.tsx>)

Complete file. 8 source lines shown.

SHA256 `bad4902630adbbcb7ed567f64b014b88ea4be0aa801b2e8a0f87346d3c8f3beb`

```tsx
0001  import type { ReactNode } from "react";
0002  import { AppShell } from "@/components/app-shell/app-shell";
0003  import { requireSession } from "@/lib/auth/session";
0004  
0005  export default async function PlatformLayout({ children }: { children: ReactNode }) {
0006    const session = await requireSession();
0007    return <AppShell session={session}>{children}</AppShell>;
0008  }
```

### F08 Shared logo

[apps/web/src/components/brand/nexora-mark.tsx](<D:/Projects new/Nexora/apps/web/src/components/brand/nexora-mark.tsx>)

Complete file. 14 source lines shown.

SHA256 `0984855cf654c7b9ae9fadf51a22ecb2936e17b93d830c726acbd4960796e7d5`

```tsx
0001  import { Sparkles } from "lucide-react";
0002  
0003  type NexoraMarkProps = { inverse?: boolean; compact?: boolean };
0004  
0005  export function NexoraMark({ inverse = false, compact = false }: NexoraMarkProps) {
0006    return (
0007      <div className="inline-flex items-center gap-3" aria-label="Nexora">
0008        <span className={`grid size-10 place-items-center rounded-[13px] shadow-lg ${inverse ? "bg-white text-[#263a7a] shadow-black/15" : "bg-[#233b81] text-white shadow-[#233b81]/20"}`}>
0009          <Sparkles className="size-[21px]" strokeWidth={2.2} aria-hidden="true" />
0010        </span>
0011        {!compact && <span className={`text-[1.35rem] font-semibold tracking-[-0.045em] ${inverse ? "text-white" : "text-[#17213a]"}`}>Nexora</span>}
0012      </div>
0013    );
0014  }
```

### F09 Shared button

[apps/web/src/components/ui/button.tsx](<D:/Projects new/Nexora/apps/web/src/components/ui/button.tsx>)

Complete file. 20 source lines shown.

SHA256 `b4be2f3d62a5890833a5560763ea484712635f5e9d27e6b8be620ad3cc89638b`

```tsx
0001  import type { ButtonHTMLAttributes } from "react";
0002  import { cn } from "@/lib/cn";
0003  
0004  type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "primary" | "secondary" | "ghost" };
0005  
0006  export function Button({ className, variant = "primary", type = "button", ...props }: ButtonProps) {
0007    return (
0008      <button
0009        type={type}
0010        className={cn(
0011          "inline-flex h-11 items-center justify-center gap-2 rounded-xl px-4 text-sm font-semibold transition disabled:cursor-not-allowed disabled:opacity-60",
0012          variant === "primary" && "bg-[#405bd8] text-white shadow-[0_8px_20px_rgba(64,91,216,.22)] hover:bg-[#334ec5] active:translate-y-px",
0013          variant === "secondary" && "border border-[#dfe4ec] bg-white text-[#263244] hover:border-[#cdd5e0] hover:bg-[#f8fafc]",
0014          variant === "ghost" && "text-[#526174] hover:bg-[#f0f3f8] hover:text-[#1c2738]",
0015          className,
0016        )}
0017        {...props}
0018      />
0019    );
0020  }
```

### F10 Login help page

[apps/web/src/app/(auth)/login/help/page.tsx](<D:/Projects new/Nexora/apps/web/src/app/(auth)/login/help/page.tsx>)

Complete file. 2 source lines shown.

SHA256 `e1ed55203c1f49fbeb23add2dfc114ab70809b9981506566e30ddd32b2bcac49`

```tsx
0001  import Link from "next/link";
0002  export default function LoginHelp(){return <main className="min-h-screen bg-[#0c142b] px-6 py-16 text-slate-100"><article className="mx-auto max-w-xl space-y-6"><p className="text-sm text-indigo-200">Nexora CRM</p><h1 className="text-3xl font-semibold">Login Guide</h1><ol className="list-decimal space-y-4 pl-5"><li>Enter the work email associated with your Nexora account.</li><li>Enter your password. Use the eye icon to check it privately.</li><li>Choose “Keep me signed in” only on a device you trust.</li><li>Select “Sign in”. You will return to your requested page.</li></ol><h2 className="text-xl font-semibold">Having trouble?</h2><p>Check your email and password, then try again. If your account is locked, inactive, or you need a password reset, contact your organisation’s Nexora administrator.</p><p>Microsoft sign-in is available only when your organisation has connected it. An unavailable button means it is not configured.</p><Link href="/login" className="inline-block rounded-lg bg-indigo-500 px-4 py-2">Back to Login</Link></article></main>}
```

### B01 Authentication setup and endpoints

[backend/src/Modules/Nexora.Modules.Identity/IdentityModule.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/IdentityModule.cs>)

Complete file. 349 source lines shown.

SHA256 `74370bc471782926a24fec69ec2e9fd8a7c2d2dc2c89f704a9f5c7f1cb3530e4`

```csharp
0001  using System.Security.Claims;
0002  using Nexora.BuildingBlocks.Security;
0003  using System.Threading.RateLimiting;
0004  using Microsoft.AspNetCore.Antiforgery;
0005  using Microsoft.AspNetCore.Authentication.Cookies;
0006  using Microsoft.AspNetCore.Authorization;
0007  using Microsoft.AspNetCore.Builder;
0008  using Microsoft.AspNetCore.Identity;
0009  using Microsoft.AspNetCore.RateLimiting;
0010  using Microsoft.AspNetCore.Http;
0011  using Microsoft.AspNetCore.Routing;
0012  using Microsoft.EntityFrameworkCore;
0013  using Microsoft.Extensions.Configuration;
0014  using Microsoft.Extensions.DependencyInjection;
0015  using Nexora.Modules.Identity.Domain;
0016  using Nexora.Modules.Identity.Infrastructure.Persistence;
0017  using Nexora.Modules.Identity.Security;
0018  
0019  namespace Nexora.Modules.Identity;
0020  
0021  public static class IdentityModule
0022  {
0023      public static IServiceCollection AddNexoraIdentity(
0024          this IServiceCollection services,
0025          IConfiguration configuration,
0026          bool isDevelopment)
0027      {
0028          var connectionString = configuration.GetConnectionString("Nexora")
0029              ?? throw new InvalidOperationException("Connection string 'Nexora' is required.");
0030  
0031          services.Configure<IdentityProviderOptions>(configuration.GetSection("Identity:Provider"));
0032          services.AddScoped<Nexora.BuildingBlocks.Security.ICrmContextProvider, Nexora.Modules.Identity.Security.CrmPortalContext>();
0033          services.AddSingleton(new LocalIdentitySettings(isDevelopment &&
0034              configuration.GetValue<bool>("Identity:Provider:LocalDevelopmentEnabled")));
0035          services.AddScoped<ILocalIdentityAdapter, LocalIdentityAdapter>();
0036          services.AddHttpContextAccessor();
0037          services.AddSingleton<WorkflowExecutionContext>();
0038          services.AddScoped<IWorkflowPrincipalDirectory, WorkflowPrincipalDirectory>();
0039          services.AddScoped<IRequestIdentity, RequestIdentity>();
0040          services.AddScoped<IIdentityDirectory, IdentityDirectoryService>();
0041          services.AddScoped<IPortalRecipientDirectory, PortalRecipientDirectory>();
0042          services.AddScoped<IWorkflowPortalDirectory, WorkflowPortalDirectory>();
0043          services.AddScoped<IWorkflowActionHandler>(sp=>new WorkflowInspectionAction(new("identity.portal.inspect-contact","Review explicit portal binding and organisation delegation","identity.read",["contactId"],[new("contactId","Contact","reference",true)],["hasLogin","issues","invalidDelegations"],AdditionalPermissions:["crm.read"]),"contactId",async(id,ct)=>{var result=await sp.GetRequiredService<IWorkflowPortalDirectory>().ReviewAsync(id,ct);return new("Portal delegation reviewed without changing grants",new(){{"hasLogin",result.HasLogin?"true":"false"},{"issues",result.Issues},{"invalidDelegations",result.InvalidDelegations.ToString()}});}));
0044          services.AddScoped<IPublicTenantDirectory, PublicTenantDirectory>();
0045          services.AddScoped<IScheduledPublisherDirectory, ScheduledPublisherDirectory>();
0046          services.AddScoped<ITenantContext, HttpTenantContext>();
0047          services.AddDbContext<NexoraIdentityDbContext>(options => options.UseSqlServer(connectionString));
0048  
0049          services
0050              .AddIdentityCore<NexoraUser>(options =>
0051              {
0052                  options.User.RequireUniqueEmail = true;
0053                  options.SignIn.RequireConfirmedAccount = false;
0054                  options.Password.RequiredLength = 12;
0055                  options.Password.RequireDigit = true;
0056                  options.Password.RequireLowercase = true;
0057                  options.Password.RequireUppercase = true;
0058                  options.Password.RequireNonAlphanumeric = true;
0059                  options.Lockout.MaxFailedAccessAttempts = 5;
0060                  options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
0061              })
0062              .AddRoles<NexoraRole>()
0063              .AddSignInManager()
0064              .AddEntityFrameworkStores<NexoraIdentityDbContext>()
0065              .AddDefaultTokenProviders()
0066              .AddClaimsPrincipalFactory<NexoraClaimsPrincipalFactory>();
0067  
0068          services
0069              .AddAuthentication(IdentityConstants.ApplicationScheme)
0070              .AddIdentityCookies();
0071  
0072          services.ConfigureApplicationCookie(options =>
0073          {
0074              options.Events.OnValidatePrincipal = async context =>
0075              {
0076                  // Resolve current tenant-scoped roles on each request: an old cookie must not
0077                  // retain access after its user or security group has been changed.
0078                  var previous = context.HttpContext.User;
0079                  context.HttpContext.User = context.Principal!;
0080                  try
0081                  {
0082                      var signIn = context.HttpContext.RequestServices.GetRequiredService<SignInManager<NexoraUser>>();
0083                      var user = await signIn.ValidateSecurityStampAsync(context.Principal);
0084                      if (user == null || !user.IsActive)
0085                      {
0086                          context.RejectPrincipal();
0087                          return;
0088                      }
0089                      var factory = context.HttpContext.RequestServices.GetRequiredService<IUserClaimsPrincipalFactory<NexoraUser>>();
0090                      var refreshed = await factory.CreateAsync(user);
0091                      context.ShouldRenew = !context.Principal!.Claims.Select(claim => (claim.Type, claim.Value)).OrderBy(claim => claim.Type).ThenBy(claim => claim.Value)
0092                          .SequenceEqual(refreshed.Claims.Select(claim => (claim.Type, claim.Value)).OrderBy(claim => claim.Type).ThenBy(claim => claim.Value));
0093                      context.ReplacePrincipal(refreshed);
0094                  }
0095                  finally { context.HttpContext.User = previous; }
0096              };
0097              options.Cookie.Name = isDevelopment ? "Nexora.Session" : "__Host-Nexora.Session";
0098              options.Cookie.HttpOnly = true;
0099              options.Cookie.SameSite = SameSiteMode.Lax;
0100              options.Cookie.SecurePolicy = isDevelopment
0101                  ? CookieSecurePolicy.SameAsRequest
0102                  : CookieSecurePolicy.Always;
0103              options.ExpireTimeSpan = TimeSpan.FromHours(8);
0104              options.SlidingExpiration = true;
0105              options.Events.OnRedirectToLogin = context =>
0106              {
0107                  context.Response.StatusCode = StatusCodes.Status401Unauthorized;
0108                  return Task.CompletedTask;
0109              };
0110              options.Events.OnRedirectToAccessDenied = context =>
0111              {
0112                  context.Response.StatusCode = StatusCodes.Status403Forbidden;
0113                  return Task.CompletedTask;
0114              };
0115          });
0116  
0117          services.AddScoped<IAuthorizationHandler, ActiveTenantAuthorizationHandler>();
0118          var activeTenantRequirement = new ActiveTenantRequirement();
0119          services.AddAuthorizationBuilder()
0120              .SetDefaultPolicy(new AuthorizationPolicyBuilder()
0121                  .RequireAuthenticatedUser()
0122                  .AddRequirements(activeTenantRequirement)
0123                  .Build())
0124              .AddPolicy(NexoraPermissions.IdentityRead, policy =>
0125                  policy.RequireAuthenticatedUser()
0126                      .AddRequirements(activeTenantRequirement)
0127                      .RequireClaim(NexoraClaimTypes.Permission, NexoraPermissions.IdentityRead))
0128              .AddPolicy(NexoraPermissions.IdentityManage, policy =>
0129                  policy.RequireAuthenticatedUser()
0130                      .AddRequirements(activeTenantRequirement)
0131                      .RequireClaim(NexoraClaimTypes.Permission, NexoraPermissions.IdentityManage))
0132              .AddPolicy(NexoraPermissions.AuditRead, policy =>
0133                  policy.RequireAuthenticatedUser()
0134                      .AddRequirements(activeTenantRequirement)
0135                      .RequireClaim(NexoraClaimTypes.Permission, NexoraPermissions.AuditRead));
0136  
0137          foreach (var permission in NexoraPermissions.All.Skip(3))
0138          {
0139              services.AddAuthorizationBuilder().AddPolicy(permission, policy => policy.RequireAuthenticatedUser()
0140                  .AddRequirements(activeTenantRequirement).RequireClaim(NexoraClaimTypes.Permission, permission));
0141          }
0142  
0143          services.AddAntiforgery(options =>
0144          {
0145              options.HeaderName = "X-CSRF-TOKEN";
0146              options.Cookie.Name = isDevelopment ? "Nexora.Antiforgery" : "__Host-Nexora.Antiforgery";
0147              options.Cookie.HttpOnly = true;
0148              options.Cookie.SameSite = SameSiteMode.Strict;
0149              options.Cookie.SecurePolicy = isDevelopment
0150                  ? CookieSecurePolicy.SameAsRequest
0151                  : CookieSecurePolicy.Always;
0152          });
0153  
0154          services.AddRateLimiter(options =>
0155          {
0156              options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
0157              options.AddPolicy("portal-registration", httpContext => RateLimitPartition.GetFixedWindowLimiter(
0158                  httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
0159                  { PermitLimit = 5, Window = TimeSpan.FromMinutes(15), QueueLimit = 0, AutoReplenishment = true }));
0160              options.AddPolicy("identity-login", httpContext =>
0161                  RateLimitPartition.GetFixedWindowLimiter(
0162                      httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
0163                      _ => new FixedWindowRateLimiterOptions
0164                      {
0165                          PermitLimit = 8,
0166                          Window = TimeSpan.FromMinutes(1),
0167                          QueueLimit = 0,
0168                          AutoReplenishment = true,
0169                      }));
0170          });
0171  
0172          services.AddPortal(isDevelopment);
0173          return services;
0174      }
0175  
0176      public static IEndpointRouteBuilder MapNexoraIdentityEndpoints(this IEndpointRouteBuilder endpoints)
0177      {
0178          var auth = endpoints.MapGroup("/api/v1/auth").WithTags("Identity");
0179  
0180          auth.MapGet("/providers", (LocalIdentitySettings settings) => Results.Ok(new
0181          {
0182              localDevelopmentEnabled = settings.Enabled,
0183              entraEnabled = false,
0184          }));
0185  
0186          auth.MapGet("/csrf", (IAntiforgery antiforgery, HttpContext context) =>
0187          {
0188              var tokens = antiforgery.GetAndStoreTokens(context);
0189              return Results.Ok(new CsrfResponse(tokens.RequestToken!));
0190          });
0191  
0192          auth.MapPost("/login", LoginAsync)
0193              .RequireRateLimiting("identity-login");
0194  
0195          auth.MapGet("/session", GetSessionAsync)
0196              .RequireAuthorization();
0197  
0198          auth.MapPost("/logout", LogoutAsync)
0199              .RequireAuthorization();
0200  
0201          endpoints.MapGet("/api/v1/admin/access-overview", GetAccessOverviewAsync)
0202              .WithTags("Administration")
0203              .RequireAuthorization(NexoraPermissions.IdentityRead);
0204  
0205          endpoints.MapPortal();
0206          endpoints.MapAdministration();
0207          return endpoints;
0208      }
0209  
0210      private static async Task<IResult> LoginAsync(
0211          LoginRequest request,
0212          ILocalIdentityAdapter adapter,
0213          HttpContext httpContext,
0214          IAntiforgery antiforgery,
0215          CancellationToken cancellationToken)
0216      {
0217          if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
0218          {
0219              return Results.ValidationProblem(new Dictionary<string, string[]>
0220              {
0221                  ["credentials"] = ["Email and password are required."],
0222              });
0223          }
0224  
0225          try
0226          {
0227              await antiforgery.ValidateRequestAsync(httpContext);
0228          }
0229          catch (AntiforgeryValidationException)
0230          {
0231              return Results.Problem(
0232                  statusCode: StatusCodes.Status400BadRequest,
0233                  title: "The security token is invalid or expired.");
0234          }
0235  
0236          return await adapter.SignInAsync(request, httpContext, cancellationToken);
0237      }
0238  
0239      private static async Task<IResult> GetSessionAsync(
0240          ClaimsPrincipal principal,
0241          UserManager<NexoraUser> userManager,
0242          NexoraIdentityDbContext dbContext,
0243          CancellationToken cancellationToken)
0244      {
0245          var user = await userManager.GetUserAsync(principal);
0246          if (user is null || !user.IsActive)
0247          {
0248              return Results.Unauthorized();
0249          }
0250  
0251          var tenant = await dbContext.Tenants
0252              .Where(candidate => candidate.Id == user.TenantId && candidate.Status == TenantStatus.Active)
0253              .Select(candidate => new { candidate.Id, candidate.Name, candidate.Slug })
0254              .SingleOrDefaultAsync(cancellationToken);
0255  
0256          if (tenant is null)
0257          {
0258              return Results.Unauthorized();
0259          }
0260  
0261          var roles = await userManager.GetRolesAsync(user);
0262          var permissions = principal.FindAll(NexoraClaimTypes.Permission)
0263              .Select(claim => claim.Value)
0264              .Distinct()
0265              .Order()
0266              .ToArray();
0267  
0268          return Results.Ok(new SessionResponse(
0269              user.Id,
0270              user.DisplayName,
0271              user.Email!,
0272              tenant.Id,
0273              tenant.Name,
0274              tenant.Slug,
0275              roles.Order().ToArray(),
0276              permissions));
0277      }
0278  
0279      private static async Task<IResult> LogoutAsync(
0280          HttpContext httpContext,
0281          IAntiforgery antiforgery,
0282          SignInManager<NexoraUser> signInManager)
0283      {
0284          try
0285          {
0286              await antiforgery.ValidateRequestAsync(httpContext);
0287          }
0288          catch (AntiforgeryValidationException)
0289          {
0290              return Results.Problem(
0291                  statusCode: StatusCodes.Status400BadRequest,
0292                  title: "The security token is invalid or expired.");
0293          }
0294  
0295          await signInManager.SignOutAsync();
0296          return Results.NoContent();
0297      }
0298  
0299      private static async Task<IResult> GetAccessOverviewAsync(
0300          ClaimsPrincipal principal,
0301          UserManager<NexoraUser> userManager,
0302          RoleManager<NexoraRole> roleManager,
0303          NexoraIdentityDbContext dbContext,
0304          CancellationToken cancellationToken)
0305      {
0306          var tenantId = Guid.Parse(principal.FindFirstValue(NexoraClaimTypes.TenantId)!);
0307  
0308          var users = await userManager.Users
0309              .Where(user => user.TenantId == tenantId)
0310              .OrderBy(user => user.DisplayName)
0311              .Select(user => new AccessUser(user.Id, user.DisplayName, user.Email!, user.IsActive))
0312              .ToListAsync(cancellationToken);
0313  
0314          var roles = await roleManager.Roles
0315              .Where(role => role.TenantId == tenantId)
0316              .OrderBy(role => role.Name)
0317              .Select(role => new AccessRole(role.Id, role.Name!, role.Description, role.IsSystem))
0318              .ToListAsync(cancellationToken);
0319  
0320          var teams = await dbContext.Teams
0321              .Where(team => team.TenantId == tenantId)
0322              .OrderBy(team => team.Name)
0323              .Select(team => new AccessTeam(team.Id, team.Name, team.Members.Count))
0324              .ToListAsync(cancellationToken);
0325  
0326          return Results.Ok(new AccessOverviewResponse(users, roles, teams));
0327      }
0328  
0329  
0330  }
0331  
0332  public sealed record LoginRequest(string Email, string Password, bool RememberMe);
0333  public sealed record CsrfResponse(string Token);
0334  public sealed record SessionResponse(
0335      Guid UserId,
0336      string DisplayName,
0337      string Email,
0338      Guid TenantId,
0339      string TenantName,
0340      string TenantSlug,
0341      IReadOnlyList<string> Roles,
0342      IReadOnlyList<string> Permissions);
0343  public sealed record AccessUser(Guid Id, string DisplayName, string Email, bool IsActive);
0344  public sealed record AccessRole(Guid Id, string Name, string? Description, bool IsSystem);
0345  public sealed record AccessTeam(Guid Id, string Name, int MemberCount);
0346  public sealed record AccessOverviewResponse(
0347      IReadOnlyList<AccessUser> Users,
0348      IReadOnlyList<AccessRole> Roles,
0349      IReadOnlyList<AccessTeam> Teams);
```

### B02 Local credential verification

[backend/src/Modules/Nexora.Modules.Identity/Security/LocalIdentityAdapter.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/LocalIdentityAdapter.cs>)

Complete file. 85 source lines shown.

SHA256 `0084e28ef4e105778c76493dd01d1db938f6b05f698663bfa5d99795967b5694`

```csharp
0001  using Microsoft.AspNetCore.Http;
0002  using Microsoft.AspNetCore.Identity;
0003  using Microsoft.EntityFrameworkCore;
0004  using Nexora.Modules.Identity.Domain;
0005  using Nexora.Modules.Identity.Infrastructure.Persistence;
0006  
0007  namespace Nexora.Modules.Identity.Security;
0008  
0009  public interface ILocalIdentityAdapter
0010  {
0011      Task<IResult> SignInAsync(LoginRequest request, HttpContext context, CancellationToken cancellationToken);
0012  }
0013  
0014  internal sealed record LocalIdentitySettings(bool Enabled);
0015  
0016  internal sealed class LocalIdentityAdapter(
0017      LocalIdentitySettings settings,
0018      UserManager<NexoraUser> userManager,
0019      SignInManager<NexoraUser> signInManager,
0020      NexoraIdentityDbContext dbContext) : ILocalIdentityAdapter
0021  {
0022      public async Task<IResult> SignInAsync(LoginRequest request, HttpContext httpContext, CancellationToken cancellationToken)
0023      {
0024          if (!settings.Enabled) return Results.Problem(statusCode: 503, title: "Local sign-in is disabled. Microsoft Entra sign-in is not yet connected.");
0025          using var systemAccess = dbContext.BeginSystemAccess();
0026          var normalizedEmail = userManager.NormalizeEmail(request.Email.Trim());
0027          var user = await userManager.Users.SingleOrDefaultAsync(
0028              candidate => candidate.NormalizedEmail == normalizedEmail,
0029              cancellationToken);
0030  
0031          var tenantIsActive = user is not null && await dbContext.Tenants.AnyAsync(
0032              tenant => tenant.Id == user.TenantId && tenant.Status == TenantStatus.Active,
0033              cancellationToken);
0034  
0035          if (user is null || !user.IsActive || !tenantIsActive)
0036          {
0037              await RecordLoginAsync(dbContext, null, request.Email, "denied", httpContext, cancellationToken);
0038              return Results.Problem(
0039                  statusCode: StatusCodes.Status401Unauthorized,
0040                  title: "Email or password is incorrect.");
0041          }
0042  
0043          var result = await signInManager.PasswordSignInAsync(
0044              user,
0045              request.Password,
0046              request.RememberMe,
0047              lockoutOnFailure: true);
0048  
0049          if (!result.Succeeded)
0050          {
0051              await RecordLoginAsync(dbContext, user, user.Email, result.IsLockedOut ? "locked" : "denied", httpContext, cancellationToken);
0052              return Results.Problem(
0053                  statusCode: StatusCodes.Status401Unauthorized,
0054                  title: result.IsLockedOut
0055                      ? "This account is temporarily locked. Try again later."
0056                      : "Email or password is incorrect.");
0057          }
0058  
0059          await RecordLoginAsync(dbContext, user, user.Email, "succeeded", httpContext, cancellationToken);
0060          return Results.Ok(new { authenticated = true });
0061      }
0062  
0063      private static async Task RecordLoginAsync(
0064          NexoraIdentityDbContext dbContext,
0065          NexoraUser? user,
0066          string? subject,
0067          string outcome,
0068          HttpContext httpContext,
0069          CancellationToken cancellationToken)
0070      {
0071          var normalizedSubject = subject?.Trim().ToLowerInvariant();
0072          dbContext.AuditEvents.Add(new AuditEvent
0073          {
0074              TenantId = user?.TenantId,
0075              ActorUserId = user?.Id,
0076              EventType = "identity.login",
0077              Outcome = outcome,
0078              Subject = normalizedSubject is null
0079                  ? null
0080                  : normalizedSubject[..Math.Min(normalizedSubject.Length, 320)],
0081              IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
0082          });
0083          await dbContext.SaveChangesAsync(cancellationToken);
0084      }
0085  }
```

### B03 User record

[backend/src/Modules/Nexora.Modules.Identity/Domain/NexoraUser.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Domain/NexoraUser.cs>)

Complete file. 11 source lines shown.

SHA256 `562cd034e854ddb42a961675780c55b0e2aa0f148fad2d1963025c7c10b99128`

```csharp
0001  using Microsoft.AspNetCore.Identity;
0002  
0003  namespace Nexora.Modules.Identity.Domain;
0004  
0005  public sealed class NexoraUser : IdentityUser<Guid>
0006  {
0007      public Guid TenantId { get; set; }
0008      public required string DisplayName { get; set; }
0009      public bool IsActive { get; set; } = true;
0010      public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
0011  }
```

### B04 Tenant and permission claims

[backend/src/Modules/Nexora.Modules.Identity/Security/NexoraClaimsPrincipalFactory.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/NexoraClaimsPrincipalFactory.cs>)

Complete file. 45 source lines shown.

SHA256 `6e21b92f5e837cd8219e32b6151135c8d5c89a27fb09d5a13b93f93e8c808c39`

```csharp
0001  using System.Security.Claims;
0002  using Microsoft.AspNetCore.Identity;
0003  using Microsoft.EntityFrameworkCore;
0004  using Microsoft.Extensions.Options;
0005  using Nexora.Modules.Identity.Domain;
0006  using Nexora.Modules.Identity.Infrastructure.Persistence;
0007  
0008  namespace Nexora.Modules.Identity.Security;
0009  
0010  internal sealed class NexoraClaimsPrincipalFactory(
0011      UserManager<NexoraUser> userManager,
0012      RoleManager<NexoraRole> roleManager,
0013      IOptions<IdentityOptions> options,
0014      NexoraIdentityDbContext dbContext)
0015      : UserClaimsPrincipalFactory<NexoraUser, NexoraRole>(userManager, roleManager, options)
0016  {
0017      protected override async Task<ClaimsIdentity> GenerateClaimsAsync(NexoraUser user)
0018      {
0019          var identity = await base.GenerateClaimsAsync(user);
0020          identity.AddClaim(new Claim(NexoraClaimTypes.TenantId, user.TenantId.ToString()));
0021          identity.AddClaim(new Claim(NexoraClaimTypes.DisplayName, user.DisplayName));
0022  
0023          var roleNames = await UserManager.GetRolesAsync(user);
0024          if (roleNames.Count == 0)
0025          {
0026              return identity;
0027          }
0028  
0029          var isAdmin = await dbContext.Roles.AnyAsync(role => role.IsSystem && role.TenantId == user.TenantId && roleNames.Contains(role.Name!));
0030          var permissions = isAdmin ? NexoraPermissions.All.ToList() : await dbContext.RolePermissions
0031              .Where(permission =>
0032                  permission.Role.TenantId == user.TenantId &&
0033                  roleNames.Contains(permission.Role.Name!))
0034              .Select(permission => permission.Permission)
0035              .Distinct()
0036              .ToListAsync();
0037  
0038          foreach (var permission in permissions)
0039          {
0040              identity.AddClaim(new Claim(NexoraClaimTypes.Permission, permission));
0041          }
0042  
0043          return identity;
0044      }
0045  }
```

### B05 Active tenant authorisation

[backend/src/Modules/Nexora.Modules.Identity/Security/ActiveTenantAuthorization.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/ActiveTenantAuthorization.cs>)

Complete file. 35 source lines shown.

SHA256 `71e1caa617f72d190ca5aadc89433aec8f984c9c0c5263a9dd0faa1eddffa0aa`

```csharp
0001  using System.Security.Claims;
0002  using Microsoft.AspNetCore.Authorization;
0003  using Microsoft.EntityFrameworkCore;
0004  using Nexora.Modules.Identity.Domain;
0005  using Nexora.Modules.Identity.Infrastructure.Persistence;
0006  
0007  namespace Nexora.Modules.Identity.Security;
0008  
0009  internal sealed class ActiveTenantRequirement : IAuthorizationRequirement;
0010  
0011  internal sealed class ActiveTenantAuthorizationHandler(NexoraIdentityDbContext dbContext)
0012      : AuthorizationHandler<ActiveTenantRequirement>
0013  {
0014      protected override async Task HandleRequirementAsync(
0015          AuthorizationHandlerContext context,
0016          ActiveTenantRequirement requirement)
0017      {
0018          if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ||
0019              !Guid.TryParse(context.User.FindFirstValue(NexoraClaimTypes.TenantId), out var tenantId))
0020          {
0021              return;
0022          }
0023  
0024          var hasActiveBoundary = await dbContext.Users
0025              .Where(user => user.Id == userId && user.TenantId == tenantId && user.IsActive)
0026              .AnyAsync() && await dbContext.Tenants
0027              .Where(tenant => tenant.Id == tenantId && tenant.Status == TenantStatus.Active)
0028              .AnyAsync();
0029  
0030          if (hasActiveBoundary)
0031          {
0032              context.Succeed(requirement);
0033          }
0034      }
0035  }
```

### B06 API startup

[backend/src/Nexora.Api/Program.cs](<D:/Projects new/Nexora/backend/src/Nexora.Api/Program.cs>)

Complete file. 126 source lines shown.

SHA256 `ced3e2c3a7318361abebe285299108af8749da7016151db590405b9eac3d15ac`

```csharp
0001  using Nexora.Modules.Workflow;
0002  using Nexora.Modules.Content;
0003  using Nexora.Api;
0004  using System.Reflection;
0005  using Nexora.Modules.Work;
0006  using Nexora.Modules.Work.Infrastructure;
0007  using Nexora.Modules.Engagement;
0008  using Nexora.Modules.Engagement.Infrastructure;
0009  using Nexora.Modules.Finance;
0010  using Nexora.Modules.Finance.Infrastructure;
0011  using Nexora.Modules.Events;
0012  using Nexora.Modules.Events.Infrastructure;
0013  using Nexora.Modules.Membership;
0014  using Nexora.Modules.Membership.Infrastructure;
0015  using Nexora.Modules.Crm;
0016  using Nexora.Modules.Crm.Infrastructure;
0017  using Microsoft.EntityFrameworkCore;
0018  using Microsoft.AspNetCore.Diagnostics.HealthChecks;
0019  using Nexora.BuildingBlocks.Platform;
0020  using Nexora.Modules.Identity;
0021  using Nexora.Modules.Identity.Infrastructure.Persistence;
0022  
0023  var builder = WebApplication.CreateBuilder(args);
0024  
0025  builder.Services.AddProblemDetails();
0026  builder.Services.AddOpenApi();
0027  builder.Services.AddHealthChecks();
0028  builder.Services.AddNexoraIdentity(builder.Configuration, builder.Environment.IsDevelopment());
0029  builder.Services.AddNexoraCrm(builder.Configuration);
0030  builder.Services.AddNexoraContent(builder.Configuration);
0031  builder.Services.AddNexoraMembership(builder.Configuration);
0032  builder.Services.AddNexoraEvents(builder.Configuration);
0033  builder.Services.AddNexoraFinance(builder.Configuration);
0034  builder.Services.AddNexoraWork(builder.Configuration);
0035  builder.Services.AddNexoraEngagement(builder.Configuration);
0036  builder.Services.AddNexoraWorkflow(builder.Configuration);
0037  builder.Services.AddCors(options =>
0038  {
0039      options.AddPolicy("NexoraWeb", policy =>
0040      {
0041          policy
0042              .WithOrigins(builder.Configuration.GetSection("Web:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
0043              .AllowAnyHeader()
0044              .AllowAnyMethod()
0045              .AllowCredentials();
0046      });
0047  });
0048  
0049  var app = builder.Build();
0050  
0051  app.UseNexoraTelemetry();
0052  app.UseExceptionHandler(new ExceptionHandlerOptions
0053  {
0054      StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
0055          ? badRequest.StatusCode
0056          : StatusCodes.Status500InternalServerError
0057  });
0058  app.UseHttpsRedirection();
0059  app.UseCors("NexoraWeb");
0060  app.UseRateLimiter();
0061  app.UseAuthentication();
0062  app.UseAuthorization();
0063  
0064  if (app.Environment.IsDevelopment())
0065  {
0066      app.MapOpenApi();
0067  }
0068  
0069  app.MapHealthChecks("/health", new HealthCheckOptions
0070  {
0071      ResponseWriter = async (context, report) =>
0072      {
0073          context.Response.ContentType = "application/json";
0074          await context.Response.WriteAsJsonAsync(new
0075          {
0076              status = report.Status.ToString().ToLowerInvariant(),
0077              service = "nexora-api",
0078              checkedAt = DateTimeOffset.UtcNow,
0079          });
0080      },
0081  });
0082  
0083  app.MapGet("/api/v1/platform", () =>
0084  {
0085      var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
0086      return Results.Ok(new PlatformDescriptor(
0087          "Nexora",
0088          "nexora-api",
0089          version,
0090          ["platform-shell", "identity", "tenancy", "crm", "membership", "events", "finance", "engagement", "work", "insights"]));
0091  })
0092  .WithName("GetPlatformDescriptor")
0093  .WithTags("Platform");
0094  
0095  app.MapNexoraIdentityEndpoints();
0096  app.MapNexoraCrm();
0097  app.MapNexoraContent();
0098  app.MapNexoraMembership();
0099  app.MapNexoraEvents();
0100  app.MapNexoraFinance();
0101  app.MapNexoraWork();
0102  app.MapNexoraEngagement();
0103  app.MapNexoraWorkflow();
0104  app.MapNexoraReporting();
0105  app.MapNexoraReadiness();
0106  
0107  if (app.Environment.IsDevelopment())
0108  {
0109      await IdentitySeeder.SeedDevelopmentIdentityAsync(app.Services, builder.Configuration);
0110      if (!string.IsNullOrWhiteSpace(builder.Configuration["Identity:SeedAdminPassword"]))
0111      {
0112          await using var scope = app.Services.CreateAsyncScope();
0113          await scope.ServiceProvider.GetRequiredService<CrmDbContext>().Database.MigrateAsync();
0114          await scope.ServiceProvider.GetRequiredService<ContentDbContext>().Database.MigrateAsync();
0115          await scope.ServiceProvider.GetRequiredService<MembershipDbContext>().Database.MigrateAsync();
0116          await scope.ServiceProvider.GetRequiredService<EventsDbContext>().Database.MigrateAsync();
0117          await scope.ServiceProvider.GetRequiredService<FinanceDbContext>().Database.MigrateAsync();
0118          await scope.ServiceProvider.GetRequiredService<WorkDbContext>().Database.MigrateAsync();
0119          await scope.ServiceProvider.GetRequiredService<EngagementDbContext>().Database.MigrateAsync();
0120          await scope.ServiceProvider.GetRequiredService<WorkflowDbContext>().Database.MigrateAsync();
0121      }
0122  }
0123  
0124  app.Run();
0125  
0126  public partial class Program;
```

### B07 Identity database mapping

[backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/NexoraIdentityDbContext.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/NexoraIdentityDbContext.cs>)

Complete file. 142 source lines shown.

SHA256 `24c52bc4dded12afb54f2859d0c91a61464a4e7ab46e7be9a43a760e62399f51`

```csharp
0001  using Nexora.BuildingBlocks.Persistence;
0002  using Microsoft.AspNetCore.Identity;
0003  using Nexora.Modules.Identity.Security;
0004  using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
0005  using Microsoft.EntityFrameworkCore;
0006  using Nexora.Modules.Identity.Domain;
0007  
0008  namespace Nexora.Modules.Identity.Infrastructure.Persistence;
0009  
0010  public sealed partial class NexoraIdentityDbContext(DbContextOptions<NexoraIdentityDbContext> options, ITenantContext? tenantContext = null)
0011      : IdentityDbContext<NexoraUser, NexoraRole, Guid>(options)
0012  {
0013      public DbSet<Tenant> Tenants => Set<Tenant>();
0014      public DbSet<PortalAccess> PortalAccess => Set<PortalAccess>();
0015      public DbSet<Team> Teams => Set<Team>();
0016      public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
0017      public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
0018      public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
0019  
0020      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { base.OnConfiguring(optionsBuilder); optionsBuilder.AddInterceptors(new DataAuditInterceptor(()=>CurrentTenantId)); }
0021      protected override void OnModelCreating(ModelBuilder builder)
0022      {
0023          base.OnModelCreating(builder);
0024          builder.Entity<Tenant>().HasQueryFilter(x => SystemAccess || (x.Id == CurrentTenantId));
0025          builder.Entity<NexoraUser>().HasQueryFilter(x => SystemAccess || (x.TenantId == CurrentTenantId));
0026          builder.Entity<NexoraRole>().HasQueryFilter(x => SystemAccess || (x.TenantId == CurrentTenantId));
0027          builder.Entity<Team>().HasQueryFilter(x => SystemAccess || (x.TenantId == CurrentTenantId));
0028          builder.Entity<AuditEvent>().HasQueryFilter(x => SystemAccess || (CurrentTenantId != null && x.TenantId == CurrentTenantId));
0029          builder.Entity<TeamMember>().HasQueryFilter(x => SystemAccess || (Teams.Any(t => t.Id == x.TeamId) && Users.Any(u => u.Id == x.UserId)));
0030          builder.Entity<RolePermission>().HasQueryFilter(x => SystemAccess || (Roles.Any(r => r.Id == x.RoleId)));
0031          builder.Entity<IdentityUserRole<Guid>>().HasQueryFilter(x => SystemAccess || (Users.Any(u => u.Id == x.UserId) && Roles.Any(r => r.Id == x.RoleId)));
0032          builder.Entity<IdentityUserClaim<Guid>>().HasQueryFilter(x => SystemAccess || (Users.Any(u => u.Id == x.UserId)));
0033          builder.Entity<IdentityUserLogin<Guid>>().HasQueryFilter(x => SystemAccess || (Users.Any(u => u.Id == x.UserId)));
0034          builder.Entity<IdentityUserToken<Guid>>().HasQueryFilter(x => SystemAccess || (Users.Any(u => u.Id == x.UserId)));
0035          builder.Entity<IdentityRoleClaim<Guid>>().HasQueryFilter(x => SystemAccess || (Roles.Any(r => r.Id == x.RoleId)));
0036  
0037  
0038          builder.HasDefaultSchema("identity");
0039          builder.Entity<PortalOrganisationRequest>(entity =>
0040          {
0041              entity.ToTable("PortalOrganisationRequests");
0042              entity.HasKey(x => x.Id);
0043              entity.HasQueryFilter(x => SystemAccess || x.TenantId == CurrentTenantId);
0044              entity.Property(x => x.OrganisationName).HasMaxLength(160);
0045              entity.Property(x => x.NormalizedName).HasMaxLength(160);
0046              entity.Property(x => x.JobTitle).HasMaxLength(160);
0047              entity.Property(x => x.Message).HasMaxLength(2000);
0048              entity.Property(x => x.DecisionMessage).HasMaxLength(1000);
0049              entity.Property(x => x.Status).HasMaxLength(20);
0050              entity.Property(x => x.Version).IsConcurrencyToken();
0051              entity.HasIndex(x => new { x.TenantId, x.PortalAccessId, x.NormalizedName }).IsUnique().HasFilter("[Status] = 'pending'");
0052              entity.HasIndex(x => new { x.TenantId, x.Status, x.CreatedAtUtc });
0053              entity.HasOne<PortalAccess>().WithMany().HasForeignKey(x => new { x.TenantId, x.PortalAccessId })
0054                  .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
0055          });
0056          builder.Entity<PortalOrganisationGrant>(entity =>
0057          {
0058              entity.ToTable("PortalOrganisationGrants");
0059              entity.HasKey(x => x.Id);
0060              entity.HasQueryFilter(x => SystemAccess || x.TenantId == CurrentTenantId);
0061              entity.HasIndex(x => new { x.TenantId, x.PortalAccessId, x.AccountId }).IsUnique().HasFilter("[IsRevoked] = 0");
0062              entity.HasOne<PortalAccess>().WithMany().HasForeignKey(x => new { x.TenantId, x.PortalAccessId })
0063                  .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
0064          });
0065          builder.Entity<PortalAccess>(entity =>
0066          {
0067              entity.ToTable("PortalAccess");
0068              entity.HasKey(x => x.Id);
0069              entity.HasQueryFilter(x => SystemAccess || x.TenantId == CurrentTenantId);
0070              entity.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique().HasFilter("[IsRevoked] = 0");
0071              entity.HasIndex(x => new { x.TenantId, x.ContactId }).IsUnique().HasFilter("[IsRevoked] = 0");
0072              entity.HasOne<NexoraUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
0073              entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
0074          });
0075          builder.Entity<Tenant>(entity =>
0076          {
0077              entity.ToTable("Tenants");
0078              entity.Property(x => x.Name).HasMaxLength(160);
0079              entity.Property(x => x.Slug).HasMaxLength(80);
0080              entity.HasIndex(x => x.Slug).IsUnique();
0081          });
0082  
0083          builder.Entity<NexoraUser>(entity =>
0084          {
0085              entity.ToTable("Users");
0086              entity.Property(x => x.DisplayName).HasMaxLength(160);
0087              entity.HasIndex(x => x.NormalizedEmail).IsUnique();
0088              entity.HasIndex(x => new { x.TenantId, x.NormalizedEmail });
0089              entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
0090          });
0091  
0092          builder.Entity<NexoraRole>(entity =>
0093          {
0094              entity.ToTable("Roles");
0095              entity.Property(x => x.Description).HasMaxLength(300);
0096              entity.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
0097              entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
0098          });
0099  
0100          builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
0101          builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
0102          builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
0103          builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
0104          builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
0105  
0106          builder.Entity<Team>(entity =>
0107          {
0108              entity.ToTable("Teams");
0109              entity.Property(x => x.Name).HasMaxLength(160);
0110              entity.Property(x => x.Description).HasMaxLength(500);
0111              entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
0112              entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
0113          });
0114  
0115          builder.Entity<TeamMember>(entity =>
0116          {
0117              entity.ToTable("TeamMembers");
0118              entity.HasKey(x => new { x.TeamId, x.UserId });
0119              entity.HasOne(x => x.Team).WithMany(x => x.Members).HasForeignKey(x => x.TeamId);
0120              entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
0121          });
0122  
0123          builder.Entity<RolePermission>(entity =>
0124          {
0125              entity.ToTable("RolePermissions");
0126              entity.HasKey(x => new { x.RoleId, x.Permission });
0127              entity.Property(x => x.Permission).HasMaxLength(120);
0128              entity.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
0129          });
0130  
0131          builder.Entity<AuditEvent>(entity =>
0132          {
0133              entity.ToTable("AuditEvents");
0134              entity.Property(x => x.EventType).HasMaxLength(120);
0135              entity.Property(x => x.Outcome).HasMaxLength(40);
0136              entity.Property(x => x.Subject).HasMaxLength(320);
0137              entity.Property(x => x.IpAddress).HasMaxLength(64);
0138              entity.HasIndex(x => new { x.TenantId, x.OccurredAtUtc });
0139          });
0140          DataAuditModel.Configure(builder,()=>CurrentTenantId);
0141      }
0142  }
```

### B08 Development identity seed

[backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/IdentitySeeder.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/IdentitySeeder.cs>)

Complete file. 108 source lines shown.

SHA256 `13cd7fb9a4b30877e70eee1281218cf1058a082f063a324b6286db4bce0c02c0`

```csharp
0001  using Microsoft.AspNetCore.Identity;
0002  using Microsoft.EntityFrameworkCore;
0003  using Microsoft.Extensions.Configuration;
0004  using Microsoft.Extensions.DependencyInjection;
0005  using Nexora.Modules.Identity.Domain;
0006  using Nexora.Modules.Identity.Security;
0007  
0008  namespace Nexora.Modules.Identity.Infrastructure.Persistence;
0009  
0010  public static class IdentitySeeder
0011  {
0012      public static async Task SeedDevelopmentIdentityAsync(
0013          IServiceProvider services,
0014          IConfiguration configuration,
0015          CancellationToken cancellationToken = default)
0016      {
0017          var password = configuration["Identity:SeedAdminPassword"];
0018          if (string.IsNullOrWhiteSpace(password))
0019          {
0020              return;
0021          }
0022  
0023          await using var scope = services.CreateAsyncScope();
0024          var dbContext = scope.ServiceProvider.GetRequiredService<NexoraIdentityDbContext>();
0025          using var systemAccess = dbContext.BeginSystemAccess();
0026          await dbContext.Database.MigrateAsync(cancellationToken);
0027  
0028          const string tenantSlug = "nexora-demo";
0029          var tenant = await dbContext.Tenants.SingleOrDefaultAsync(x => x.Slug == tenantSlug, cancellationToken);
0030          if (tenant is null)
0031          {
0032              tenant = new Tenant { Name = "Nexora Demo", Slug = tenantSlug };
0033              dbContext.Tenants.Add(tenant);
0034              await dbContext.SaveChangesAsync(cancellationToken);
0035          }
0036  
0037          var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<NexoraRole>>();
0038          var role = await roleManager.Roles.SingleOrDefaultAsync(
0039              x => x.TenantId == tenant.Id && x.NormalizedName == "PLATFORM ADMINISTRATOR",
0040              cancellationToken);
0041          if (role is null)
0042          {
0043              role = new NexoraRole
0044              {
0045                  TenantId = tenant.Id,
0046                  Name = "Platform Administrator",
0047                  Description = "Full tenant administration access",
0048                  IsSystem = true,
0049              };
0050              var roleResult = await roleManager.CreateAsync(role);
0051              EnsureSucceeded(roleResult);
0052          }
0053  
0054          foreach (var permission in NexoraPermissions.All)
0055          {
0056              if (!await dbContext.RolePermissions.AnyAsync(
0057                      x => x.RoleId == role.Id && x.Permission == permission,
0058                      cancellationToken))
0059              {
0060                  dbContext.RolePermissions.Add(new RolePermission { RoleId = role.Id, Permission = permission });
0061              }
0062          }
0063  
0064          var email = configuration["Identity:SeedAdminEmail"] ?? "admin@nexora.local";
0065          var userManager = scope.ServiceProvider.GetRequiredService<UserManager<NexoraUser>>();
0066          var user = await userManager.FindByEmailAsync(email);
0067          if (user is null)
0068          {
0069              user = new NexoraUser
0070              {
0071                  TenantId = tenant.Id,
0072                  DisplayName = "Nexora Administrator",
0073                  UserName = email,
0074                  Email = email,
0075                  EmailConfirmed = true,
0076              };
0077              var userResult = await userManager.CreateAsync(user, password);
0078              EnsureSucceeded(userResult);
0079              EnsureSucceeded(await userManager.AddToRoleAsync(user, role.Name!));
0080          }
0081          else if (configuration.GetValue<bool>("Identity:ResetSeedAdminPassword"))
0082          {
0083              EnsureSucceeded(await userManager.RemovePasswordAsync(user));
0084              EnsureSucceeded(await userManager.AddPasswordAsync(user, password));
0085          }
0086  
0087          if (!await dbContext.Teams.AnyAsync(x => x.TenantId == tenant.Id, cancellationToken))
0088          {
0089              dbContext.Teams.Add(new Team
0090              {
0091                  TenantId = tenant.Id,
0092                  Name = "Customer Success",
0093                  Description = "Owns customer relationships and retention",
0094                  Members = [new TeamMember { UserId = user.Id }],
0095              });
0096          }
0097  
0098          await dbContext.SaveChangesAsync(cancellationToken);
0099      }
0100  
0101      private static void EnsureSucceeded(IdentityResult result)
0102      {
0103          if (!result.Succeeded)
0104          {
0105              throw new InvalidOperationException(string.Join("; ", result.Errors.Select(error => error.Description)));
0106          }
0107      }
0108  }
```

### S01 Root layout and title

[apps/web/src/app/layout.tsx](<D:/Projects new/Nexora/apps/web/src/app/layout.tsx>)

Complete file. 19 source lines shown.

SHA256 `7f00111a66595d803aa5d35ca5830a36a049e80a112c053bac106f30c8d33945`

```tsx
0001  import type { Metadata } from "next";
0002  import { Geist, Geist_Mono } from "next/font/google";
0003  import "./globals.css";
0004  
0005  const geistSans = Geist({ variable: "--font-geist-sans", subsets: ["latin"] });
0006  const geistMono = Geist_Mono({ variable: "--font-geist-mono", subsets: ["latin"] });
0007  
0008  export const metadata: Metadata = {
0009    title: { default: "Nexora", template: "%s · Nexora" },
0010    description: "One connected workspace for relationships, operations and insight.",
0011  };
0012  
0013  export default function RootLayout({ children }: LayoutProps<"/">) {
0014    return (
0015      <html lang="en" suppressHydrationWarning className={`${geistSans.variable} ${geistMono.variable}`}>
0016        <body suppressHydrationWarning>{children}</body>
0017      </html>
0018    );
0019  }
```

### S02 Global imports and base styles

[apps/web/src/app/globals.css](<D:/Projects new/Nexora/apps/web/src/app/globals.css>)

Excerpt with original line numbers. 42 source lines shown.

SHA256 `9c04f768a779994d3706361af15759c263f3c6a32227951f6e79f086478130db`

```css
0001  @import "tailwindcss";
0002  @import "../components/app-shell/workspace-theme.css";
0003  
0004  :root {
0005    --background: #f5f7fb;
0006    --foreground: #182230;
0007    --surface: #ffffff;
0008    --border: #e4e8ef;
0009    --muted: #667085;
0010    --brand-100: #dfe9ff;
0011    --brand-500: #4f6ff5;
0012    --brand-600: #405bd8;
0013    --brand-700: #3348ac;
0014    --navy-900: #111c35;
0015  }
0016  
0017  @theme inline {
0018    --color-background: var(--background);
0019    --color-foreground: var(--foreground);
0020    --font-sans: var(--font-geist-sans);
0021    --font-mono: var(--font-geist-mono);
0022  }
0023  
0024  * { box-sizing: border-box; }
0025  html { min-height: 100%; background: var(--background); }
0026  body {
0027    min-height: 100vh;
0028    margin: 0;
0029    background: var(--background);
0030    color: var(--foreground);
0031    font-family: var(--font-geist-sans), Arial, Helvetica, sans-serif;
0032    text-rendering: optimizeLegibility;
0033  }
0034  button, input, select, textarea { font-family: inherit; }
0035  button, a { -webkit-tap-highlight-color: transparent; }
0036  ::selection { background: var(--brand-100); color: var(--navy-900); }
0037  :focus-visible { outline: 3px solid rgba(79, 111, 245, 0.28); outline-offset: 2px; }
0038  
0039  .login-aurora {
0040    background:
0041      radial-gradient(circle at 88% 8%, rgba(73, 126, 255, 0.28), transparent 22%),
0042      radial-gradient(circle at 10% 92%, rgba(54, 91, 186, 0.2), transparent 30%),
```

### S03 Shared default colour variables

[apps/web/src/components/app-shell/workspace-theme.css](<D:/Projects new/Nexora/apps/web/src/components/app-shell/workspace-theme.css>)

Excerpt with original line numbers. 8 source lines shown.

SHA256 `951ce3c28b6c959ec2eb002c8c504712a8a0a31ca040838ded754801ccfc26bb`

```css
0001  /* Shared CRM palette, matching the website studio and neon member portal. */
0002  :root {
0003    --nexora-canvas: #09090b;
0004    --nexora-foreground: #fafafa;
0005    --nexora-accent: #50f5d6;
0006    --nexora-secondary: #9255ff;
0007  }
0008  
```

### S04 Class merge helper

[apps/web/src/lib/cn.ts](<D:/Projects new/Nexora/apps/web/src/lib/cn.ts>)

Complete file. 6 source lines shown.

SHA256 `d1f1e0d62cb8d8d1e04c26e14de842d8a151f75812d81b046c65b5d1fe8e4b27`

```typescript
0001  import { clsx, type ClassValue } from "clsx";
0002  import { twMerge } from "tailwind-merge";
0003  
0004  export function cn(...inputs: ClassValue[]) {
0005    return twMerge(clsx(inputs));
0006  }
```

### S05 Development provider configuration

[backend/src/Nexora.Api/appsettings.Development.json](<D:/Projects new/Nexora/backend/src/Nexora.Api/appsettings.Development.json>)

Complete file. 13 source lines shown.

SHA256 `aedeba9f45956dc58421eaef17a1d37d9e676f2f4accd9879de05921c9dd3fe3`

```json
0001  {
0002    "Logging": {
0003      "LogLevel": {
0004        "Default": "Information",
0005        "Microsoft.AspNetCore": "Warning"
0006      }
0007    },
0008    "Identity": {
0009      "Provider": {
0010        "LocalDevelopmentEnabled": true
0011      }
0012    }
0013  }
```

### S06 Provider option model

[backend/src/Modules/Nexora.Modules.Identity/Security/IdentityProviderOptions.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/IdentityProviderOptions.cs>)

Complete file. 15 source lines shown.

SHA256 `044e62011106f3da2a20453ff1812dccdd71c4eaa0811a8757932c936f52beed`

```csharp
0001  namespace Nexora.Modules.Identity.Security;
0002  
0003  // Configuration for the forthcoming OIDC adapter. Local development needs no Entra credentials.
0004  public sealed class IdentityProviderOptions
0005  {
0006      public bool LocalDevelopmentEnabled { get; set; }
0007      public EntraOptions Entra { get; set; } = new();
0008  }
0009  
0010  public sealed class EntraOptions
0011  {
0012      public string? TenantId { get; set; }
0013      public string? ClientId { get; set; }
0014      public string CallbackPath { get; set; } = "/signin-oidc";
0015  }
```

### S07 Permission names and tenant context

[backend/src/Modules/Nexora.Modules.Identity/Security/NexoraSecurity.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Security/NexoraSecurity.cs>)

Complete file. 34 source lines shown.

SHA256 `77eb5c3eb13674526401fdccc894708219af89d7c984979133c9cf9e62193e02`

```csharp
0001  using System.Security.Claims;
0002  using Microsoft.AspNetCore.Http;
0003  
0004  namespace Nexora.Modules.Identity.Security;
0005  
0006  public static class NexoraClaimTypes
0007  {
0008      public const string TenantId = "tenant_id";
0009      public const string DisplayName = "display_name";
0010      public const string Permission = "permission";
0011  }
0012  
0013  public static class NexoraPermissions
0014  {
0015      public const string IdentityRead = "identity.read";
0016      public const string IdentityManage = "identity.manage";
0017      public const string AuditRead = "audit.read";
0018  
0019      public static readonly string[] All = [IdentityRead, IdentityManage, AuditRead, "crm.read", "crm.manage", "crm.configure", "crm.files", "membership.read", "membership.manage", "membership.decide", "events.read", "events.manage", "events.results", "finance.read", "finance.manage", "finance.post", "finance.export", "engagement.read", "engagement.manage", "engagement.fundraise", "work.read", "work.manage", "work.import", "work.export", "insights.read", "insights.export", "content.read", "content.manage", "content.publish", "workflows.read", "workflows.create", "workflows.edit", "workflows.publish", "workflows.activate", "workflows.delete", "workflows.execute", "workflows.history", "workflows.retry", "workflows.cancel", "workflows.integrations", "workflows.ai", "workflows.approve"];
0020  }
0021  
0022  public interface ITenantContext
0023  {
0024      Guid? TenantId { get; }
0025  }
0026  
0027  internal sealed class HttpTenantContext(IHttpContextAccessor accessor, Nexora.BuildingBlocks.Security.WorkflowExecutionContext execution) : ITenantContext
0028  {
0029      public Guid? TenantId => accessor.HttpContext?.User.Identity?.IsAuthenticated == true && Guid.TryParse(
0030          accessor.HttpContext?.User.FindFirstValue(NexoraClaimTypes.TenantId),
0031          out var tenantId)
0032          ? tenantId
0033          : execution.Current?.TenantId;
0034  }
```

### S08 Tenant record

[backend/src/Modules/Nexora.Modules.Identity/Domain/Tenant.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Domain/Tenant.cs>)

Complete file. 16 source lines shown.

SHA256 `77e1c6e45431f6186e692a4c17cf9bcf4fe5597b49bb59766e6ea8dbe81403e7`

```csharp
0001  namespace Nexora.Modules.Identity.Domain;
0002  
0003  public enum TenantStatus
0004  {
0005      Active = 1,
0006      Suspended = 2,
0007  }
0008  
0009  public sealed class Tenant
0010  {
0011      public Guid Id { get; set; } = Guid.NewGuid();
0012      public required string Name { get; set; }
0013      public required string Slug { get; set; }
0014      public TenantStatus Status { get; set; } = TenantStatus.Active;
0015      public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
0016  }
```

### S09 Audit record

[backend/src/Modules/Nexora.Modules.Identity/Domain/AuditEvent.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Domain/AuditEvent.cs>)

Complete file. 13 source lines shown.

SHA256 `1974bd1f0f1a53f47896d09b67ab5ef246bfb099c529e5722390bcdd81c7714a`

```csharp
0001  namespace Nexora.Modules.Identity.Domain;
0002  
0003  public sealed class AuditEvent
0004  {
0005      public long Id { get; set; }
0006      public Guid? TenantId { get; set; }
0007      public Guid? ActorUserId { get; set; }
0008      public required string EventType { get; set; }
0009      public required string Outcome { get; set; }
0010      public string? Subject { get; set; }
0011      public string? IpAddress { get; set; }
0012      public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
0013  }
```

### S10 System access scope

[backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/TenantWriteBoundary.cs](<D:/Projects new/Nexora/backend/src/Modules/Nexora.Modules.Identity/Infrastructure/Persistence/TenantWriteBoundary.cs>)

Excerpt with original line numbers. 24 source lines shown.

SHA256 `dbd7b6d08602d0ed0d907f963ebc97bbb76bc853060d162202ee389bb6bc0cc5`

```csharp
0001  using Microsoft.EntityFrameworkCore;
0002  using Microsoft.EntityFrameworkCore.ChangeTracking;
0003  using Nexora.Modules.Identity.Domain;
0004  
0005  namespace Nexora.Modules.Identity.Infrastructure.Persistence;
0006  
0007  public sealed partial class NexoraIdentityDbContext
0008  {
0009      private Guid? CurrentTenantId => tenantContext?.TenantId;
0010      internal bool SystemAccess { get; private set; }
0011  
0012      // Restricted to bootstrap, credential verification and host-resolved registration, never a caller-supplied tenant.
0013      internal IDisposable BeginSystemAccess()
0014      {
0015          var previous = SystemAccess;
0016          SystemAccess = true;
0017          return new Scope(() => { ChangeTracker.Clear(); SystemAccess = previous; });
0018      }
0019  
0020      private sealed class Scope(Action restore) : IDisposable
0021      {
0022          public void Dispose() => restore();
0023      }
0024  
```

### S11 Protected dashboard route

[apps/web/src/app/(platform)/dashboard/page.tsx](<D:/Projects new/Nexora/apps/web/src/app/(platform)/dashboard/page.tsx>)

Complete file. 3 source lines shown.

SHA256 `f26c3f6a3aeb43d1e7924a77d16b878e79790902880e678a4f2e754286cf3774`

```tsx
0001  import { requireSession } from "@/lib/auth/session";
0002  import { LiveDashboard } from "@/features/insights/live-dashboard";
0003  export default async function Dashboard(){const session=await requireSession();return <LiveDashboard name={session.displayName} allowed={session.permissions.includes("insights.read")} />;}
```

### S12 Staff sign out handler

[apps/web/src/components/app-shell/app-shell.tsx](<D:/Projects new/Nexora/apps/web/src/components/app-shell/app-shell.tsx>)

Excerpt with original line numbers. 15 source lines shown.

SHA256 `f71536c468698009067c830ceb19d467276e7aabd157ba26c6418d56c0f5c990`

```tsx
0042    async function signOut() {
0043      setIsSigningOut(true);
0044      try {
0045        const csrf = await fetch("/api/v1/auth/csrf", { credentials: "include" });
0046        const { token } = (await csrf.json()) as { token: string };
0047        await fetch("/api/v1/auth/logout", {
0048          method: "POST",
0049          credentials: "include",
0050          headers: { "X-CSRF-TOKEN": token },
0051        });
0052      } finally {
0053        router.replace("/login");
0054        router.refresh();
0055      }
0056    }
```

<!-- END GENERATED LOGIN SOURCE -->
