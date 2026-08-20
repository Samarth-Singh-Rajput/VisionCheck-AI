# VisionCheck AI — Blazor WebAssembly client

Frontend for an industrial surface-inspection system. This is a **standalone Blazor
WebAssembly (.NET 8) client only** — it contains no backend code and consumes the
inspection API purely as an external REST service over `HttpClient`.

---

## Running it

```bash
cd VisionCheckAI.Client
dotnet restore
dotnet run
```

The dev server starts on <https://localhost:7285> (see `Properties/launchSettings.json`).

## Testing the frontend on its own (no backend)

`wwwroot/appsettings.json` ships with `"UseFakeData": true`, so
`dotnet run` gives you the whole UI backed by in-memory data — no API, no database,
no mock server. Every screen, filter and action works.

**Signing in:** any password is accepted. The *username* picks your role, so you can
check the role gating:

| Username contains | Role you get | Override button |
| ----------------- | ------------ | --------------- |
| `admin` | Administrator | visible |
| `super` | Supervisor | visible |
| anything else | Inspector | hidden, with a note |
| `fail` | — | shows the invalid-credentials error |

**What to click through:**

- **Dashboard** — KPIs, all three charts, and the Refresh button. Calls are delayed
  by 300–900 ms on purpose so you can see the loading skeletons.
- **New inspection** — pick a product, then drag an image onto the dropzone or click
  to browse. Sample images are in `wwwroot/img/samples/`. The *filename* decides the
  verdict: anything containing `rust`, `scratch`, `deform` or `fracture` comes back
  **Defective** with bounding boxes drawn over your image; anything else passes. The
  inspection call takes 1.4–2.6 s so the pending skeleton is clearly visible.
- **Review** — Confirm works for everyone; Override only appears for
  Supervisor/Administrator. Both update the panel and raise a toast.
- **History** — 140 seeded records across 6 pages. Every filter works (product,
  date range, category, severity, result), paging works, and filtering to a product
  that has no rows shows the empty state. Click any row for the detail drawer —
  check that Escape closes it and that focus lands inside.
- **Theme** — the sun/moon toggle in the top bar; reload to confirm it persisted.
- **Responsive** — narrow the window below 1024px and the sidebar collapses to an
  icon rail.

Uploads and reviews you perform are held in memory and show up in History and on the
Dashboard immediately. A page refresh resets to the seeded data.

**Switching to the real API:** set `"UseFakeData": false` in
`wwwroot/appsettings.json` and point `BaseUrl` at your backend. The fakes
are only registered when the flag is true, and live in `Services/Fakes/` — delete
that folder and the toggle in `Program.cs` when you no longer need them.

## Pointing it at the API

The API host is read from configuration and is **never hardcoded**. Edit
`wwwroot/appsettings.json`:

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7080/"
  }
}
```

`wwwroot/appsettings.Development.json` and `wwwroot/appsettings.Production.json`
override this per environment; Blazor loads the matching file automatically based on
`ASPNETCORE_ENVIRONMENT`. A trailing slash is added if you omit it.

Because the client and API run on different origins in development, the API must
allow this origin via CORS and expose the `Authorization` header.

## Endpoints consumed

| Method | Path | Used by |
| ------ | ---- | ------- |
| `POST` | `/api/auth/login` | Login |
| `GET`  | `/api/products` | New inspection, History filter |
| `POST` | `/api/inspections/upload` (multipart: `file`, `productId`) | New inspection |
| `POST` | `/api/inspections/{id}/review` | Review panel |
| `GET`  | `/api/inspections` (`productId`, `fromUtc`, `toUtc`, `defectCategory`, `severity`, `result`, `page`, `pageSize`) | History |
| `GET`  | `/api/dashboard/summary` | Dashboard |

---

## Project layout

```
VisionCheckAI.Client/
├── Models/          DTOs matching the API contract
├── Services/        Typed API clients, auth state, storage, toasts
├── Pages/           One .razor per route
│   ├── Login.razor
│   ├── Dashboard.razor
│   └── Inspection/  New.razor, History.razor
├── Shared/          Layout, Sidebar, TopBar, Drawer, Toast, badges, icons
│   └── Charts/      Hand-rolled SVG charts
└── wwwroot/
    ├── css/         theme.css (tokens), layout.css, components.css
    ├── js/app.js    localStorage + theme interop
    └── appsettings*.json
```

## Authentication

- `POST /api/auth/login` returns a JWT. The token and user (id, username, display
  name, role) are stored in `localStorage` and restored **before first render**, so
  route guards never flash the login page on refresh.
- `AuthTokenHandler` (a `DelegatingHandler`) attaches `Authorization: Bearer …` to
  every outgoing request.
- A `401` on any request clears the session and redirects to `/login?returnUrl=…`.
  The login call itself is exempt, so bad credentials show an inline error rather
  than triggering a redirect loop.
- `VisionCheckAuthStateProvider` projects the session into a `ClaimsPrincipal`.
  Protected pages use `@attribute [Authorize]`; `/login` redirects to `/dashboard`
  when already signed in.
- Roles: **Inspector**, **Supervisor**, **Administrator**. Confirming an AI result is
  open to any signed-in user; **Override** is gated to Supervisor/Administrator via
  `<AuthorizeView Roles="Supervisor,Administrator">`.

If the login response omits user details, they are read from the JWT claims
(`sub`, `unique_name`, `name`, `role`, `exp`) as a fallback.

## Design

Plain CSS with custom properties — no Bootstrap, no component library.

- **Theme**: dark (`#0A0A0B`) by default with a light mode toggle, persisted in
  `localStorage` and applied by an inline script before first paint so there is no
  flash. Tokens live in `wwwroot/css/theme.css`.
- **Accent**: one muted amber, used only for primary actions, the active nav marker,
  and chart data. Status colours are desaturated green/red; severity runs
  yellow → orange → red.
- **Type**: Inter only, with a deliberate scale and tabular numerals on every
  figure so columns align.
- **Spacing**: a 4/8/12/16/24/32px rhythm exposed as `--s-1` … `--s-8`.
- **Icons**: an inline SVG set (`Shared/Icon.razor`), Lucide/Feather style. No emoji.
- **Charts**: hand-rolled SVG — thin lines, muted gridlines, no chart junk. Strokes
  use `vector-effect: non-scaling-stroke` so they stay hairline at any width.
- **Detections**: bounding boxes render as machine-vision corner brackets rather
  than plain rectangles.
- Responsive to tablet width: below 1024px the sidebar collapses to an icon rail and
  the top bar takes over section naming.
- Semantic HTML, associated labels, visible focus rings, ARIA on the drawer and
  toasts, and `prefers-reduced-motion` respected.

## Notes for the backend developer

Response shapes were inferred from the endpoint contract, so the client is
deliberately tolerant rather than brittle. It will accept either shape in each case
below without changes:

| Field | Accepted |
| ----- | -------- |
| Identifiers (`id`, `productId`) | JSON string **or** number |
| `confidence` | `0–1` fraction **or** `0–100` percentage |
| `boundingBox` | normalised `0–1` **or** source pixels (needs `imageWidth`/`imageHeight`) |
| `GET /api/inspections` | paged envelope `{ items, page, pageSize, totalCount }` **or** a bare array |
| `result` | `Pass` / `Defective` (also accepts `Fail`, `Reject`) |
| Errors | `detail`, `title`, `message`, or `error` — whichever is present is shown to the user |

Defect categories in the History filter match the classifier's classes:
`Deformation`, `Fracture`, `Rusting`, `Scratches`. (`Excellent` is the pass class and
is not offered as a defect filter.)

Upload is sent as `multipart/form-data` with the image under the field name `file`
and the product under `productId`. The client caps uploads at 10 MB.
