import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'auth/callback',
    loadComponent: () =>
      import('./features/auth/callback/callback.component').then((m) => m.CallbackComponent),
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then((m) => m.LoginComponent),
    data: { title: 'Sign in' },
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then(
        (m) => m.RegisterComponent,
      ),
    data: { title: 'Create account' },
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./shared/layout/app-shell.component').then((m) => m.AppShellComponent),
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
        data: { title: 'Dashboard' },
      },
      {
        path: 'wallets',
        loadComponent: () =>
          import('./features/wallets/wallets.component').then((m) => m.WalletsComponent),
        data: { title: 'My Wallets' },
      },
      {
        path: 'wallets/:id',
        loadComponent: () =>
          import('./features/wallets/wallet-detail.component').then(
            (m) => m.WalletDetailComponent,
          ),
        data: { title: 'Wallet Detail' },
      },
      {
        path: 'transfer',
        loadComponent: () =>
          import('./features/transactions/transfer.component').then(
            (m) => m.TransferComponent,
          ),
        data: { title: 'Transfer Money' },
      },
      {
        path: 'deposit',
        loadComponent: () =>
          import('./features/transactions/deposit.component').then(
            (m) => m.DepositComponent,
          ),
        data: { title: 'Deposit Funds' },
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./features/history/history.component').then((m) => m.HistoryComponent),
        data: { title: 'Transaction History' },
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('./features/history/notifications.component').then(
            (m) => m.NotificationsComponent,
          ),
        data: { title: 'Notifications' },
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('./features/profile/profile.component').then((m) => m.ProfileComponent),
        data: { title: 'Profile' },
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/profile/settings.component').then((m) => m.SettingsComponent),
        data: { title: 'Settings' },
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
