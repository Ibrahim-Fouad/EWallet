import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';

import {
  AppStateService,
  fmtAmount,
} from '../../core/services/app-state.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { CurrencyBadgeComponent } from '../../shared/ui/currency-badge.component';
import { FieldComponent } from '../../shared/ui/field.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';

@Component({
  selector: 'app-profile',
  imports: [
    ReactiveFormsModule,
    IconComponent,
    CurrencyBadgeComponent,
    FieldComponent,
    StatusBadgeComponent,
  ],
  template: `
    <div class="page" style="max-width: 920px; margin: 0 auto">
      <h2 class="t-h1">Profile</h2>

      <div class="card card-pad">
        <div class="row gap-4" style="align-items: center">
          <div class="avatar-lg">{{ user().avatar }}</div>
          <div class="col grow">
            <div class="t-h2">{{ user().fullName }}</div>
            <div class="secondary">{{ user().email }}</div>
            <div class="row gap-3" style="margin-top: 8px">
              <span class="badge badge-primary">
                <app-icon name="wallet" [size]="11" /> {{ walletCountLabel() }}
              </span>
              <span class="badge badge-success"><span class="badge-dot"></span> Verified</span>
              <span class="t-tiny secondary">Joined {{ joinedLabel() }}</span>
            </div>
          </div>
          <button type="button" class="btn btn-secondary">
            <app-icon name="camera" [size]="14" /> Change photo
          </button>
        </div>
      </div>

      <div class="card card-pad">
        <div class="row between" style="margin-bottom: 20px">
          <div class="t-h3">Personal information</div>
          @if (!editing()) {
            <button type="button" class="btn btn-secondary btn-sm" (click)="startEdit()">
              <app-icon name="edit" [size]="14" /> Edit
            </button>
          } @else {
            <div class="row gap-2">
              <button type="button" class="btn btn-secondary btn-sm" (click)="cancelEdit()">
                Cancel
              </button>
              <button type="button" class="btn btn-primary btn-sm" (click)="save()">
                Save changes
              </button>
            </div>
          }
        </div>

        <form [formGroup]="form" style="display: grid; grid-template-columns: 1fr 1fr; gap: 20px">
          <app-field label="Full name">
            <input class="input" formControlName="fullName" />
          </app-field>
          <app-field label="Email address">
            <input class="input" formControlName="email" />
          </app-field>
          <app-field label="Phone">
            <input class="input t-num" formControlName="phone" />
          </app-field>
          <app-field label="Member since">
            <input class="input" [value]="memberSince()" disabled />
          </app-field>
        </form>
      </div>

      <div class="card card-pad">
        <div class="t-h3" style="margin-bottom: 16px">Your wallets</div>
        <div class="col gap-2">
          @for (w of wallets(); track w.id) {
            <div
              class="row gap-3"
              style="padding: 12px; background: var(--surface-2); border-radius: 8px"
            >
              <app-currency-badge [currency]="w.currency" />
              <div class="col grow">
                <div class="t-num" style="font-weight: 500">{{ w.phone }}</div>
                <div class="t-small secondary t-num">{{ formatBalance(w.balance, w.currency) }}</div>
              </div>
              <app-status-badge [status]="$any(w.status)" />
            </div>
          }
        </div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent {
  private readonly state = inject(AppStateService);
  private readonly fb = inject(FormBuilder);

  protected readonly user = this.state.user;
  protected readonly wallets = this.state.wallets;
  protected readonly editing = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    fullName: [this.user().fullName],
    email: [this.user().email],
    phone: [this.user().phone],
  });

  constructor() {
    this.form.disable();
  }

  protected readonly walletCountLabel = computed(() => {
    const n = this.wallets().length;
    return `${n} wallet${n === 1 ? '' : 's'}`;
  });

  protected readonly joinedLabel = computed(() =>
    new Date(this.user().joined).toLocaleDateString('en-US', {
      month: 'long',
      year: 'numeric',
    })
  );

  protected readonly memberSince = computed(() =>
    new Date(this.user().joined).toLocaleDateString('en-US', {
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    })
  );

  protected formatBalance(balance: number, currency: string): string {
    return fmtAmount(balance, currency);
  }

  protected startEdit(): void {
    this.form.reset({
      fullName: this.user().fullName,
      email: this.user().email,
      phone: this.user().phone,
    });
    this.form.enable();
    this.editing.set(true);
  }

  protected cancelEdit(): void {
    this.form.reset({
      fullName: this.user().fullName,
      email: this.user().email,
      phone: this.user().phone,
    });
    this.form.disable();
    this.editing.set(false);
  }

  protected save(): void {
    const { fullName, email, phone } = this.form.getRawValue();
    this.state.user.update((u) => ({ ...u, fullName, email, phone }));
    this.form.disable();
    this.editing.set(false);
    this.state.pushToast({ kind: 'success', title: 'Profile updated' });
  }
}
