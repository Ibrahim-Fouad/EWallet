import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import { AppStateService, fmtAmount } from '../../core/services/app-state.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { BreadcrumbsComponent } from '../../shared/layout/breadcrumbs.component';
import { CurrencyBadgeComponent } from '../../shared/ui/currency-badge.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { TxTableComponent } from '../../shared/ui/tx-table.component';
import { WalletCardComponent } from '../../shared/ui/wallet-card.component';

type Tab = 'all' | 'in' | 'out';

@Component({
  selector: 'app-wallet-detail',
  imports: [
    IconComponent,
    BreadcrumbsComponent,
    CurrencyBadgeComponent,
    StatusBadgeComponent,
    TxTableComponent,
    WalletCardComponent,
  ],
  template: `
    @if (wallet(); as w) {
      <div class="page">
        <app-breadcrumbs
          [items]="[
            { label: 'Wallets', link: '/wallets' },
            { label: w.currency + ' · ' + w.phone },
          ]"
        />
        <div class="row gap-4" style="align-items: stretch">
          <div style="width: 360px">
            <app-wallet-card [wallet]="w" />
          </div>
          <div class="card grow" style="padding: 24px">
            <div class="t-h3" style="margin-bottom: 16px">Wallet details</div>
            <div class="detail-row">
              <span class="lbl">Phone number</span>
              <span class="val t-num">{{ w.phone }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Currency</span>
              <span class="val"><app-currency-badge [currency]="w.currency" /></span>
            </div>
            <div class="detail-row">
              <span class="lbl">Status</span>
              <span class="val"><app-status-badge [status]="$any(w.status)" /></span>
            </div>
            <div class="detail-row">
              <span class="lbl">Available balance</span>
              <span class="val t-num">{{ formatBalance(w.balance, w.currency) }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Opened</span>
              <span class="val">{{ formatJoined(w.created) }}</span>
            </div>
            <div class="row gap-2" style="margin-top: 20px">
              <button type="button" class="btn btn-secondary" (click)="goDeposit(w.id)">
                <app-icon name="arrow-down" [size]="14" /> Deposit
              </button>
              <button type="button" class="btn btn-primary" (click)="goTransfer(w.id)">
                <app-icon name="send" [size]="14" /> Transfer
              </button>
            </div>
          </div>
        </div>

        <div class="card">
          <div style="padding: 16px 24px; border-bottom: 1px solid var(--border)">
            <div class="t-h3" style="margin-bottom: 12px">Activity for this wallet</div>
            <div class="tabs">
              @for (t of tabs(); track t.id) {
                <button
                  type="button"
                  class="tab"
                  [attr.data-active]="tab() === t.id"
                  (click)="setTab(t.id)"
                >
                  {{ t.label }}
                </button>
              }
            </div>
          </div>
          <app-tx-table [rows]="filteredTransactions()" [hideWallet]="true" />
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WalletDetailComponent {
  private readonly state = inject(AppStateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  private readonly walletId = toSignal(
    this.route.paramMap.pipe(map((p) => p.get('id'))),
    { initialValue: null }
  );

  protected readonly wallet = computed(() => {
    const id = this.walletId();
    const list = this.state.wallets();
    return list.find((w) => w.id === id) ?? list[0];
  });

  protected readonly walletTx = computed(() =>
    this.state.transactions().filter((t) => t.walletId === this.wallet()?.id)
  );

  protected readonly tab = signal<Tab>('all');

  protected readonly tabs = computed(() => {
    const wTx = this.walletTx();
    return [
      { id: 'all' as Tab, label: `All (${wTx.length})` },
      { id: 'in' as Tab, label: `Incoming (${wTx.filter((t) => t.type === 'in' || t.type === 'deposit').length})` },
      { id: 'out' as Tab, label: `Outgoing (${wTx.filter((t) => t.type === 'out').length})` },
    ];
  });

  protected readonly filteredTransactions = computed(() => {
    const wTx = this.walletTx();
    const t = this.tab();
    if (t === 'all') return wTx;
    if (t === 'in') return wTx.filter((x) => x.type === 'in' || x.type === 'deposit');
    return wTx.filter((x) => x.type === 'out');
  });

  protected setTab(t: Tab): void {
    this.tab.set(t);
  }

  protected formatBalance(balance: number, currency: string): string {
    return fmtAmount(balance, currency);
  }

  protected formatJoined(iso: string): string {
    return new Date(iso).toLocaleDateString('en-US', {
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    });
  }

  protected goDeposit(id: string): void {
    void this.router.navigate(['/deposit'], { queryParams: { walletId: id } });
  }

  protected goTransfer(id: string): void {
    void this.router.navigate(['/transfer'], { queryParams: { fromWalletId: id } });
  }
}
