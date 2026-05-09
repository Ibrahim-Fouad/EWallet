# Frontend Migration Progress

Tracks the JSX → Angular port from `EWallet-Screens/` into this `frontend/` workspace.

## Status at a glance

| Area | Status | Notes |
|---|---|---|
| Scaffold (standalone, routing, bootstrap) | done | NgModule scaffold removed; `bootstrapApplication` + `provideRouter` |
| Global CSS (tokens + components) | done | Loaded via `src/styles.css` |
| Icon library | done | Single `<app-icon name="..." />` component, all 38 icons |
| Shared UI primitives | done | Field, Toggle, StatusBadge, CurrencyBadge, Amount, WalletCard, StatCard, Modal, Drawer, EmptyState, Skeleton, ToastStack |
| Shared widgets | done | TxTable, WalletPicker |
| Core services (AppState + Auth) | done — mocked | Signal-based; auth uses 700ms `setTimeout`, no backend yet |
| Layout chrome | done | Sidebar, TopBar (with bell + avatar dropdowns), Breadcrumbs, AppShell |
| Auth feature | done — mocked | `/login`, `/register`, navigates to `/dashboard` after submit |
| Dashboard | done | KPIs, balance chart (inline SVG sparkline), quick actions, recent tx |
| Wallets feature | done | List, CreateWalletModal, WalletDetail with tabbed activity |
| Transactions feature | done | Deposit (form → success), Transfer (form → confirm modal → success/fail) |
| History feature | done | Filters, pagination, drawer with TxDetail; opens via `?txId=...` |
| Notifications | done | List + mark-as-read; empty state |
| Profile + Settings | done | Edit-in-place profile, settings modals (change password, log out) |
| Real backend wiring (Identity, OpenIddict) | **todo** | Replace mocked AuthService, add HTTP interceptor, route guards |
| Vitest specs | **todo** | None ported — original `app.spec.ts` was deleted |

## Project layout

```
frontend/src/
├── main.ts
├── styles.css
├── styles/
│   ├── tokens.css
│   └── components.css
└── app/
    ├── app.ts                              # standalone root: <router-outlet/> + <app-toast-stack/>
    ├── app.config.ts                       # provideRouter, provideZonelessChangeDetection
    ├── app.routes.ts                       # /login, /register, AppShell wrapper for authed routes
    ├── core/services/
    │   ├── app-state.service.ts            # signal-based seed data + actions
    │   └── auth.service.ts                 # mocked login/register
    ├── shared/
    │   ├── icons/icon.component.ts
    │   ├── layout/
    │   │   ├── app-shell.component.ts      # Sidebar + TopBar + <router-outlet/>
    │   │   ├── sidebar.component.ts
    │   │   ├── topbar.component.ts
    │   │   └── breadcrumbs.component.ts
    │   └── ui/                             # Field, Toggle, badges, Amount, WalletCard,
    │                                       # StatCard, Modal, Drawer, EmptyState, Skeleton,
    │                                       # ToastStack, TxTable, WalletPicker
    └── features/
        ├── auth/{auth-illustration,login,register}/
        ├── dashboard/{dashboard,balance-chart}.component.ts
        ├── wallets/{wallets,wallet-detail,create-wallet-modal}.component.ts
        ├── transactions/{deposit,transfer}.component.ts
        ├── history/{history,tx-detail,notifications}.component.ts
        └── profile/{profile,settings}.component.ts
```

## Routes

| Path | Component | Notes |
|---|---|---|
| `/login` | LoginComponent | Lazy |
| `/register` | RegisterComponent | Lazy |
| `/` (AppShell) | AppShellComponent | Lazy parent, hosts `<router-outlet/>` |
| `/dashboard` | DashboardComponent | Lazy child |
| `/wallets` | WalletsComponent | Lazy child |
| `/wallets/:id` | WalletDetailComponent | Lazy child |
| `/transfer?fromWalletId=...` | TransferComponent | Lazy child |
| `/deposit?walletId=...` | DepositComponent | Lazy child |
| `/history?txId=...` | HistoryComponent | Lazy child; opens TxDetail drawer when `txId` present |
| `/notifications` | NotificationsComponent | Lazy child |
| `/profile` | ProfileComponent | Lazy child |
| `/settings` | SettingsComponent | Lazy child |
| `**` | redirect → `/login` | |

## Conventions (don't deviate without a reason)

These come from [.claude/CLAUDE.md](.claude/CLAUDE.md). Every component already follows them.

- **Standalone components only.** Never set `standalone: true`.
- `changeDetection: ChangeDetectionStrategy.OnPush` on every `@Component`.
- **Inputs/outputs:** `input()`, `input.required()`, `output()` — never decorators.
- **State:** `signal()` local, `computed()` derived. `set` / `update`, never `mutate`.
- **Forms:** Reactive (`FormBuilder.nonNullable.group({...})`). `ToggleComponent` is a CVA — bind with `formControlName`.
- **Templates:** `@if`, `@for`, `@switch`. Class bindings (`[class.error]`), style bindings (`[style.background]`). No `ngClass`/`ngStyle`.
- **Services:** `@Injectable({ providedIn: 'root' })`, use `inject()`.
- **Use existing CSS classes.** The visual system lives in `src/styles/components.css`. Don't reinvent.
- **Icons:** `<app-icon name="..." [size]="..." />`. Names listed in `IconName` type.
- **Form errors:** mirror JSX feel — track `submitted = signal(false)`, render errors only when `submitted() && control.invalid`.
- **Accessibility:** every input has `<label for>` + `aria-invalid` + `aria-describedby`. Decorative panels (auth illustration) are `aria-hidden="true"`. Run AXE devtools when shipping a screen.

## What's left

### 1. Real backend wiring

The mocked `AuthService` is a 700ms timer. When ready to wire the real backend:

- Replace [auth.service.ts](src/app/core/services/auth.service.ts) bodies with `HttpClient` calls to `POST /connect/token` (form-encoded, OpenIddict password grant) and `POST /api/v1/identity/register`.
- Add `provideHttpClient(withInterceptors([authInterceptor, correlationIdInterceptor]))` to [app.config.ts](src/app/app.config.ts).
- `authInterceptor` injects `Authorization: Bearer <token>` from `AuthService.authenticated` plus a stored token signal.
- `correlationIdInterceptor` adds `X-Correlation-Id: crypto.randomUUID()` per request — required by the backend correlation middleware.
- Add a `canActivate` guard for the layout shell route that checks `AuthService.authenticated`.
- Configure `proxy.conf.json` so `/api` and `/connect` forward to the backend.
- Wire transactions: replace the mock `deposit`/`transfer` in `AppStateService` with HTTP calls to the Wallets/Transactions modules.
- Replace the mock `simulateInbound` with a SignalR client connected to the Notifications hub.

### 2. Tests

No vitest specs were ported. Set up:
- Unit specs for `AppStateService` (toast lifecycle, transfer/deposit edge cases, validation) — pure signal logic, easy to test.
- Component specs for at least Login, Register, Transfer (validation, confirmation modal flow), History (filtering + pagination).

### 3. Known cosmetic / behavioural gaps vs. JSX prototype

- `TweaksUI` panel (accent-color picker, density toggle, sidebar dark/light) is **not** ported — it was a dev tool, not part of the product. Skip permanently.
- `MiniSparkline` (small chart inside StatCard) is not ported. Only the larger `BalanceChart` is. Add later if needed.
- The "fade-in on route change" animation from JSX is not wired — Angular's router doesn't fade by default. Could add `@.disabled` + a `.fade-in` class on the outlet host.
- TopBar dropdowns close on outside click but do not trap focus (low priority for v1).

## How to run

```powershell
cd frontend
npm install   # only first time
npm start     # http://localhost:4200
npm run build # production build (current size: ~75 kB initial transfer)
```

## Reference

- Original JSX prototype: `EWallet-Screens/`
- Conventions: [.claude/CLAUDE.md](.claude/CLAUDE.md)
- Original migration plan: `C:\Users\imashad\.claude\plans\d-personal-e-wallet-ewallet-screens-i-h-lively-summit.md`
