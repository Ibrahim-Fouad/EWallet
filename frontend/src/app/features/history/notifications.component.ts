import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';

import {
  AppNotification,
  AppStateService,
  NotificationKind,
  relTime,
} from '../../core/services/app-state.service';
import { PaymentRequestStatus } from '../../core/models/transaction.model';
import { IconComponent, IconName } from '../../shared/icons/icon.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';

const KIND_ICON: Record<NotificationKind, IconName> = {
  received: 'arrow-down',
  completed: 'check',
  deposit: 'plus',
  failed: 'x',
  'payment-request': 'wallet',
};

const PR_STATUS_ICON: Record<PaymentRequestStatus, IconName> = {
  Pending: 'wallet',
  Approved: 'badge-check',
  Completed: 'badge-check',
  Rejected: 'x-circle',
  Expired: 'clock-x',
  Failed: 'x-circle',
};

const PR_STATUS_LABEL: Record<PaymentRequestStatus, string> = {
  Pending: 'Pending',
  Approved: 'Approved',
  Completed: 'Paid',
  Rejected: 'Rejected',
  Expired: 'Expired',
  Failed: 'Failed',
};

@Component({
  selector: 'app-notifications',
  imports: [IconComponent, EmptyStateComponent, CurrencyPipe, DatePipe],
  template: `
    @if (notifications().length === 0 && !loadingMore()) {
      <div class="page">
        <h2 class="t-h1">Notifications</h2>
        <div class="card">
          <app-empty-state
            icon="bell"
            title="You're all caught up"
            body="When you receive transfers or your wallets change, you'll see alerts here."
          />
        </div>
      </div>
    } @else {
      <div class="page">
        <div class="page-header">
          <div>
            <div class="t-tiny secondary" style="text-transform: uppercase; letter-spacing: 0.08em">
              Inbox
            </div>
            <h2 class="t-h1" style="margin-top: 4px">
              Notifications
              @if (unread() > 0) {
                <span
                  class="badge badge-primary"
                  style="margin-left: 8px; vertical-align: middle; font-size: 12px"
                >
                  {{ unread() }} new
                </span>
              }
            </h2>
            <p class="row gap-2 secondary t-small" style="margin-top: 6px">
              <span class="live-dot"></span> Connected live — history persists across sessions
            </p>
          </div>
          @if (unread() > 0) {
            <button type="button" class="btn btn-secondary" (click)="markAllRead()">
              <app-icon name="check" [size]="14" /> Mark all as read
            </button>
          }
        </div>

        <div class="card">
          @for (n of notifications(); track n.id) {
            @switch (n.kind) {
              @case ('payment-request') {
                <div class="notif-row" [attr.data-unread]="!n.read" (click)="markRead(n.id)">
                  <div
                    [class]="
                      'dd-notif-icon payment-request ' + prIconClass(n.paymentRequest!.status)
                    "
                    style="width: 40px; height: 40px; border-radius: 10px"
                  >
                    <app-icon [name]="prIcon(n.paymentRequest!.status)" [size]="18" />
                  </div>
                  <div class="grow">
                    <div style="font-weight: 500; font-size: 14px">
                      <strong>{{ n.paymentRequest!.merchantName }}</strong> requested pay
                      {{
                        n.paymentRequest!.amount
                          | currency: n.paymentRequest!.currency : 'symbol' : '1.2-2'
                      }}
                    </div>
                    <div class="t-small secondary" style="margin-top: 2px">
                      @if (n.paymentRequest!.status === 'Pending') {
                        @if (isExpired(n.paymentRequest!.expiresAt)) {
                          Expired
                        } @else {
                          Expires {{ n.paymentRequest!.expiresAt | date: 'shortTime' }}
                        }
                      } @else {
                        {{ prStatusLabel(n.paymentRequest!.status) }}
                        @if (n.paymentRequest!.actionTakenAt) {
                          · {{ formatTime(n.paymentRequest!.actionTakenAt) }}
                        }
                      }
                    </div>
                    @if (
                      n.paymentRequest!.status === 'Pending' &&
                      !isExpired(n.paymentRequest!.expiresAt) &&
                      !isInFlight(n.id)
                    ) {
                      <div
                        class="row gap-2"
                        style="margin-top: 8px"
                        role="group"
                        aria-label="Payment request actions"
                      >
                        <button
                          type="button"
                          class="btn btn-primary"
                          style="font-size: 13px; padding: 4px 12px"
                          (click)="$event.stopPropagation(); approve(n)"
                          [attr.aria-disabled]="false"
                        >
                          Approve
                        </button>
                        <button
                          type="button"
                          class="btn btn-secondary"
                          style="font-size: 13px; padding: 4px 12px"
                          (click)="$event.stopPropagation(); reject(n)"
                          [attr.aria-disabled]="false"
                        >
                          Reject
                        </button>
                      </div>
                    }
                  </div>
                  <div class="col" style="align-items: flex-end; gap: 6px">
                    <span class="t-tiny muted">{{ formatTime(n.at) }}</span>
                    @if (!n.read) {
                      <span
                        style="width: 8px; height: 8px; border-radius: 999px; background: var(--primary)"
                      ></span>
                    }
                  </div>
                </div>
              }
              @default {
                <div class="notif-row" [attr.data-unread]="!n.read" (click)="markRead(n.id)">
                  <div
                    [class]="'dd-notif-icon ' + n.kind"
                    style="width: 40px; height: 40px; border-radius: 10px"
                  >
                    <app-icon [name]="iconFor(n.kind)" [size]="18" />
                  </div>
                  <div class="grow">
                    <div style="font-weight: 500; font-size: 14px">{{ n.title }}</div>
                    <div class="t-small secondary" style="margin-top: 2px">{{ n.body }}</div>
                  </div>
                  <div class="col" style="align-items: flex-end; gap: 6px">
                    <span class="t-tiny muted">{{ formatTime(n.at) }}</span>
                    @if (!n.read) {
                      <span
                        style="width: 8px; height: 8px; border-radius: 999px; background: var(--primary)"
                      ></span>
                    }
                  </div>
                </div>
              }
            }
          }
          @if (hasMore()) {
            <div style="padding: 16px; text-align: center">
              <button
                type="button"
                class="btn btn-secondary"
                [disabled]="loadingMore()"
                (click)="loadMore()"
              >
                @if (loadingMore()) {
                  Loading…
                } @else {
                  Load more
                }
              </button>
            </div>
          }
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsComponent {
  private readonly state = inject(AppStateService);

  protected readonly notifications = this.state.notifications;
  protected readonly unread = this.state.unreadCount;
  protected readonly hasMore = this.state.notificationsHasMore;
  protected readonly loadingMore = this.state.notificationsLoading;

  protected iconFor(kind: AppNotification['kind']): IconName {
    return KIND_ICON[kind];
  }

  protected prIcon(status: PaymentRequestStatus): IconName {
    return PR_STATUS_ICON[status];
  }

  protected prIconClass(status: PaymentRequestStatus): string {
    if (status === 'Completed' || status === 'Approved') return 'success';
    if (status === 'Rejected' || status === 'Failed') return 'danger';
    if (status === 'Expired') return 'muted';
    return '';
  }

  protected prStatusLabel(status: PaymentRequestStatus): string {
    return PR_STATUS_LABEL[status];
  }

  protected prStatusPillClass(status: PaymentRequestStatus): string {
    if (status === 'Completed' || status === 'Approved') return 'success';
    if (status === 'Rejected' || status === 'Failed') return 'danger';
    return 'muted';
  }

  protected isExpired(expiresAt: string): boolean {
    return new Date(expiresAt) <= new Date();
  }

  protected isInFlight(notificationId: string): boolean {
    return this.state.inFlightNotifications().has(notificationId);
  }

  protected async approve(n: AppNotification): Promise<void> {
    await this.state.approvePaymentRequest(n.id, n.paymentRequest!.id);
  }

  protected async reject(n: AppNotification): Promise<void> {
    await this.state.rejectPaymentRequest(n.id, n.paymentRequest!.id);
  }

  protected markAllRead(): void {
    this.state.markAllRead();
  }

  protected markRead(id: string): void {
    this.state.markRead(id);
  }

  protected async loadMore(): Promise<void> {
    await this.state.loadMoreNotifications();
  }

  protected formatTime(iso: string): string {
    return relTime(iso);
  }
}
