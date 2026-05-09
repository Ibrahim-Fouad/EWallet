import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';

import { fmtAmount } from '../../core/services/app-state.service';
import type { Wallet } from './wallet-card.component';
import { CurrencyBadgeComponent } from './currency-badge.component';
import { IconComponent } from '../icons/icon.component';

export type PickerWallet = Wallet & { id: string };

@Component({
  selector: 'app-wallet-picker',
  imports: [CurrencyBadgeComponent, IconComponent],
  template: `
    <div class="field">
      <label class="field-label">{{ label() }}</label>
      <div style="position: relative">
        <button
          type="button"
          class="input"
          style="width: 100%; text-align: left; display: flex; align-items: center; gap: 12px"
          (click)="toggle($event)"
        >
          @if (selected(); as sel) {
            <app-currency-badge [currency]="sel.currency" />
            <span class="t-num" style="font-weight: 500">{{ sel.phone }}</span>
            <span class="muted t-small">·</span>
            <span class="t-num secondary">{{ formatBalance(sel) }}</span>
            <span style="margin-left: auto; color: var(--text-muted); display: inline-flex">
              <app-icon name="chevron-down" [size]="16" />
            </span>
          } @else {
            <span class="muted">Select a wallet…</span>
          }
        </button>
        @if (open()) {
          <div class="dropdown" style="width: 100%; left: 0; right: 0; padding: 0">
            @for (w of wallets(); track w.id) {
              <button
                type="button"
                class="dd-item"
                style="padding: 12px; border-radius: 0; gap: 12px"
                (click)="select(w.id)"
              >
                <app-currency-badge [currency]="w.currency" />
                <div class="col grow" style="align-items: flex-start">
                  <span class="t-num" style="font-weight: 500">{{ w.phone }}</span>
                  <span class="t-tiny secondary">{{ formatBalance(w) }} available</span>
                </div>
                @if (value() === w.id) {
                  <span style="color: var(--primary); display: inline-flex">
                    <app-icon name="check" [size]="16" />
                  </span>
                }
              </button>
            }
          </div>
        }
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:mousedown)': 'onDocClick($event)',
  },
})
export class WalletPickerComponent {
  private readonly el = inject(ElementRef<HTMLElement>);

  readonly wallets = input.required<PickerWallet[]>();
  readonly value = input<string | null>(null);
  readonly label = input<string>('Source wallet');
  readonly valueChange = output<string>();

  protected readonly open = signal(false);

  protected readonly selected = computed(() =>
    this.wallets().find((w) => w.id === this.value()) ?? null
  );

  protected toggle(event: Event): void {
    event.stopPropagation();
    this.open.update((v) => !v);
  }

  protected select(id: string): void {
    this.valueChange.emit(id);
    this.open.set(false);
  }

  protected formatBalance(w: PickerWallet): string {
    return fmtAmount(w.balance, w.currency);
  }

  protected onDocClick(event: MouseEvent): void {
    if (!this.el.nativeElement.contains(event.target as Node)) {
      this.open.set(false);
    }
  }
}
