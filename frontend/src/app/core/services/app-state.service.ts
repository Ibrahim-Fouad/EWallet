import { Injectable, computed, signal } from '@angular/core';

import { Wallet, WalletColor } from '../../shared/ui/wallet-card.component';

export interface User {
  id: string;
  fullName: string;
  email: string;
  phone: string;
  joined: string;
  avatar: string;
}

export type TransactionType = 'in' | 'out' | 'deposit';
export type TransactionStatus = 'completed' | 'pending' | 'failed';

export interface Transaction {
  id: string;
  type: TransactionType;
  walletId: string;
  counter: string;
  counterName: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  at: string;
  note?: string;
  failReason?: string;
}

export type NotificationKind = 'received' | 'completed' | 'deposit' | 'failed';

export interface AppNotification {
  id: string;
  kind: NotificationKind;
  title: string;
  body: string;
  at: string;
  read: boolean;
}

export type ToastKind = 'received' | 'success' | 'error' | 'info';

export interface Toast {
  id: string;
  kind: ToastKind;
  title: string;
  body?: string;
  duration?: number;
}

export interface Counterpart {
  name: string;
  phone: string;
}

const SEED_USER: User = {
  id: 'usr_001',
  fullName: 'Yara Mansour',
  email: 'yara.mansour@example.com',
  phone: '+20 100 234 5678',
  joined: '2024-08-12',
  avatar: 'YM',
};

const SEED_WALLETS: (Wallet & { id: string; created: string })[] = [
  { id: 'w1', phone: '01012345678', currency: 'EGP', balance: 18420.75, status: 'active', primary: true, color: 'blue', created: '2024-08-12' },
  { id: 'w2', phone: '01198765432', currency: 'USD', balance: 1250.40, status: 'active', primary: false, color: 'indigo', created: '2024-11-03' },
];

export const COUNTERPARTS: readonly Counterpart[] = [
  { name: 'Omar Khaled', phone: '01055443322' },
  { name: 'Layla Hassan', phone: '01187766554' },
  { name: 'Mariam Sayed', phone: '01233445566' },
  { name: 'Ahmed Tarek', phone: '01099887766' },
  { name: 'Nour Adel', phone: '01566778899' },
  { name: 'Hany Fouad', phone: '01122334455' },
];

const SEED_TX: Transaction[] = [
  { id: 'tx_8821', type: 'in', walletId: 'w1', counter: '01055443322', counterName: 'Omar Khaled', amount: 500, currency: 'EGP', status: 'completed', at: '2026-05-07T10:14:00', note: 'Lunch split' },
  { id: 'tx_8819', type: 'out', walletId: 'w1', counter: '01187766554', counterName: 'Layla Hassan', amount: 1200, currency: 'EGP', status: 'completed', at: '2026-05-07T08:42:00', note: 'Rent share' },
  { id: 'tx_8815', type: 'in', walletId: 'w2', counter: '01233445566', counterName: 'Mariam Sayed', amount: 75, currency: 'USD', status: 'completed', at: '2026-05-06T19:30:00' },
  { id: 'tx_8810', type: 'deposit', walletId: 'w1', counter: 'Bank transfer', counterName: 'Bank deposit', amount: 5000, currency: 'EGP', status: 'completed', at: '2026-05-06T11:05:00' },
  { id: 'tx_8807', type: 'out', walletId: 'w1', counter: '01099887766', counterName: 'Ahmed Tarek', amount: 320, currency: 'EGP', status: 'pending', at: '2026-05-06T09:15:00' },
  { id: 'tx_8801', type: 'out', walletId: 'w2', counter: '01566778899', counterName: 'Nour Adel', amount: 150, currency: 'USD', status: 'completed', at: '2026-05-05T16:22:00' },
  { id: 'tx_8795', type: 'in', walletId: 'w1', counter: '01122334455', counterName: 'Hany Fouad', amount: 2200, currency: 'EGP', status: 'completed', at: '2026-05-05T13:48:00' },
  { id: 'tx_8788', type: 'out', walletId: 'w1', counter: '01055443322', counterName: 'Omar Khaled', amount: 80, currency: 'EGP', status: 'failed', at: '2026-05-04T22:11:00', failReason: 'Recipient wallet inactive' },
  { id: 'tx_8780', type: 'in', walletId: 'w1', counter: '01233445566', counterName: 'Mariam Sayed', amount: 1500, currency: 'EGP', status: 'completed', at: '2026-05-04T15:00:00' },
  { id: 'tx_8772', type: 'deposit', walletId: 'w2', counter: 'Bank transfer', counterName: 'Bank deposit', amount: 500, currency: 'USD', status: 'completed', at: '2026-05-03T10:30:00' },
  { id: 'tx_8765', type: 'out', walletId: 'w1', counter: '01187766554', counterName: 'Layla Hassan', amount: 240, currency: 'EGP', status: 'completed', at: '2026-05-02T18:45:00' },
  { id: 'tx_8760', type: 'in', walletId: 'w1', counter: '01099887766', counterName: 'Ahmed Tarek', amount: 600, currency: 'EGP', status: 'completed', at: '2026-05-02T12:10:00' },
];

const SEED_NOTIFS: AppNotification[] = [
  { id: 'n1', kind: 'received', title: 'You received 500.00 EGP', body: 'From Omar Khaled · 01055443322', at: '2026-05-07T10:14:00', read: false },
  { id: 'n2', kind: 'completed', title: 'Transfer completed', body: '1,200.00 EGP sent to Layla Hassan', at: '2026-05-07T08:42:00', read: false },
  { id: 'n3', kind: 'received', title: 'You received 75.00 USD', body: 'From Mariam Sayed · 01233445566', at: '2026-05-06T19:30:00', read: true },
  { id: 'n4', kind: 'deposit', title: 'Deposit completed', body: '5,000.00 EGP added to Wallet 01012345678', at: '2026-05-06T11:05:00', read: true },
  { id: 'n5', kind: 'failed', title: 'Transfer failed', body: 'Recipient wallet inactive — 80.00 EGP', at: '2026-05-04T22:11:00', read: true },
];

export function fmtAmount(n: number, currency = 'EGP'): string {
  const formatted = n.toLocaleString('en-US', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
  return `${formatted} ${currency}`;
}

export function fmtSigned(n: number, currency: string, type: TransactionType): string {
  const sign = type === 'in' || type === 'deposit' ? '+' : '−';
  return `${sign}${fmtAmount(n, currency)}`;
}

export function relTime(iso: string): string {
  const d = new Date(iso);
  const now = new Date('2026-05-07T11:00:00');
  const diff = (now.getTime() - d.getTime()) / 1000;
  if (diff < 60) return 'just now';
  if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
  if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
  if (diff < 86400 * 7) return `${Math.floor(diff / 86400)}d ago`;
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

export function fmtDateTime(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

@Injectable({ providedIn: 'root' })
export class AppStateService {
  readonly user = signal<User>(SEED_USER);
  readonly wallets = signal<(Wallet & { id: string; created: string })[]>(SEED_WALLETS);
  readonly transactions = signal<Transaction[]>(SEED_TX);
  readonly notifications = signal<AppNotification[]>(SEED_NOTIFS);
  readonly toasts = signal<Toast[]>([]);

  readonly unreadCount = computed(
    () => this.notifications().filter((n) => !n.read).length
  );

  pushToast(toast: Omit<Toast, 'id'>): void {
    const id = 't_' + Math.random().toString(36).slice(2, 8);
    this.toasts.update((ts) => [...ts, { id, ...toast }]);
    setTimeout(() => this.dismissToast(id), toast.duration ?? 5000);
  }

  dismissToast(id: string): void {
    this.toasts.update((ts) => ts.filter((t) => t.id !== id));
  }

  markAllRead(): void {
    this.notifications.update((ns) => ns.map((n) => ({ ...n, read: true })));
  }

  markRead(id: string): void {
    this.notifications.update((ns) =>
      ns.map((n) => (n.id === id ? { ...n, read: true } : n))
    );
  }

  createWallet({ phone, currency }: { phone: string; currency: string }) {
    const id = 'w' + (this.wallets().length + 1);
    const w = {
      id,
      phone,
      currency,
      balance: 0,
      status: 'active',
      primary: false,
      color: 'teal' as WalletColor,
      created: new Date().toISOString(),
    };
    this.wallets.update((ws) => [...ws, w]);
    return w;
  }

  deposit({ walletId, amount }: { walletId: string; amount: number }) {
    const w = this.wallets().find((x) => x.id === walletId);
    if (!w) return { ok: false as const, reason: 'Wallet not found' };
    this.wallets.update((ws) =>
      ws.map((x) => (x.id === walletId ? { ...x, balance: x.balance + amount } : x))
    );
    const tx: Transaction = {
      id: 'tx_' + Math.floor(9000 + Math.random() * 999),
      type: 'deposit',
      walletId,
      counter: 'Bank transfer',
      counterName: 'Bank deposit',
      amount,
      currency: w.currency,
      status: 'completed',
      at: new Date().toISOString(),
    };
    this.transactions.update((ts) => [tx, ...ts]);
    return { ok: true as const, tx };
  }

  transfer({
    fromWalletId,
    toPhone,
    amount,
    simulateFail,
  }: {
    fromWalletId: string;
    toPhone: string;
    amount: number;
    simulateFail?: boolean;
  }) {
    const w = this.wallets().find((x) => x.id === fromWalletId);
    if (!w) return { ok: false as const, reason: 'Source wallet not found' };
    if (amount > w.balance) return { ok: false as const, reason: 'Insufficient balance' };
    const counterName =
      COUNTERPARTS.find((c) => c.phone === toPhone)?.name ?? 'Recipient';
    if (simulateFail) {
      const tx: Transaction = {
        id: 'tx_' + Math.floor(9000 + Math.random() * 999),
        type: 'out',
        walletId: fromWalletId,
        counter: toPhone,
        counterName,
        amount,
        currency: w.currency,
        status: 'failed',
        at: new Date().toISOString(),
        failReason: 'Recipient wallet not found or inactive',
      };
      this.transactions.update((ts) => [tx, ...ts]);
      return { ok: false as const, reason: tx.failReason!, tx };
    }
    this.wallets.update((ws) =>
      ws.map((x) => (x.id === fromWalletId ? { ...x, balance: x.balance - amount } : x))
    );
    const tx: Transaction = {
      id: 'tx_' + Math.floor(9000 + Math.random() * 999),
      type: 'out',
      walletId: fromWalletId,
      counter: toPhone,
      counterName,
      amount,
      currency: w.currency,
      status: 'completed',
      at: new Date().toISOString(),
    };
    this.transactions.update((ts) => [tx, ...ts]);
    return { ok: true as const, tx };
  }

  simulateInbound(): void {
    const c = COUNTERPARTS[Math.floor(Math.random() * COUNTERPARTS.length)];
    const w = this.wallets()[0];
    const amount = Math.floor(50 + Math.random() * 500);
    this.wallets.update((ws) =>
      ws.map((x) => (x.id === w.id ? { ...x, balance: x.balance + amount } : x))
    );
    const tx: Transaction = {
      id: 'tx_' + Math.floor(9000 + Math.random() * 999),
      type: 'in',
      walletId: w.id,
      counter: c.phone,
      counterName: c.name,
      amount,
      currency: w.currency,
      status: 'completed',
      at: new Date().toISOString(),
    };
    this.transactions.update((ts) => [tx, ...ts]);
    this.notifications.update((ns) => [
      {
        id: 'n_' + Math.random().toString(36).slice(2, 6),
        kind: 'received',
        title: `You received ${fmtAmount(amount, w.currency)}`,
        body: `From ${c.name} · ${c.phone}`,
        at: new Date().toISOString(),
        read: false,
      },
      ...ns,
    ]);
    this.pushToast({
      kind: 'received',
      title: `Received ${fmtAmount(amount, w.currency)}`,
      body: `From ${c.name}`,
    });
  }
}
