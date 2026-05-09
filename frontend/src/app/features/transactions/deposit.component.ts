import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import {
  AppStateService,
  Transaction,
  fmtAmount,
} from '../../core/services/app-state.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { BreadcrumbsComponent } from '../../shared/layout/breadcrumbs.component';
import { FieldComponent } from '../../shared/ui/field.component';
import { WalletPickerComponent } from '../../shared/ui/wallet-picker.component';

type Method = 'bank' | 'card';
type Step = 'form' | 'success';

@Component({
  selector: 'app-deposit',
  imports: [
    IconComponent,
    BreadcrumbsComponent,
    FieldComponent,
    WalletPickerComponent,
  ],
  template: `
    @if (step() === 'success' && successTx() && walletForSummary(); as ctx) {
      <div class="page" style="max-width: 560px; margin: 48px auto">
        <div class="card" style="padding: 40px; text-align: center">
          <div class="success-icon"><app-icon name="check" [size]="36" [strokeWidth]="2.5" /></div>
          <h2 class="t-h1" style="margin-top: 24px">Deposit completed</h2>
          <p class="secondary" style="margin-top: 8px">
            <span class="t-num" style="font-weight: 600; color: var(--text-primary)">
              {{ formattedAmount() }}
            </span>
            added to your {{ walletForSummary()!.currency }} wallet.
          </p>
          <div style="margin-top: 24px; padding: 16px; background: var(--surface-2); border-radius: 8px; text-align: left">
            <div class="detail-row">
              <span class="lbl">Transaction ID</span>
              <span class="val t-mono t-small">{{ successTx()!.id }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Wallet</span>
              <span class="val t-num">{{ walletForSummary()!.phone }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">New balance</span>
              <span class="val t-num">{{ formattedBalance() }}</span>
            </div>
          </div>
          <div class="row gap-2" style="margin-top: 24px; justify-content: center">
            <button type="button" class="btn btn-secondary" (click)="goDashboard()">
              Go to dashboard
            </button>
            <button type="button" class="btn btn-primary" (click)="depositAgain()">
              Deposit again
            </button>
          </div>
        </div>
      </div>
    } @else {
      <div class="page" style="max-width: 720px; margin: 0 auto">
        <app-breadcrumbs
          [items]="[{ label: 'Dashboard', link: '/dashboard' }, { label: 'Deposit' }]"
        />
        <h2 class="t-h1">Deposit funds</h2>

        <div class="row gap-4" style="align-items: flex-start">
          <div class="card card-pad col gap-4" style="flex: 2">
            <app-wallet-picker
              [wallets]="wallets()"
              [value]="walletId()"
              label="Deposit into"
              (valueChange)="setWalletId($event)"
            />

            <app-field label="Amount" [help]="amountHelp()">
              <div style="position: relative">
                <input
                  class="input t-num"
                  style="font-size: 28px; height: 64px; font-weight: 600; padding-left: 60px"
                  [value]="amount()"
                  (input)="onAmountInput($event)"
                  placeholder="0.00"
                  inputmode="decimal"
                />
                <span
                  style="position: absolute; left: 16px; top: 50%; transform: translateY(-50%); font-size: 14px; font-weight: 700; color: var(--text-muted)"
                >
                  {{ currentCurrency() }}
                </span>
              </div>
              <div class="row gap-2" style="margin-top: 4px">
                @for (v of presets; track v) {
                  <button type="button" class="chip" (click)="setAmount(v)">
                    +{{ v.toLocaleString() }}
                  </button>
                }
              </div>
            </app-field>

            <app-field label="Funding source">
              <div class="col gap-2">
                @for (m of methods; track m.id) {
                  <button
                    type="button"
                    style="padding: 14px; border-radius: 8px; text-align: left; display: flex; align-items: center; gap: 12px"
                    [style.border]="'1px solid ' + (method() === m.id ? 'var(--primary)' : 'var(--border)')"
                    [style.background]="method() === m.id ? 'var(--primary-50)' : 'var(--surface)'"
                    (click)="setMethod(m.id)"
                  >
                    <div
                      style="width: 36px; height: 36px; border-radius: 8px; display: grid; place-items: center"
                      [style.background]="method() === m.id ? 'var(--primary)' : 'var(--surface-3)'"
                      [style.color]="method() === m.id ? '#fff' : 'var(--text-secondary)'"
                    >
                      <app-icon name="wallet" [size]="16" />
                    </div>
                    <div class="col grow">
                      <div style="font-weight: 500">{{ m.title }}</div>
                      <div class="t-small secondary">{{ m.sub }}</div>
                    </div>
                    @if (method() === m.id) {
                      <span style="color: var(--primary); display: inline-flex">
                        <app-icon name="check" [size]="16" />
                      </span>
                    }
                  </button>
                }
              </div>
            </app-field>
          </div>

          <div class="card card-pad col gap-3" style="flex: 1; position: sticky; top: 96px">
            <div class="t-h3">Summary</div>
            <div class="detail-row">
              <span class="lbl">Amount</span>
              <span class="val t-num">{{ summaryAmount() }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Fee</span>
              <span class="val">{{ summaryFee() }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Arrives</span>
              <span class="val">{{ method() === 'card' ? 'Instantly' : '1–2 days' }}</span>
            </div>
            <div
              class="detail-row"
              style="border-top: 1px solid var(--border); padding-top: 12px; margin-top: 8px"
            >
              <span style="font-weight: 600">Total credited</span>
              <span class="t-num" style="font-weight: 700; font-size: 18px">
                {{ summaryTotal() }}
              </span>
            </div>
            <button
              type="button"
              class="btn btn-primary btn-lg"
              [disabled]="!canSubmit()"
              (click)="submit()"
            >
              Confirm deposit <app-icon name="arrow-right" [size]="16" />
            </button>
            <div
              class="t-tiny secondary"
              style="text-align: center; display: inline-flex; align-items: center; gap: 4px; justify-content: center"
            >
              <app-icon name="shield" [size]="11" /> Encrypted · 256-bit TLS
            </div>
          </div>
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DepositComponent {
  private readonly state = inject(AppStateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly wallets = this.state.wallets;

  private readonly initialWalletId = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('walletId'))),
    { initialValue: null }
  );

  protected readonly walletId = signal<string | null>(null);
  protected readonly amount = signal('');
  protected readonly method = signal<Method>('bank');
  protected readonly step = signal<Step>('form');
  protected readonly successTx = signal<Transaction | null>(null);
  private readonly successWalletId = signal<string | null>(null);

  protected readonly presets = [100, 500, 1000, 5000];
  protected readonly methods = [
    { id: 'bank' as Method, title: 'Bank transfer', sub: '1-2 business days · No fees' },
    { id: 'card' as Method, title: 'Debit / credit card', sub: 'Instant · 1.5% fee' },
  ];

  protected readonly currentWallet = computed(
    () => this.wallets().find((w) => w.id === this.walletId()) ?? null
  );

  protected readonly currentCurrency = computed(
    () => this.currentWallet()?.currency ?? 'EGP'
  );

  protected readonly amountNum = computed(() => parseFloat(this.amount()) || 0);

  protected readonly canSubmit = computed(
    () => this.amountNum() > 0 && this.walletId() != null
  );

  protected readonly amountHelp = computed(() => {
    const w = this.currentWallet();
    return w ? `Available: ${fmtAmount(w.balance, w.currency)}` : '';
  });

  protected readonly summaryAmount = computed(() =>
    this.amountNum() > 0 ? fmtAmount(this.amountNum(), this.currentCurrency()) : '—'
  );

  protected readonly summaryFee = computed(() =>
    this.method() === 'card'
      ? fmtAmount(this.amountNum() * 0.015, this.currentCurrency())
      : 'Free'
  );

  protected readonly summaryTotal = computed(() => {
    const n = this.amountNum();
    if (n <= 0) return '—';
    const total = this.method() === 'card' ? n * 0.985 : n;
    return fmtAmount(total, this.currentCurrency());
  });

  protected readonly walletForSummary = computed(
    () => this.wallets().find((w) => w.id === this.successWalletId()) ?? null
  );

  protected readonly formattedAmount = computed(() => {
    const tx = this.successTx();
    return tx ? fmtAmount(tx.amount, tx.currency) : '';
  });

  protected readonly formattedBalance = computed(() => {
    const w = this.walletForSummary();
    return w ? fmtAmount(w.balance, w.currency) : '';
  });

  constructor() {
    // Initialise selected wallet once params are read.
    queueMicrotask(() => {
      const id = this.initialWalletId() ?? this.wallets()[0]?.id ?? null;
      this.walletId.set(id);
    });
  }

  protected setWalletId(id: string): void {
    this.walletId.set(id);
  }

  protected setAmount(v: number): void {
    this.amount.set(String(v));
  }

  protected setMethod(m: Method): void {
    this.method.set(m);
  }

  protected onAmountInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value.replace(/[^0-9.]/g, '');
    this.amount.set(value);
  }

  protected submit(): void {
    if (!this.canSubmit()) return;
    const id = this.walletId()!;
    const r = this.state.deposit({ walletId: id, amount: this.amountNum() });
    if (r.ok) {
      this.successTx.set(r.tx);
      this.successWalletId.set(id);
      this.step.set('success');
      this.state.pushToast({
        kind: 'success',
        title: 'Deposit completed',
        body: `${fmtAmount(this.amountNum(), this.currentCurrency())} added`,
      });
    }
  }

  protected depositAgain(): void {
    this.step.set('form');
    this.amount.set('');
    this.successTx.set(null);
  }

  protected goDashboard(): void {
    void this.router.navigateByUrl('/dashboard');
  }
}
