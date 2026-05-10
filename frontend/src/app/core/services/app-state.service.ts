import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { TransactionDto, WalletDto } from '../models/transaction.model';
import { Wallet, WalletColor } from '../../shared/ui/wallet-card.component';
import { AuthService } from './auth.service';
import { WalletService } from './wallet.service';
import { TransactionService } from './transaction.service';

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

export const COUNTERPARTS: readonly Counterpart[] = [
  { name: 'Omar Khaled', phone: '01055443322' },
  { name: 'Layla Hassan', phone: '01187766554' },
  { name: 'Mariam Sayed', phone: '01233445566' },
  { name: 'Ahmed Tarek', phone: '01099887766' },
  { name: 'Nour Adel', phone: '01566778899' },
  { name: 'Hany Fouad', phone: '01122334455' },
];

const EMPTY_USER: User = { id: '', fullName: '', email: '', phone: '', joined: '', avatar: '' };
const WALLET_COLORS: WalletColor[] = ['blue', 'indigo', 'teal', 'slate'];

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
  const now = new Date();
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

type InternalWallet = Wallet & { id: string; created: string };

@Injectable({ providedIn: 'root' })
export class AppStateService {
  private readonly auth = inject(AuthService);
  private readonly walletService = inject(WalletService);
  private readonly txService = inject(TransactionService);

  readonly loadState = signal<'idle' | 'loading' | 'loaded' | 'error'>('idle');

  constructor() {
    effect(() => {
      if (this.auth.authenticated()) {
        void this.initialize();
      }
    });
  }

  readonly user = signal<User>(EMPTY_USER);
  readonly wallets = signal<InternalWallet[]>([]);
  readonly transactions = signal<Transaction[]>([]);
  readonly notifications = signal<AppNotification[]>([]);
  readonly toasts = signal<Toast[]>([]);

  readonly unreadCount = computed(
    () => this.notifications().filter((n) => !n.read).length
  );

  async initialize(): Promise<void> {
    if (this.loadState() === 'loading' || this.loadState() === 'loaded') return;
    this.loadState.set('loading');
    try {
      const claims = this.auth.getClaims();
      if (claims) {
        this.user.set({
          id: claims.sub,
          fullName: claims.name ?? 'User',
          email: claims.email ?? '',
          phone: claims.phone_number ?? '',
          joined: '',
          avatar: this.getInitials(claims.name ?? 'U'),
        });
      }

      const walletDtos = await firstValueFrom(this.walletService.getMyWallets());
      const internalWallets = walletDtos.map((w, i) => this.mapWalletDto(w, i));
      this.wallets.set(internalWallets);

      const allTxArrays = await Promise.all(
        internalWallets.map((w) =>
          firstValueFrom(this.txService.getHistory(w.phone, 1, 50)).then((paged) =>
            this.mapTransactions(paged.items, w)
          )
        )
      );
      const allTx = allTxArrays
        .flat()
        .sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime());
      this.transactions.set(allTx);

      this.loadState.set('loaded');
    } catch {
      this.loadState.set('error');
    }
  }

  async refresh(): Promise<void> {
    this.loadState.set('idle');
    await this.initialize();
  }

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
    const w: InternalWallet = {
      id,
      phone,
      currency,
      balance: 0,
      status: 'active',
      primary: false,
      color: WALLET_COLORS[this.wallets().length % WALLET_COLORS.length],
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
  }: {
    fromWalletId: string;
    toPhone: string;
    amount: number;
  }) {
    const w = this.wallets().find((x) => x.id === fromWalletId);
    if (!w) return { ok: false as const, reason: 'Source wallet not found' };
    if (amount > w.balance) return { ok: false as const, reason: 'Insufficient balance' };
    const counterName = COUNTERPARTS.find((c) => c.phone === toPhone)?.name ?? 'Recipient';
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
    const ws = this.wallets();
    if (ws.length === 0) return;
    const w = ws[0];
    const amount = Math.floor(50 + Math.random() * 500);
    this.wallets.update((list) =>
      list.map((x) => (x.id === w.id ? { ...x, balance: x.balance + amount } : x))
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

  private mapWalletDto(dto: WalletDto, index: number): InternalWallet {
    return {
      id: dto.id,
      phone: dto.phoneNumber,
      currency: dto.currency,
      balance: dto.balance,
      status: dto.isActive ? 'active' : 'inactive',
      primary: index === 0,
      color: WALLET_COLORS[index % WALLET_COLORS.length],
      created: dto.createdAt,
    };
  }

  private mapTransactions(dtos: TransactionDto[], wallet: InternalWallet): Transaction[] {
    return dtos.map((dto) => {
      const isDeposit = dto.description.toLowerCase().includes('deposit');
      const isInbound = dto.destinationPhoneNumber === wallet.phone;
      const type: TransactionType = isDeposit ? 'deposit' : isInbound ? 'in' : 'out';
      return {
        id: dto.id,
        type,
        walletId: wallet.id,
        counter: isDeposit ? 'Bank transfer' : dto.destinationPhoneNumber,
        counterName: isDeposit ? 'Bank deposit' : dto.destinationPhoneNumber,
        amount: dto.amount,
        currency: dto.currency,
        status: this.mapStatus(dto.status),
        at: dto.createdAt,
        note: dto.notes ?? undefined,
      };
    });
  }

  private mapStatus(s: string): TransactionStatus {
    if (s === 'Completed') return 'completed';
    if (s === 'Failed') return 'failed';
    return 'pending';
  }

  private getInitials(name: string): string {
    return name
      .split(' ')
      .slice(0, 2)
      .map((p) => p[0]?.toUpperCase() ?? '')
      .join('');
  }
}
