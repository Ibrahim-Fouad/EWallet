import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';

import { AppStateService, Transaction } from '../../core/services/app-state.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { StatCardComponent } from '../../shared/ui/stat-card.component';
import { TxTableComponent } from '../../shared/ui/tx-table.component';
import { WalletCardComponent } from '../../shared/ui/wallet-card.component';
import { BalanceChartComponent } from './balance-chart.component';

@Component({
  selector: 'app-dashboard',
  imports: [
    IconComponent,
    StatCardComponent,
    TxTableComponent,
    WalletCardComponent,
    BalanceChartComponent,
  ],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <div
            class="t-tiny secondary"
            style="text-transform: uppercase; letter-spacing: 0.08em"
          >
            Welcome back
          </div>
          <h2 class="t-h1" style="margin-top: 4px">Hey, {{ firstName() }} 👋</h2>
        </div>
        <div class="row gap-2">
          <button type="button" class="btn btn-secondary" (click)="goTo('/deposit')">
            <app-icon name="arrow-down" [size]="16" /> Deposit
          </button>
          <button type="button" class="btn btn-primary" (click)="goTo('/transfer')">
            <app-icon name="send" [size]="16" /> New transfer
          </button>
        </div>
      </div>

      <div class="row gap-4" style="align-items: stretch">
        @for (w of wallets(); track w.id) {
          <div style="flex: 1; min-width: 0">
            <app-wallet-card
              [wallet]="w"
              [interactive]="true"
              (activate)="openWallet(w.id)"
            />
          </div>
        }
        @if (wallets().length < 3) {
          <button type="button" class="wallet-card-new" style="flex: 1" (click)="goTo('/wallets')">
            <div
              style="width: 40px; height: 40px; border-radius: 999px; border: 2px dashed currentColor; display: grid; place-items: center"
            >
              <app-icon name="plus" [size]="20" />
            </div>
            <div style="font-weight: 600; font-size: 14px">Open new wallet</div>
            <div class="t-small">{{ remainingSlots() }} available</div>
          </button>
        }
      </div>

      <div class="row gap-4">
        <div style="flex: 1">
          <app-stat-card
            icon="arrow-down"
            label="Received this month"
            [value]="receivedDisplay()"
            trend="+18.2% vs last month"
            trendDir="up"
          />
        </div>
        <div style="flex: 1">
          <app-stat-card
            icon="arrow-up"
            label="Sent this month"
            [value]="sentDisplay()"
            trend="−4.1% vs last month"
            trendDir="down"
          />
        </div>
        <div style="flex: 1">
          <app-stat-card
            icon="send"
            label="Total transfers"
            [value]="totalTransfers()"
            trend="3 this week"
            trendDir="up"
          />
        </div>
        <div style="flex: 1">
          <app-stat-card
            icon="wallet"
            label="Active wallets"
            [value]="wallets().length + ' / 3'"
            trend="Up to 3 allowed"
            trendDir="up"
          />
        </div>
      </div>

      <div class="row gap-4" style="align-items: stretch">
        <div style="flex: 2">
          <app-balance-chart [seedTotal]="totalBalance()" />
        </div>
        <div class="card" style="flex: 1; padding: 24px">
          <div class="t-h3" style="margin-bottom: 16px">Quick actions</div>
          <div class="col gap-2">
            <button type="button" class="qa-btn" (click)="goTo('/transfer')">
              <div class="qa-icon"><app-icon name="send" [size]="18" /></div>
              <div>
                <div class="qa-title">Send money</div>
                <div class="qa-sub">Transfer by phone number</div>
              </div>
            </button>
            <button type="button" class="qa-btn" (click)="goTo('/deposit')">
              <div class="qa-icon"><app-icon name="arrow-down" [size]="18" /></div>
              <div>
                <div class="qa-title">Deposit funds</div>
                <div class="qa-sub">From bank or card</div>
              </div>
            </button>
            <button type="button" class="qa-btn" (click)="goTo('/wallets')">
              <div class="qa-icon"><app-icon name="plus" [size]="18" /></div>
              <div>
                <div class="qa-title">Open wallet</div>
                <div class="qa-sub">EGP or USD wallet</div>
              </div>
            </button>
          </div>
        </div>
      </div>

      <div class="card">
        <div
          class="row between"
          style="padding: 20px 24px; border-bottom: 1px solid var(--border)"
        >
          <div>
            <div class="t-h3">Recent transactions</div>
            <div class="t-small secondary">Across all your wallets</div>
          </div>
          <button type="button" class="btn btn-ghost btn-sm" (click)="goTo('/history')">
            View all <app-icon name="arrow-right" [size]="14" />
          </button>
        </div>
        <app-tx-table [rows]="recentTransactions()" (rowClick)="openTransaction($event)" />
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  private readonly state = inject(AppStateService);
  private readonly router = inject(Router);

  protected readonly user = this.state.user;
  protected readonly wallets = this.state.wallets;
  protected readonly transactions = this.state.transactions;

  protected readonly firstName = computed(() => this.user().fullName.split(' ')[0]);

  protected readonly recentTransactions = computed(() => this.transactions().slice(0, 6));

  protected readonly totalBalance = computed(() =>
    this.wallets().reduce((a, w) => a + w.balance, 0)
  );

  protected readonly receivedDisplay = computed(() => {
    const sum = this.transactions()
      .filter((t) => t.type === 'in' || t.type === 'deposit')
      .reduce((a, t) => a + t.amount, 0);
    return '+' + this.formatNumber(sum);
  });

  protected readonly sentDisplay = computed(() => {
    const sum = this.transactions()
      .filter((t) => t.type === 'out' && t.status === 'completed')
      .reduce((a, t) => a + t.amount, 0);
    return '−' + this.formatNumber(sum);
  });

  protected readonly totalTransfers = computed(() =>
    String(this.transactions().filter((t) => t.type !== 'deposit').length)
  );

  protected readonly remainingSlots = computed(() => {
    const left = 3 - this.wallets().length;
    return left + (left === 1 ? ' slot' : ' slots');
  });

  protected goTo(path: string): void {
    void this.router.navigateByUrl(path);
  }

  protected openWallet(id: string): void {
    void this.router.navigate(['/wallets', id]);
  }

  protected openTransaction(tx: Transaction): void {
    void this.router.navigate(['/history'], { queryParams: { txId: tx.id } });
  }

  private formatNumber(n: number): string {
    return n.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }
}
