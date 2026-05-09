import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import {
  AppStateService,
  AppNotification,
  NotificationKind,
  relTime,
} from '../../core/services/app-state.service';
import { IconComponent, IconName } from '../../shared/icons/icon.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';

const KIND_ICON: Record<NotificationKind, IconName> = {
  received: 'arrow-down',
  completed: 'check',
  deposit: 'plus',
  failed: 'x',
};

@Component({
  selector: 'app-notifications',
  imports: [IconComponent, EmptyStateComponent],
  template: `
    @if (notifications().length === 0) {
      <div class="page">
        <h2 class="t-h1">Notifications</h2>
        <div class="card">
          <app-empty-state
            icon="bell"
            title="You're all caught up"
            body="When you receive transfers or your wallets change, you'll see alerts here in real time."
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
              <span class="live-dot"></span> Real-time alerts via SignalR
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
            <div
              class="notif-row"
              [attr.data-unread]="!n.read"
              (click)="markRead(n.id)"
            >
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

  protected iconFor(kind: AppNotification['kind']): IconName {
    return KIND_ICON[kind];
  }

  protected markAllRead(): void {
    this.state.markAllRead();
  }

  protected markRead(id: string): void {
    this.state.markRead(id);
  }

  protected formatTime(iso: string): string {
    return relTime(iso);
  }
}
