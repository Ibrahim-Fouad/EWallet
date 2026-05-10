import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { AppStateService } from '../../core/services/app-state.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { FieldComponent } from '../../shared/ui/field.component';
import { ModalComponent } from '../../shared/ui/modal.component';

interface CurrencyOption {
  code: 'EGP' | 'USD';
  name: string;
  symbol: string;
}

const CURRENCIES: CurrencyOption[] = [
  { code: 'EGP', name: 'Egyptian Pound', symbol: '£' },
  { code: 'USD', name: 'US Dollar', symbol: '$' },
];

@Component({
  selector: 'app-create-wallet-modal',
  imports: [
    ReactiveFormsModule,
    IconComponent,
    FieldComponent,
    ModalComponent,
  ],
  template: `
    <app-modal
      [open]="open()"
      title="Open new wallet"
      [width]="460"
      (close)="closeModal.emit()"
    >
      <form [formGroup]="form" class="col gap-4">
        <app-field
          label="Phone number"
          for="cw-phone"
          [error]="phoneError()"
          [help]="phoneError() ? '' : 'This will be your wallet\\'s unique identifier for receiving transfers.'"
        >
          <div class="input-w-icon">
            <span class="input-icon"><app-icon name="phone" [size]="16" /></span>
            <input
              id="cw-phone"
              class="input t-num"
              [class.error]="!!phoneError()"
              formControlName="phone"
              placeholder="01XXXXXXXXX"
              inputmode="numeric"
              maxlength="11"
              (input)="sanitizePhone($event)"
            />
          </div>
        </app-field>

        <app-field
          label="Currency"
          help="Choose the currency for this wallet. Cannot be changed later."
        >
          <div class="row gap-2">
            @for (c of currencies; track c.code) {
              <button
                type="button"
                style="flex: 1; padding: 12px 16px; border-radius: 8px; text-align: left; transition: all .15s"
                [style.border]="'1px solid ' + (currency() === c.code ? 'var(--primary)' : 'var(--border)')"
                [style.background]="currency() === c.code ? 'var(--primary-50)' : 'var(--surface)'"
                (click)="setCurrency(c.code)"
              >
                <div class="row between" style="margin-bottom: 4px">
                  <span
                    style="font-size: 18px; font-weight: 700"
                    [style.color]="currency() === c.code ? 'var(--primary)' : 'var(--text-primary)'"
                  >
                    {{ c.code }}
                  </span>
                  <span style="font-size: 22px; color: var(--text-muted); font-weight: 600">
                    {{ c.symbol }}
                  </span>
                </div>
                <div class="t-small secondary">{{ c.name }}</div>
              </button>
            }
          </div>
        </app-field>

        <div
          style="background: var(--primary-50); border: 1px solid var(--primary-100); border-radius: 8px; padding: 12px"
        >
          <div class="row gap-2" style="align-items: flex-start; color: var(--primary)">
            <span style="margin-top: 2px; flex-shrink: 0; display: inline-flex">
              <app-icon name="info" [size]="16" />
            </span>
            <div class="t-small" style="color: var(--primary-hover)">
              You can have up to <b>3 wallets</b> total. Each must have a unique phone number.
            </div>
          </div>
        </div>
      </form>

      <div modal-footer class="modal-footer">
        <button type="button" class="btn btn-secondary" (click)="closeModal.emit()">Cancel</button>
        <button type="button" class="btn btn-primary" [disabled]="submitting()" (click)="submit()">
          {{ submitting() ? 'Creating…' : 'Create wallet' }}
        </button>
      </div>
    </app-modal>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateWalletModalComponent {
  private readonly fb = inject(FormBuilder);
  private readonly state = inject(AppStateService);

  readonly open = input.required<boolean>();
  readonly closeModal = output<void>();
  readonly created = output<{ id: string }>();

  protected readonly currencies = CURRENCIES;
  protected readonly currency = signal<'EGP' | 'USD'>('EGP');
  protected readonly submitted = signal(false);
  protected readonly submitting = signal(false);
  private readonly serverPhoneError = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    phone: ['', [Validators.required, Validators.pattern(/^01[0-9]{9}$/)]],
  });

  constructor() {
    effect(() => {
      if (this.open()) {
        this.form.reset({ phone: '' });
        this.currency.set('EGP');
        this.submitted.set(false);
        this.submitting.set(false);
        this.serverPhoneError.set(null);
      }
    });
  }

  protected phoneError(): string | null {
    const server = this.serverPhoneError();
    if (server) return server;
    if (!this.submitted()) return null;
    const c = this.form.controls.phone;
    if (!c.errors) return null;
    if (c.errors['required']) return 'Phone number is required';
    if (c.errors['pattern']) return 'Egyptian format: 01XXXXXXXXX (11 digits)';
    return null;
  }

  protected setCurrency(code: 'EGP' | 'USD'): void {
    this.currency.set(code);
  }

  protected sanitizePhone(event: Event): void {
    const input = event.target as HTMLInputElement;
    const cleaned = input.value.replace(/[^0-9]/g, '').slice(0, 11);
    if (cleaned !== input.value) {
      input.value = cleaned;
      this.form.controls.phone.setValue(cleaned);
    }
    this.serverPhoneError.set(null);
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    if (this.form.invalid) return;

    this.submitting.set(true);
    this.serverPhoneError.set(null);

    try {
      const { phone } = this.form.getRawValue();
      const w = await this.state.createWallet({ phone, currency: this.currency() });
      this.state.pushToast({
        kind: 'success',
        title: 'Wallet created',
        body: `${this.currency()} wallet · ${phone}`,
      });
      this.created.emit({ id: w.id });
    } catch (err: unknown) {
      const code = this.extractErrorCode(err);
      const description = this.extractErrorDescription(err);
      if (code === 'Wallet.PhoneNumberAlreadyInUse') {
        this.serverPhoneError.set('This phone number is already in use.');
      } else {
        this.state.pushToast({
          kind: 'error',
          title: 'Could not create wallet',
          body: description ?? 'Please try again.',
        });
      }
    } finally {
      this.submitting.set(false);
    }
  }

  private extractErrorCode(err: unknown): string | null {
    if (err && typeof err === 'object' && 'error' in err) {
      const body = (err as { error?: { Code?: string } }).error;
      return body?.Code ?? null;
    }
    return null;
  }

  private extractErrorDescription(err: unknown): string | null {
    if (err && typeof err === 'object' && 'error' in err) {
      const body = (err as { error?: { Description?: string } }).error;
      return body?.Description ?? null;
    }
    return null;
  }
}
