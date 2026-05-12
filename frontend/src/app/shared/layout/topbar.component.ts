import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';

import { AppStateService, relTime } from '../../core/services/app-state.service';
import { AuthService } from '../../core/services/auth.service';
import { IconComponent, IconName } from '../icons/icon.component';
import type { NotificationKind } from '../../core/services/app-state.service';

const NOTIF_ICON: Record<NotificationKind, IconName> = {
  received: 'arrow-down',
  completed: 'check',
  deposit: 'plus',
  failed: 'x',
  'payment-request': 'mail',
};

@Component({
  selector: 'app-topbar',
  imports: [IconComponent, RouterLink],
  template: `
    <header class="topbar">
      <div class="col">
        <div class="t-tiny" style="color: var(--text-muted); text-transform: uppercase">
          EWallet
        </div>
        <h1 class="t-h1" style="margin: 0">{{ title() }}</h1>
      </div>
      <div style="flex: 1"></div>

      <div class="searchbar">
        <span style="color: var(--text-muted); display: inline-flex">
          <app-icon name="search" [size]="16" />
        </span>
        <input placeholder="Search transactions, wallets, contacts…" />
        <kbd>⌘K</kbd>
      </div>

      <button
        type="button"
        class="icon-btn"
        (click)="simulateInbound()"
        title="Simulate inbound transfer"
        aria-label="Simulate inbound transfer"
      >
        <app-icon name="arrow-down" [size]="18" />
      </button>

      <div style="position: relative">
        <button
          type="button"
          class="icon-btn"
          (click)="toggleBell($event)"
          aria-label="Notifications"
        >
          <app-icon name="bell" [size]="18" />
          @if (unread() > 0) {
            <span class="icon-badge"></span>
          }
        </button>
        @if (bellOpen()) {
          <div class="dropdown" style="width: 360px">
            <div
              class="row between"
              style="padding: 12px 16px; border-bottom: 1px solid var(--border)"
            >
              <div class="t-h3">Notifications</div>
              <a class="btn-ghost btn btn-sm" routerLink="/notifications" (click)="closeAll()">
                View all
              </a>
            </div>
            <div style="max-height: 360px; overflow: auto">
              @for (n of recentNotifications(); track n.id) {
                <div class="dd-notif">
                  <div [class]="'dd-notif-icon ' + n.kind">
                    <app-icon [name]="iconFor(n.kind)" [size]="14" />
                  </div>
                  <div class="grow">
                    <div style="font-weight: 500; font-size: 13px">{{ n.title }}</div>
                    <div class="t-small secondary">{{ n.body }}</div>
                    <div class="t-tiny muted" style="margin-top: 2px">{{ relTime(n.at) }}</div>
                  </div>
                  @if (!n.read) {
                    <span class="unread-dot"></span>
                  }
                </div>
              }
            </div>
          </div>
        }
      </div>

      <div style="position: relative">
        <button
          type="button"
          class="avatar-btn"
          (click)="toggleMenu($event)"
          aria-label="Account menu"
        >
          <div class="avatar">{{ user().avatar }}</div>
          <div class="col" style="align-items: flex-start; line-height: 1.2">
            <span style="font-size: 13px; font-weight: 500">{{ user().fullName }}</span>
            <span class="t-tiny muted">{{ user().email }}</span>
          </div>
          <span style="color: var(--text-muted); display: inline-flex">
            <app-icon name="chevron-down" [size]="14" />
          </span>
        </button>
        @if (menuOpen()) {
          <div class="dropdown" style="width: 220px">
            <a class="dd-item" routerLink="/profile" (click)="closeAll()">
              <app-icon name="user" [size]="16" /> Profile
            </a>
            <a class="dd-item" routerLink="/settings" (click)="closeAll()">
              <app-icon name="settings" [size]="16" /> Settings
            </a>
            <div style="border-top: 1px solid var(--border); margin: 4px 0"></div>
            <button type="button" class="dd-item danger" (click)="logout()">
              <app-icon name="logout" [size]="16" /> Log out
            </button>
          </div>
        }
      </div>
    </header>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:mousedown)': 'onDocClick($event)',
  },
})
export class TopBarComponent {
  private readonly state = inject(AppStateService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly el = inject(ElementRef<HTMLElement>);

  protected readonly user = this.state.user;
  protected readonly unread = this.state.unreadCount;
  protected readonly recentNotifications = computed(() => this.state.notifications().slice(0, 4));

  protected readonly bellOpen = signal(false);
  protected readonly menuOpen = signal(false);

  protected readonly title = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      startWith(null),
      map(() => {
        let r = this.route;
        while (r.firstChild) r = r.firstChild;
        return (r?.snapshot?.data['title'] as string) ?? '';
      }),
    ),
    { initialValue: '' },
  );

  protected toggleBell(event: Event): void {
    event.stopPropagation();
    this.bellOpen.update((v) => !v);
    this.menuOpen.set(false);
  }

  protected toggleMenu(event: Event): void {
    event.stopPropagation();
    this.menuOpen.update((v) => !v);
    this.bellOpen.set(false);
  }

  protected closeAll(): void {
    this.bellOpen.set(false);
    this.menuOpen.set(false);
  }

  protected simulateInbound(): void {
    this.state.simulateInbound();
  }

  protected logout(): void {
    this.auth.logout();
    this.closeAll();
    void this.router.navigateByUrl('/login');
  }

  protected iconFor(kind: NotificationKind): IconName {
    return NOTIF_ICON[kind];
  }

  protected relTime(iso: string): string {
    return relTime(iso);
  }

  protected onDocClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target as Node)) {
      this.closeAll();
    }
  }
}
