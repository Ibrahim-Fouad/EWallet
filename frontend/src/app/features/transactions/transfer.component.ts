import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { HttpErrorResponse } from '@angular/common/http';
import { map } from 'rxjs';

import {
  AppStateService,
  COUNTERPARTS,
  fmtAmount,
} from '../../core/services/app-state.service';
import { TransactionService } from '../../core/services/transaction.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { BreadcrumbsComponent } from '../../shared/layout/breadcrumbs.component';
import { FieldComponent } from '../../shared/ui/field.component';
import { ModalComponent } from '../../shared/ui/modal.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { WalletPickerComponent } from '../../shared/ui/wallet-picker.component';

type Step = 'form' | 'confirm' | 'fail';

interface TransferErrors {
  from?: string;
  to?: string;
  amount?: string;
}

@Component({
  selector: 'app-transfer',
  imports: [
    IconComponent,
    BreadcrumbsComponent,
    FieldComponent,
    ModalComponent,
    StatusBadgeComponent,
    WalletPickerComponent,
  ],
  template: `
    @if (step() === 'fail') {
      <div class="page" style="max-width: 560px; margin: 48px auto">
        <div class="card" style="padding: 40px; text-align: center">
          <div class="fail-icon">
            <app-icon name="x" [size]="36" [strokeWidth]="2.5" />
          </div>
          <h2 class="t-h1" style="margin-top: 24px">Transfer failed</h2>
          <p class="secondary" style="margin-top: 8px; text-wrap: balance">
            {{ failReason() }}
          </p>
          <div
            style="margin-top: 24px; padding: 16px; background: var(--surface-2); border-radius: 8px; text-align: left"
          >
            <div class="detail-row">
              <span class="lbl">Recipient</span>
              <span class="val t-num">{{ toPhone() }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Amount</span>
              <span class="val t-num">{{ formattedAmount() }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Status</span>
              <span class="val"><app-status-badge status="failed" /></span>
            </div>
          </div>
          <div class="row gap-2" style="margin-top: 24px; justify-content: center">
            <button type="button" class="btn btn-secondary" (click)="goDashboard()">
              Back to dashboard
            </button>
            <button type="button" class="btn btn-primary" (click)="backToForm()">
              Try again
            </button>
          </div>
        </div>
      </div>
    } @else {
      <div class="page" style="max-width: 720px; margin: 0 auto">
        <app-breadcrumbs
          [items]="[{ label: 'Dashboard', link: '/dashboard' }, { label: 'Transfer' }]"
        />
        <h2 class="t-h1">Send money</h2>
        <div class="row gap-4" style="align-items: flex-start">
          <div class="card card-pad col gap-4" style="flex: 2">
            <app-wallet-picker
              [wallets]="wallets()"
              [value]="fromId()"
              (valueChange)="setFromId($event)"
            />

            <app-field
              label="Recipient phone number"
              for="tr-to"
              [error]="errors().to"
              [help]="recipient() ? '' : 'Send by phone number — no IBANs needed.'"
            >
              <div class="input-w-icon">
                <span class="input-icon"><app-icon name="phone" [size]="16" /></span>
                <input
                  id="tr-to"
                  class="input t-num"
                  [class.error]="!!errors().to"
                  [value]="toPhone()"
                  (input)="onPhoneInput($event)"
                  placeholder="01XXXXXXXXX"
                  inputmode="numeric"
                  maxlength="11"
                />
              </div>
              @if (recipient(); as r) {
                <div
                  class="row gap-2"
                  style="padding: 10px 12px; background: var(--success-bg); border-radius: 8px; margin-top: 6px"
                >
                  <span style="color: var(--success); display: inline-flex">
                    <app-icon name="check-circle" [size]="16" />
                  </span>
                  <span class="t-small" style="color: #15803D; font-weight: 500">
                    Recipient found: {{ r.name }}
                  </span>
                </div>
              }
              <div class="row gap-2" style="margin-top: 8px; flex-wrap: wrap">
                <span class="t-tiny secondary" style="margin-right: 4px">Recent:</span>
                @for (c of recentContacts; track c.phone) {
                  <button type="button" class="chip" (click)="setToPhone(c.phone)">
                    {{ c.name }}
                  </button>
                }
              </div>
            </app-field>

            <app-field label="Amount" [error]="errors().amount">
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
                  {{ from()?.currency || 'EGP' }}
                </span>
                @if (showCurrencyMatch()) {
                  <div
                    style="position: absolute; right: 16px; top: 50%; transform: translateY(-50%); display: flex; align-items: center; gap: 6px"
                  >
                    <span style="color: var(--success); display: inline-flex">
                      <app-icon name="check-circle" [size]="14" />
                    </span>
                    <span class="t-small secondary">Currency match</span>
                  </div>
                }
              </div>
            </app-field>

            <app-field label="Note (optional)">
              <input
                class="input"
                [value]="note()"
                (input)="onNoteInput($event)"
                placeholder="What's this transfer for?"
              />
            </app-field>

            <div
              style="background: var(--primary-50); border: 1px solid var(--primary-100); border-radius: 8px; padding: 12px"
            >
              <div class="row gap-2" style="align-items: flex-start; color: var(--primary)">
                <span style="margin-top: 2px; flex-shrink: 0; display: inline-flex">
                  <app-icon name="shield" [size]="16" />
                </span>
                <div class="t-small" style="color: var(--primary-hover)">
                  <b>Idempotency protected.</b> If you accidentally tap "Confirm" twice, only one
                  transfer is sent.
                </div>
              </div>
            </div>
          </div>

          <div class="card card-pad col gap-3" style="flex: 1; position: sticky; top: 96px">
            <div class="t-h3">Summary</div>
            <div class="detail-row">
              <span class="lbl">From</span>
              <span class="val t-num">{{ from()?.phone || '—' }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">To</span>
              <span class="val t-num">{{ toPhone() || '—' }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Currency</span>
              <span class="val">{{ from()?.currency || '—' }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">Fee</span>
              <span class="val">Free</span>
            </div>
            <div
              class="detail-row"
              style="border-top: 1px solid var(--border); padding-top: 12px; margin-top: 8px"
            >
              <span style="font-weight: 600">Total</span>
              <span class="t-num" style="font-weight: 700; font-size: 18px">
                {{ summaryTotal() }}
              </span>
            </div>
            <button type="button" class="btn btn-primary btn-lg" (click)="proceed()">
              Continue <app-icon name="arrow-right" [size]="16" />
            </button>
          </div>
        </div>

        <app-modal
          [open]="step() === 'confirm'"
          title="Confirm transfer"
          [width]="460"
          (close)="backToForm()"
        >
          <div style="text-align: center; padding: 8px 0 16px">
            <div class="t-small secondary">You're about to send</div>
            <div
              class="t-num"
              style="font-size: 32px; font-weight: 700; margin-top: 4px; letter-spacing: -0.02em"
            >
              {{ formattedAmount() }}
            </div>
          </div>
          <div style="background: var(--surface-2); border-radius: 8px; padding: 16px">
            <div class="detail-row">
              <span class="lbl">From wallet</span>
              <span class="val t-num">{{ from()?.phone }}</span>
            </div>
            <div class="detail-row">
              <span class="lbl">To phone</span>
              <span class="val t-num">{{ toPhone() }}</span>
            </div>
            @if (recipient(); as r) {
              <div class="detail-row">
                <span class="lbl">Recipient</span>
                <span class="val">{{ r.name }}</span>
              </div>
            }
            @if (note()) {
              <div class="detail-row">
                <span class="lbl">Note</span>
                <span class="val">{{ note() }}</span>
              </div>
            }
          </div>
          <div
            class="row gap-2"
            style="margin-top: 16px; padding: 10px 12px; background: var(--warning-bg); border-radius: 8px; align-items: flex-start"
          >
            <span style="flex-shrink: 0; margin-top: 1px; color: var(--warning); display: inline-flex">
              <app-icon name="alert" [size]="16" />
            </span>
            <div class="t-small" style="color: #92400E">
              This action is irreversible. Double-check the recipient phone.
            </div>
          </div>

          <div modal-footer class="modal-footer">
            <button
              type="button"
              class="btn btn-secondary"
              [disabled]="submitting()"
              (click)="backToForm()"
            >
              Cancel
            </button>
            <button
              type="button"
              class="btn btn-primary"
              [disabled]="submitting()"
              (click)="confirmSend()"
            >
              @if (submitting()) {
                Sending…
              } @else {
                Confirm & send {{ formattedAmount() }}
              }
            </button>
          </div>
        </app-modal>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TransferComponent {
  private readonly state = inject(AppStateService);
  private readonly txService = inject(TransactionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly wallets = this.state.wallets;

  private readonly initialFromId = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('fromWalletId'))),
    { initialValue: null }
  );

  protected readonly fromId = signal<string | null>(null);
  protected readonly toPhone = signal('');
  protected readonly amount = signal('');
  protected readonly note = signal('');
  protected readonly errors = signal<TransferErrors>({});
  protected readonly step = signal<Step>('form');
  protected readonly failReason = signal<string>('');
  protected readonly submitting = signal(false);

  private readonly idempotencyKey = signal<string>(crypto.randomUUID());

  protected readonly recentContacts = COUNTERPARTS.slice(0, 3);

  protected readonly from = computed(
    () => this.wallets().find((w) => w.id === this.fromId()) ?? null
  );

  protected readonly amountNum = computed(() => parseFloat(this.amount()) || 0);

  protected readonly recipient = computed(() =>
    COUNTERPARTS.find((c) => c.phone === this.toPhone()) ?? null
  );

  protected readonly formattedAmount = computed(() => {
    const f = this.from();
    return fmtAmount(this.amountNum(), f?.currency ?? 'EGP');
  });

  protected readonly summaryTotal = computed(() => {
    if (this.amountNum() <= 0) return '—';
    return fmtAmount(this.amountNum(), this.from()?.currency ?? 'EGP');
  });

  protected readonly showCurrencyMatch = computed(() => {
    const f = this.from();
    if (!f) return false;
    const n = this.amountNum();
    return n > 0 && n <= f.balance;
  });

  constructor() {
    queueMicrotask(() => {
      const id = this.initialFromId() ?? this.wallets()[0]?.id ?? null;
      this.fromId.set(id);
    });
  }

  protected setFromId(id: string): void {
    this.fromId.set(id);
  }

  protected setToPhone(phone: string): void {
    this.toPhone.set(phone);
    this.errors.update((e) => ({ ...e, to: undefined }));
  }

  protected onPhoneInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value
      .replace(/[^0-9]/g, '')
      .slice(0, 11);
    this.toPhone.set(value);
    this.errors.update((e) => ({ ...e, to: undefined }));
  }

  protected onAmountInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value.replace(/[^0-9.]/g, '');
    this.amount.set(value);
    this.errors.update((e) => ({ ...e, amount: undefined }));
  }

  protected onNoteInput(event: Event): void {
    this.note.set((event.target as HTMLInputElement).value);
  }

  protected proceed(): void {
    if (!this.validate()) return;
    this.idempotencyKey.set(crypto.randomUUID());
    this.step.set('confirm');
  }

  protected confirmSend(): void {
    const from = this.from();
    if (!from) return;

    this.submitting.set(true);

    this.txService
      .transfer(
        {
          sourcePhoneNumber: from.phone,
          destinationPhoneNumber: this.toPhone(),
          amount: this.amountNum(),
          notes: this.note() || undefined,
        },
        this.idempotencyKey(),
      )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.state.addPendingOutboundTransaction({
            transactionId: res.transactionId,
            sourcePhone: from.phone,
            destinationPhone: this.toPhone(),
            amount: this.amountNum(),
            currency: res.currency,
          });
          this.state.pushToast({
            kind: 'info',
            title: 'Transfer is being processed',
            body: `${fmtAmount(res.amount, res.currency)} to ${this.toPhone()}`,
          });
          void this.router.navigateByUrl('/history');
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          this.failReason.set(this.mapBackendError(err));
          this.step.set('fail');
        },
      });
  }

  protected backToForm(): void {
    this.step.set('form');
  }

  protected goDashboard(): void {
    void this.router.navigateByUrl('/dashboard');
  }

  private validate(): boolean {
    const e: TransferErrors = {};
    const from = this.from();
    if (!this.fromId()) e.from = 'Select a wallet';
    const to = this.toPhone();
    if (!to) e.to = 'Enter recipient phone';
    else if (!/^01[0-9]{9}$/.test(to)) e.to = 'Egyptian format: 01XXXXXXXXX';
    else if (from && to === from.phone) e.to = "You can't transfer to yourself";
    const n = this.amountNum();
    if (n <= 0) e.amount = 'Enter an amount';
    else if (from && n > from.balance) {
      e.amount = `Exceeds balance of ${fmtAmount(from.balance, from.currency)}`;
    }
    this.errors.set(e);
    return Object.keys(e).length === 0;
  }

  private mapBackendError(err: unknown): string {
    if (err instanceof HttpErrorResponse) {
      const code = err.error?.code as string | undefined;
      if (code === 'Transaction.SelfTransfer') return 'You cannot transfer to your own wallet';
      if (code === 'Transaction.CurrencyMismatch')
        return 'Source and destination wallets must use the same currency';
      if (code === 'Transaction.DestinationNotFound')
        return 'No wallet found with that phone number';
      if (code === 'Transaction.InsufficientFunds') return 'Insufficient balance';
      if (code === 'Transfer.MissingIdempotencyKey') return 'Request error — please try again';
      if (err.status === 409) return 'A duplicate transfer was detected';
      const description = err.error?.description as string | undefined;
      if (description) return description;
    }
    return 'Something went wrong — please try again';
  }
}
