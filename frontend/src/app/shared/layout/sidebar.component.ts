import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { AppStateService } from '../../core/services/app-state.service';
import { IconComponent, IconName } from '../icons/icon.component';

interface NavItem {
  id: string;
  label: string;
  icon: IconName;
  link: string;
}

const NAV_ITEMS: NavItem[] = [
  { id: 'dashboard', label: 'Dashboard', icon: 'home', link: '/dashboard' },
  { id: 'wallets', label: 'My Wallets', icon: 'wallet', link: '/wallets' },
  { id: 'transfer', label: 'Transfer', icon: 'send', link: '/transfer' },
  { id: 'deposit', label: 'Deposit', icon: 'arrow-down', link: '/deposit' },
  { id: 'history', label: 'History', icon: 'history', link: '/history' },
  { id: 'notifications', label: 'Notifications', icon: 'bell', link: '/notifications' },
];

const NAV_BOTTOM: NavItem[] = [
  { id: 'profile', label: 'Profile', icon: 'user', link: '/profile' },
  { id: 'settings', label: 'Settings', icon: 'settings', link: '/settings' },
];

@Component({
  selector: 'app-sidebar',
  imports: [IconComponent, RouterLink, RouterLinkActive],
  template: `
    <aside class="sidebar">
      <div class="sidebar-brand">
        <div class="sidebar-logo" style="color: #fff">
          <app-icon name="wallet" [size]="18" />
        </div>
        <div class="col">
          <div style="color: #fff; font-weight: 600; font-size: 15px">EWallet</div>
          <div style="color: var(--sidebar-text); font-size: 11px">Money, simply.</div>
        </div>
      </div>

      <div class="sidebar-section-label">Main</div>
      <nav class="col gap-1">
        @for (item of mainItems; track item.id) {
          <a
            class="sidebar-item"
            [routerLink]="item.link"
            routerLinkActive
            #rla="routerLinkActive"
            [attr.data-active]="rla.isActive"
          >
            <app-icon [name]="item.icon" [size]="18" />
            <span>{{ item.label }}</span>
            @if (item.id === 'notifications' && unread() > 0) {
              <span class="sidebar-badge">{{ unread() }}</span>
            }
          </a>
        }
      </nav>

      <div class="sidebar-section-label" style="margin-top: 24px">Account</div>
      <nav class="col gap-1">
        @for (item of bottomItems; track item.id) {
          <a
            class="sidebar-item"
            [routerLink]="item.link"
            routerLinkActive
            #rla="routerLinkActive"
            [attr.data-active]="rla.isActive"
          >
            <app-icon [name]="item.icon" [size]="18" />
            <span>{{ item.label }}</span>
          </a>
        }
      </nav>

      <div style="flex: 1"></div>

      <div class="sidebar-promo">
        <div class="row gap-2" style="margin-bottom: 6px; color: #fbbf24">
          <app-icon name="sparkle" [size]="14" />
          <div style="color: #fff; font-size: 12px; font-weight: 600">Tip</div>
        </div>
        <div style="color: var(--sidebar-text); font-size: 12px; line-height: 1.45">
          Click the bell icon in the top bar to simulate a real-time inbound transfer.
        </div>
      </div>
    </aside>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SidebarComponent {
  private readonly state = inject(AppStateService);

  protected readonly mainItems = NAV_ITEMS;
  protected readonly bottomItems = NAV_BOTTOM;
  protected readonly unread = computed(() => this.state.unreadCount());
}
