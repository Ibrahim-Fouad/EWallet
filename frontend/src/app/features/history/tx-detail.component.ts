import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';

import {
  AppStateService,
  Transaction,
  fmtDateTime,
} from '../../core/services/app-state.service';
import { IconComponent, IconName } from '../../shared/icons/icon.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';

const TYPE_ICON: Record<Transaction['type'], IconName> = {
  in: 'arrow-down',
  out: 'arrow-up',
  deposit: 'plus',
};

@Component({
  selector: 'app-tx-detail',
  imports: [IconComponent, StatusBadgeComponent],
  template: `
    <div class="col gap-4">
      <div style="text-align: center; padding: 16px 0">
        <div
          [class]="'tx-icon ' + tx().type"
          style="width: 56px; height: 56px; border-radius: 14px; margin: 0 auto"
        >
          <app-icon [name]="iconName()" [size]="24" />
        </div>
        <div
          class="t-num"
          style="font-size: 32px; font-weight: 700; margin-top: 16px; letter-spacing: -0.02em"
          [style.color]="positive() ? 'var(--success)' : 'var(--text-primary)'"
        >
          {{ positive() ? '+' : '−' }}{{ formattedAmount() }}
          <span style="font-size: 16px; font-weight: 500; opacity: 0.7; margin-left: 6px">
            {{ tx().currency }}
          </span>
        </div>
        <div class="t-small secondary" style="margin-top: 4px">{{ summary() }}</div>
        <div style="margin-top: 12px">
          <app-status-badge [status]="$any(tx().status)" />
        </div>
      </div>

      <div style="background: var(--surface-2); border-radius: 12px; padding: 4px 16px">
        <div class="detail-row">
          <span class="lbl">Transaction ID</span>
          <span class="val t-mono t-small row gap-1">
            {{ tx().id }}
            <span style="color: var(--text-muted); display: inline-flex">
              <app-icon name="copy" [size]="12" />
            </span>
          </span>
        </div>
        <div class="detail-row">
          <span class="lbl">Type</span>
          <span class="val" style="text-transform: capitalize">{{ typeLabel() }}</span>
        </div>
        <div class="detail-row">
          <span class="lbl">{{ tx().type === 'out' ? 'From wallet' : 'To wallet' }}</span>
          <span class="val t-num">{{ walletPhone() }}</span>
        </div>
        <div class="detail-row">
          <span class="lbl">{{ counterLabel() }}</span>
          <span class="val t-num">{{ tx().counter }}</span>
        </div>
        @if (tx().counterName && tx().type !== 'deposit') {
          <div class="detail-row">
            <span class="lbl">Counterpart</span>
            <span class="val">{{ tx().counterName }}</span>
          </div>
        }
        <div class="detail-row">
          <span class="lbl">Created at</span>
          <span class="val">{{ formatDate(tx().at) }}</span>
        </div>
        @if (tx().status === 'completed') {
          <div class="detail-row">
            <span class="lbl">Completed at</span>
            <span class="val">{{ formatDate(tx().at) }}</span>
          </div>
        }
        @if (tx().note) {
          <div class="detail-row">
            <span class="lbl">Note</span>
            <span class="val" style="max-width: 240px">{{ tx().note }}</span>
          </div>
        }
        @if (tx().failReason) {
          <div class="detail-row" style="align-items: flex-start">
            <span class="lbl">Failure reason</span>
            <span class="val" style="color: var(--danger); max-width: 240px; text-align: right">
              {{ tx().failReason }}
            </span>
          </div>
        }
      </div>

      <div class="row gap-2">
        <button type="button" class="btn btn-secondary" style="flex: 1">
          <app-icon name="copy" [size]="14" /> Copy ID
        </button>
        <button type="button" class="btn btn-secondary" style="flex: 1">
          Download receipt
        </button>
      </div>
      @if (tx().status === 'failed') {
        <button type="button" class="btn btn-primary">
          <app-icon name="send" [size]="14" /> Retry transfer
        </button>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TxDetailComponent {
  private readonly state = inject(AppStateService);

  readonly tx = input.required<Transaction>();

  protected readonly positive = computed(() => {
    const t = this.tx();
    return t.type === 'in' || t.type === 'deposit';
  });

  protected readonly iconName = computed<IconName>(() => TYPE_ICON[this.tx().type]);

  protected readonly formattedAmount = computed(() =>
    this.tx().amount.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })
  );

  protected readonly summary = computed(() => {
    const t = this.tx();
    if (t.type === 'in') return `Received from ${t.counterName}`;
    if (t.type === 'out') return `Sent to ${t.counterName}`;
    return 'Deposit from bank';
  });

  protected readonly typeLabel = computed(() => {
    const t = this.tx();
    if (t.type === 'in') return 'Incoming transfer';
    if (t.type === 'out') return 'Outgoing transfer';
    return 'Deposit';
  });

  protected readonly counterLabel = computed(() => {
    const t = this.tx();
    if (t.type === 'out') return 'To phone';
    if (t.type === 'in') return 'From phone';
    return 'Source';
  });

  protected readonly walletPhone = computed(() => {
    const t = this.tx();
    return this.state.wallets().find((w) => w.id === t.walletId)?.phone ?? '—';
  });

  protected formatDate(iso: string): string {
    return fmtDateTime(iso);
  }
}
