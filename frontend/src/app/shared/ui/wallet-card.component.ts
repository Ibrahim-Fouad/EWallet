import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';


export type WalletColor = 'blue' | 'indigo' | 'teal' | 'slate';

export interface Wallet {
  id?: string;
  phone: string;
  currency: string;
  balance: number;
  status: string;
  primary?: boolean;
  color?: WalletColor;
}

const PALETTE: Record<WalletColor, { bg: string; glow: string }> = {
  blue: {
    bg: 'linear-gradient(135deg, #2563EB 0%, #1D4ED8 60%, #1E40AF 100%)',
    glow: 'rgba(37, 99, 235, 0.35)',
  },
  indigo: {
    bg: 'linear-gradient(135deg, #4F46E5 0%, #4338CA 60%, #3730A3 100%)',
    glow: 'rgba(79, 70, 229, 0.35)',
  },
  teal: {
    bg: 'linear-gradient(135deg, #0D9488 0%, #0F766E 60%, #115E59 100%)',
    glow: 'rgba(13, 148, 136, 0.35)',
  },
  slate: {
    bg: 'linear-gradient(135deg, #334155 0%, #1E293B 60%, #0F172A 100%)',
    glow: 'rgba(51, 65, 85, 0.35)',
  },
};

@Component({
  selector: 'app-wallet-card',
  template: `
    <div
      [class]="cls()"
      [style.background]="palette().bg"
      [style.box-shadow]="'0 8px 24px -8px ' + palette().glow"
      [attr.role]="interactive() ? 'button' : null"
      [attr.tabindex]="interactive() ? 0 : null"
      (click)="handleActivate()"
      (keydown.enter)="handleActivate()"
      (keydown.space)="handleActivate(); $event.preventDefault()"
    >
      <div class="wallet-card-noise"></div>
      <div class="wallet-card-row">
        <div>
          <div class="wallet-card-label">Wallet · {{ wallet().currency }}</div>
          <div class="wallet-card-phone t-num">{{ wallet().phone }}</div>
        </div>
        <div class="wallet-card-currency">{{ wallet().currency }}</div>
      </div>
      <div style="flex: 1"></div>
      <div>
        <div class="wallet-card-label">Available balance</div>
        <div class="wallet-card-balance t-num">
          {{ formattedBalance() }}<span style="font-size: 14px; opacity: 0.7; margin-left: 6px; font-weight: 500">{{ wallet().currency }}</span>
        </div>
      </div>
      <div class="wallet-card-row" style="margin-top: 12px">
        <span class="wallet-card-status">
          <span class="wallet-card-status-dot"></span> {{ wallet().status }}
        </span>
        @if (wallet().primary) {
          <span class="wallet-card-primary">PRIMARY</span>
        }
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WalletCardComponent {
  readonly wallet = input.required<Wallet>();
  readonly compact = input<boolean>(false);
  readonly selected = input<boolean>(false);
  readonly interactive = input<boolean>(false);
  readonly activate = output<void>();

  protected readonly palette = computed(
    () => PALETTE[this.wallet().color ?? 'blue'] ?? PALETTE.blue
  );

  protected readonly cls = computed(() => {
    const parts = ['wallet-card'];
    if (this.compact()) parts.push('wallet-card-compact');
    if (this.selected()) parts.push('wallet-card-selected');
    return parts.join(' ');
  });

  protected readonly formattedBalance = computed(() =>
    this.wallet().balance.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })
  );

  protected handleActivate(): void {
    if (this.interactive()) {
      this.activate.emit();
    }
  }
}
