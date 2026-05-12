import { HttpClient } from '@angular/common/http';
import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  NotificationDto,
  PaymentRequestCreatedPayload,
  PaymentRequestStatus,
  PaymentRequestUpdatedPayload,
  TransactionCompletedPayload,
  TransactionDto,
  TransactionFailedPayload,
  TransferReceivedPayload,
  WalletDto,
} from '../models/transaction.model';
import { Wallet, WalletColor } from '../../shared/ui/wallet-card.component';
import { AuthService } from './auth.service';
import { WalletService } from './wallet.service';
import { TransactionService } from './transaction.service';
import { SignalRService } from './signalr.service';
import { NotificationService } from './notification.service';
import { environment } from '../../../environments/environment';

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

export type NotificationKind = 'received' | 'completed' | 'deposit' | 'failed' | 'payment-request';

export interface PaymentRequestInfo {
  id: string;
  merchantName: string;
  amount: number;
  currency: string;
  expiresAt: string;
  status: PaymentRequestStatus;
  actionTakenAt?: string;
}

export interface AppNotification {
  id: string;
  kind: NotificationKind;
  title: string;
  body: string;
  at: string;
  read: boolean;
  paymentRequest?: PaymentRequestInfo;
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

const STATUS_RANK: Record<PaymentRequestStatus, number> = {
  Pending: 0,
  Approved: 1,
  Rejected: 2,
  Expired: 2,
  Completed: 3,
  Failed: 3,
};

function mergePaymentRequestUpdate(
  current: AppNotification,
  incoming: { status: PaymentRequestStatus; actionTakenAt?: string },
): AppNotification {
  const currentStatus = current.paymentRequest?.status ?? 'Pending';
  if (STATUS_RANK[incoming.status] < STATUS_RANK[currentStatus]) {
    return current; // ignore stale push
  }
  return {
    ...current,
    paymentRequest: {
      ...current.paymentRequest!,
      status: incoming.status,
      actionTakenAt: incoming.actionTakenAt ?? current.paymentRequest?.actionTakenAt,
    },
  };
}

type InternalWallet = Wallet & { id: string; created: string };

@Injectable({ providedIn: 'root' })
export class AppStateService {
  private readonly auth = inject(AuthService);
  private readonly walletService = inject(WalletService);
  private readonly txService = inject(TransactionService);
  private readonly signalr = inject(SignalRService);
  private readonly notificationService = inject(NotificationService);
  private readonly http = inject(HttpClient);

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

  readonly notificationsHasMore = signal(false);
  readonly notificationsLoading = signal(false);
  private readonly notificationsPage = signal(0);

  /** Notification IDs with in-flight approve/reject requests */
  readonly inFlightNotifications = signal<Set<string>>(new Set());

  readonly unreadCount = computed(() => this.notifications().filter((n) => !n.read).length);

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
            this.mapTransactions(paged.items, w),
          ),
        ),
      );
      const allTx = allTxArrays
        .flat()
        .sort((a, b) => new Date(b.at).getTime() - new Date(a.at).getTime());
      this.transactions.set(allTx);

      await this.loadNotificationsPage(1, true);

      this.loadState.set('loaded');

      await this.connectSignalR();
    } catch {
      this.loadState.set('error');
    }
  }

  async refresh(): Promise<void> {
    this.loadState.set('idle');
    await this.initialize();
  }

  async refreshNotifications(): Promise<void> {
    await this.loadNotificationsPage(1, true);
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
    firstValueFrom(this.notificationService.markAllAsRead()).catch(() => {
      // non-fatal: server state corrected on next login
    });
  }

  markRead(id: string): void {
    const n = this.notifications().find((x) => x.id === id);
    if (!n || n.read) return;
    this.notifications.update((ns) => ns.map((n) => (n.id === id ? { ...n, read: true } : n)));
    firstValueFrom(this.notificationService.markAsRead(id)).catch(() => {
      this.notifications.update((ns) => ns.map((n) => (n.id === id ? { ...n, read: false } : n)));
    });
  }

  async loadMoreNotifications(): Promise<void> {
    if (!this.notificationsHasMore() || this.notificationsLoading()) return;
    await this.loadNotificationsPage(this.notificationsPage() + 1, false);
  }

  async approvePaymentRequest(notificationId: string, paymentRequestId: string): Promise<void> {
    const snapshot = this.notifications();
    this.setInFlight(notificationId, true);
    this.applyLocalPaymentRequestUpdate(notificationId, {
      status: 'Approved',
      read: true,
    });
    try {
      await firstValueFrom(
        this.http.post(
          `${environment.backendUrl}/api/v1/payment-requests/${paymentRequestId}/approve`,
          {},
        ),
      );
    } catch {
      this.notifications.set(snapshot);
      await this.refreshOnePaymentRequest(notificationId, paymentRequestId);
      this.pushToast({ kind: 'error', title: 'This request can no longer be approved.' });
    } finally {
      this.setInFlight(notificationId, false);
    }
  }

  async rejectPaymentRequest(notificationId: string, paymentRequestId: string): Promise<void> {
    const snapshot = this.notifications();
    this.setInFlight(notificationId, true);
    this.applyLocalPaymentRequestUpdate(notificationId, {
      status: 'Rejected',
      read: true,
    });
    try {
      await firstValueFrom(
        this.http.post(
          `${environment.backendUrl}/api/v1/payment-requests/${paymentRequestId}/reject`,
          {},
        ),
      );
    } catch {
      this.notifications.set(snapshot);
      await this.refreshOnePaymentRequest(notificationId, paymentRequestId);
      this.pushToast({ kind: 'error', title: 'This request can no longer be rejected.' });
    } finally {
      this.setInFlight(notificationId, false);
    }
  }

  async createWallet({
    phone,
    currency,
  }: {
    phone: string;
    currency: 'EGP' | 'USD';
  }): Promise<InternalWallet> {
    const dto = await firstValueFrom(
      this.walletService.createWallet({ phoneNumber: phone, currency }),
    );
    const idx = this.wallets().length;
    const w: InternalWallet = {
      id: dto.walletId,
      phone: dto.phoneNumber,
      currency: dto.currency,
      balance: dto.balance,
      status: 'active',
      primary: idx === 0,
      color: WALLET_COLORS[idx % WALLET_COLORS.length],
      created: new Date().toISOString(),
    };
    this.wallets.update((ws) => [...ws, w]);
    return w;
  }

  deposit({ walletId, amount }: { walletId: string; amount: number }) {
    const w = this.wallets().find((x) => x.id === walletId);
    if (!w) return { ok: false as const, reason: 'Wallet not found' };
    this.wallets.update((ws) =>
      ws.map((x) => (x.id === walletId ? { ...x, balance: x.balance + amount } : x)),
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
      ws.map((x) => (x.id === fromWalletId ? { ...x, balance: x.balance - amount } : x)),
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

  /** Called by TransferComponent immediately after the POST returns Pending.
   *  Optimistically adds a pending outbound transaction to the list. */
  addPendingOutboundTransaction(data: {
    transactionId: string;
    sourcePhone: string;
    destinationPhone: string;
    amount: number;
    currency: string;
  }): void {
    const wallet = this.wallets().find((w) => w.phone === data.sourcePhone);
    const tx: Transaction = {
      id: data.transactionId,
      type: 'out',
      walletId: wallet?.id ?? '',
      counter: data.destinationPhone,
      counterName:
        COUNTERPARTS.find((c) => c.phone === data.destinationPhone)?.name ?? data.destinationPhone,
      amount: data.amount,
      currency: data.currency,
      status: 'pending',
      at: new Date().toISOString(),
    };
    this.transactions.update((ts) => [tx, ...ts]);
  }

  simulateInbound(): void {
    const c = COUNTERPARTS[Math.floor(Math.random() * COUNTERPARTS.length)];
    const ws = this.wallets();
    if (ws.length === 0) return;
    const w = ws[0];
    const amount = Math.floor(50 + Math.random() * 500);
    this.wallets.update((list) =>
      list.map((x) => (x.id === w.id ? { ...x, balance: x.balance + amount } : x)),
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

  private async loadNotificationsPage(page: number, reset: boolean): Promise<void> {
    this.notificationsLoading.set(true);
    try {
      const paged = await firstValueFrom(this.notificationService.getHistory(page, 50));
      const mapped = paged.items.map((dto) => this.mapNotificationDto(dto));
      if (reset) {
        this.notifications.set(mapped);
      } else {
        this.notifications.update((ns) => [...ns, ...mapped]);
      }
      this.notificationsPage.set(page);
      this.notificationsHasMore.set(paged.totalCount > page * paged.pageSize);
    } finally {
      this.notificationsLoading.set(false);
    }
  }

  private async connectSignalR(): Promise<void> {
    try {
      await this.signalr.connect();

      this.signalr.transferReceived$.subscribe((p) => this.handleTransferReceived(p));
      this.signalr.transactionCompleted$.subscribe((p) => this.handleTransactionCompleted(p));
      this.signalr.transactionFailed$.subscribe((p) => this.handleTransactionFailed(p));
      this.signalr.paymentRequestCreated$.subscribe((p) => this.handlePaymentRequestCreated(p));
      this.signalr.paymentRequestUpdated$.subscribe((p) => this.handlePaymentRequestUpdated(p));

      // Re-fetch on reconnect to catch missed updates during offline window
      this.signalr.reconnected$.subscribe(() => void this.refreshNotifications());
    } catch {
      // SignalR connection failure is non-fatal — app works without real-time
    }
  }

  private handleTransferReceived(payload: TransferReceivedPayload): void {
    const wallet = this.wallets().find((w) => w.currency === payload.currency) ?? this.wallets()[0];
    if (!wallet) return;

    this.wallets.update((ws) =>
      ws.map((w) => (w.id === wallet.id ? { ...w, balance: w.balance + payload.amount } : w)),
    );

    const tx: Transaction = {
      id: payload.transactionId,
      type: 'in',
      walletId: wallet.id,
      counter: payload.senderPhoneNumber,
      counterName:
        COUNTERPARTS.find((c) => c.phone === payload.senderPhoneNumber)?.name ??
        payload.senderPhoneNumber,
      amount: payload.amount,
      currency: payload.currency,
      status: 'completed',
      at: payload.receivedAt,
    };

    this.transactions.update((ts) => {
      const exists = ts.some((t) => t.id === payload.transactionId);
      return exists ? ts : [tx, ...ts];
    });

    this.notifications.update((ns) => {
      if (ns.some((n) => n.id === payload.notificationId)) return ns;
      return [
        {
          id: payload.notificationId,
          kind: 'received' as NotificationKind,
          title: `You received ${fmtAmount(payload.amount, payload.currency)}`,
          body: `From ${payload.senderPhoneNumber}`,
          at: payload.receivedAt,
          read: false,
        },
        ...ns,
      ];
    });

    this.pushToast({
      kind: 'received',
      title: `Received ${fmtAmount(payload.amount, payload.currency)}`,
      body: `From ${payload.senderPhoneNumber}`,
    });
  }

  private handleTransactionCompleted(payload: TransactionCompletedPayload): void {
    this.transactions.update((ts) =>
      ts.map((t) =>
        t.id === payload.transactionId ? { ...t, status: 'completed' as TransactionStatus } : t,
      ),
    );

    void firstValueFrom(this.walletService.getMyWallets()).then((dtos) => {
      this.wallets.update((ws) =>
        ws.map((w, i) => ({ ...w, balance: dtos[i]?.balance ?? w.balance })),
      );
    });

    this.pushToast({
      kind: 'success',
      title: 'Transfer completed',
      body: `${fmtAmount(payload.amount, payload.currency)} sent successfully`,
    });

    this.notifications.update((ns) => {
      if (ns.some((n) => n.id === payload.notificationId)) return ns;
      return [
        {
          id: payload.notificationId,
          kind: 'completed' as NotificationKind,
          title: 'Transfer completed',
          body: `${fmtAmount(payload.amount, payload.currency)} delivered`,
          at: payload.completedAt,
          read: false,
        },
        ...ns,
      ];
    });
  }

  private handleTransactionFailed(payload: TransactionFailedPayload): void {
    const tx = this.transactions().find((t) => t.id === payload.transactionId);

    this.transactions.update((ts) =>
      ts.map((t) =>
        t.id === payload.transactionId
          ? { ...t, status: 'failed' as TransactionStatus, failReason: payload.failureReason }
          : t,
      ),
    );

    if (tx) {
      this.wallets.update((ws) =>
        ws.map((w) => (w.id === tx.walletId ? { ...w, balance: w.balance + tx.amount } : w)),
      );
    }

    this.pushToast({
      kind: 'error',
      title: 'Transfer failed',
      body: payload.failureReason,
    });

    this.notifications.update((ns) => {
      if (ns.some((n) => n.id === payload.notificationId)) return ns;
      return [
        {
          id: payload.notificationId,
          kind: 'failed' as NotificationKind,
          title: 'Transfer failed',
          body: payload.failureReason,
          at: new Date().toISOString(),
          read: false,
        },
        ...ns,
      ];
    });
  }

  private handlePaymentRequestCreated(payload: PaymentRequestCreatedPayload): void {
    this.notifications.update((ns) => {
      if (ns.some((n) => n.id === payload.notificationId)) return ns;
      const n: AppNotification = {
        id: payload.notificationId,
        kind: 'payment-request',
        title: `${payload.merchantName} request paying ${fmtAmount(payload.amount, payload.currency)}`,
        body: '',
        at: payload.createdAt,
        read: false,
        paymentRequest: {
          id: payload.paymentRequestId,
          merchantName: payload.merchantName,
          amount: payload.amount,
          currency: payload.currency,
          expiresAt: payload.expiresAt,
          status: 'Pending',
        },
      };
      return [n, ...ns];
    });

    this.pushToast({
      kind: 'info',
      title: `Payment request from ${payload.merchantName}`,
      body: `${fmtAmount(payload.amount, payload.currency)} — open notifications to respond`,
    });
  }

  private handlePaymentRequestUpdated(payload: PaymentRequestUpdatedPayload): void {
    this.notifications.update((ns) =>
      ns.map((n) => {
        if (n.id !== payload.notificationId) return n;
        return mergePaymentRequestUpdate(n, {
          status: payload.actionStatus as PaymentRequestStatus,
          actionTakenAt: payload.actionTakenAt,
        });
      }),
    );
  }

  private applyLocalPaymentRequestUpdate(
    notificationId: string,
    patch: { status: PaymentRequestStatus; read?: boolean },
  ): void {
    this.notifications.update((ns) =>
      ns.map((n) => {
        if (n.id !== notificationId) return n;
        return {
          ...mergePaymentRequestUpdate(n, { status: patch.status }),
          read: patch.read ?? n.read,
        };
      }),
    );
  }

  private async refreshOnePaymentRequest(
    notificationId: string,
    paymentRequestId: string,
  ): Promise<void> {
    try {
      const pr = await firstValueFrom(
        this.http.get<{
          status: PaymentRequestStatus;
          resolvedAt: string | null;
        }>(`/api/v1/payment-requests/${paymentRequestId}`),
      );
      this.notifications.update((ns) =>
        ns.map((n) => {
          if (n.id !== notificationId) return n;
          return mergePaymentRequestUpdate(n, {
            status: pr.status,
            actionTakenAt: pr.resolvedAt ?? undefined,
          });
        }),
      );
    } catch {
      // best-effort — stale optimistic state is better than crashing
    }
  }

  private setInFlight(notificationId: string, inFlight: boolean): void {
    this.inFlightNotifications.update((s) => {
      const next = new Set(s);
      if (inFlight) next.add(notificationId);
      else next.delete(notificationId);
      return next;
    });
  }

  private mapNotificationDto(dto: NotificationDto): AppNotification {
    if (dto.type === 'PaymentRequestCreated') {
      return {
        id: dto.id,
        kind: 'payment-request',
        title: `${dto.merchantName} request paying ${fmtAmount(dto.amount!, dto.currency!)}`,
        body: '',
        at: dto.createdAt,
        read: dto.isRead,
        paymentRequest: {
          id: dto.paymentRequestId!,
          merchantName: dto.merchantName!,
          amount: dto.amount!,
          currency: dto.currency!,
          expiresAt: dto.expiresAt!,
          status: dto.actionStatus ?? 'Pending',
          actionTakenAt: dto.actionTakenAt ?? undefined,
        },
      };
    }

    switch (dto.type) {
      case 'TransferReceived':
        return {
          id: dto.id,
          kind: 'received',
          title: `You received ${fmtAmount(dto.amount!, dto.currency!)}`,
          body: `From ${dto.senderPhoneNumber}`,
          at: dto.receivedAt ?? dto.createdAt,
          read: dto.isRead,
        };
      case 'TransactionCompleted':
        return {
          id: dto.id,
          kind: 'completed',
          title: 'Transfer completed',
          body: `${fmtAmount(dto.amount!, dto.currency!)} delivered`,
          at: dto.completedAt ?? dto.createdAt,
          read: dto.isRead,
        };
      case 'TransactionFailed':
        return {
          id: dto.id,
          kind: 'failed',
          title: 'Transfer failed',
          body: dto.failureReason ?? 'Unknown reason',
          at: dto.createdAt,
          read: dto.isRead,
        };
    }
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
      const counterPhone = isDeposit
        ? 'Bank transfer'
        : isInbound
          ? dto.sourcePhoneNumber || dto.destinationPhoneNumber
          : dto.destinationPhoneNumber;
      return {
        id: dto.id,
        type,
        walletId: wallet.id,
        counter: counterPhone,
        counterName: isDeposit ? 'Bank deposit' : counterPhone,
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
