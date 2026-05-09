import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';

import {
  AppStateService,
  Transaction,
  relTime,
} from '../../core/services/app-state.service';
import { IconComponent, IconName } from '../icons/icon.component';
import { AmountComponent } from './amount.component';
import { CurrencyBadgeComponent } from './currency-badge.component';
import { EmptyStateComponent } from './empty-state.component';
import { StatusBadgeComponent } from './status-badge.component';

const TYPE_ICON: Record<Transaction['type'], IconName> = {
  in: 'arrow-down',
  out: 'arrow-up',
  deposit: 'plus',
};

@Component({
  selector: 'app-tx-table',
  imports: [
    IconComponent,
    AmountComponent,
    CurrencyBadgeComponent,
    EmptyStateComponent,
    StatusBadgeComponent,
  ],
  template: `
    @if (rows().length === 0) {
      <app-empty-state
        icon="history"
        title="No transactions yet"
        body="Once you deposit or transfer, you'll see history here."
      />
    } @else {
      <table class="tx-table">
        <thead>
          <tr>
            <th style="width: 50px"></th>
            <th>Counterpart</th>
            @if (!hideWallet()) {
              <th>Wallet</th>
            }
            <th>Amount</th>
            <th>Status</th>
            <th>Date</th>
          </tr>
        </thead>
        <tbody>
          @for (tx of rows(); track tx.id) {
            <tr (click)="rowClick.emit(tx)">
              <td>
                <div [class]="'tx-icon ' + tx.type">
                  <app-icon [name]="iconFor(tx.type)" [size]="14" />
                </div>
              </td>
              <td>
                <div style="font-weight: 500">{{ tx.counterName }}</div>
                <div class="t-small muted t-num">{{ tx.counter }}</div>
              </td>
              @if (!hideWallet()) {
                <td>
                  <div class="row gap-2">
                    <app-currency-badge [currency]="tx.currency" />
                    <span class="t-num t-small">{{ walletPhone(tx.walletId) }}</span>
                  </div>
                </td>
              }
              <td>
                <app-amount [value]="tx.amount" [currency]="tx.currency" [type]="tx.type" />
              </td>
              <td><app-status-badge [status]="tx.status" /></td>
              <td class="secondary t-small">{{ relTime(tx.at) }}</td>
            </tr>
          }
        </tbody>
      </table>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TxTableComponent {
  private readonly state = inject(AppStateService);

  readonly rows = input.required<Transaction[]>();
  readonly hideWallet = input<boolean>(false);
  readonly rowClick = output<Transaction>();

  protected iconFor(type: Transaction['type']): IconName {
    return TYPE_ICON[type];
  }

  protected walletPhone(walletId: string): string {
    return this.state.wallets().find((w) => w.id === walletId)?.phone ?? '—';
  }

  protected relTime(iso: string): string {
    return relTime(iso);
  }
}
