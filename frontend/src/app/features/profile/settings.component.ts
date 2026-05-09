import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { AppStateService } from '../../core/services/app-state.service';
import { AuthService } from '../../core/services/auth.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { FieldComponent } from '../../shared/ui/field.component';
import { ModalComponent } from '../../shared/ui/modal.component';
import { ToggleComponent } from '../../shared/ui/toggle.component';

interface NotifPref {
  id: 'inbound' | 'outbound' | 'deposits' | 'security' | 'marketing';
  title: string;
  sub: string;
}

const NOTIF_PREFS: NotifPref[] = [
  { id: 'inbound', title: 'Incoming transfers', sub: 'Notify me when I receive money' },
  { id: 'outbound', title: 'Outgoing transfers', sub: 'Notify me when a transfer I sent is completed' },
  { id: 'deposits', title: 'Deposits', sub: 'Notify me when funds arrive in a wallet' },
  { id: 'security', title: 'Security alerts', sub: 'Sign-in attempts and password changes' },
  { id: 'marketing', title: 'Product updates', sub: 'Occasional emails about new features' },
];

@Component({
  selector: 'app-settings',
  imports: [
    ReactiveFormsModule,
    IconComponent,
    FieldComponent,
    ModalComponent,
    ToggleComponent,
  ],
  template: `
    <div class="page" style="max-width: 920px; margin: 0 auto">
      <h2 class="t-h1">Settings</h2>

      <div class="card card-pad">
        <div class="t-h3" style="margin-bottom: 8px">Notifications</div>
        <p class="t-small secondary" style="margin-bottom: 16px">
          Choose which events trigger alerts and emails.
        </p>
        <form [formGroup]="notifForm" class="col">
          @for (item of notifPrefs; track item.id) {
            <div
              class="row between"
              style="padding: 14px 0; border-bottom: 1px solid var(--border)"
            >
              <div>
                <div style="font-weight: 500; font-size: 14px">{{ item.title }}</div>
                <div class="t-small secondary" style="margin-top: 2px">{{ item.sub }}</div>
              </div>
              <app-toggle [formControlName]="item.id" />
            </div>
          }
        </form>
      </div>

      <div class="card card-pad">
        <div class="t-h3" style="margin-bottom: 8px">Security</div>
        <p class="t-small secondary" style="margin-bottom: 16px">
          Protect your account and transfers.
        </p>
        <form [formGroup]="securityForm" class="col">
          <div
            class="row between"
            style="padding: 14px 0; border-bottom: 1px solid var(--border)"
          >
            <div>
              <div style="font-weight: 500; font-size: 14px">Password</div>
              <div class="t-small secondary" style="margin-top: 2px">Last changed 4 months ago</div>
            </div>
            <button type="button" class="btn btn-secondary btn-sm" (click)="openPwModal()">
              Change password
            </button>
          </div>
          <div
            class="row between"
            style="padding: 14px 0; border-bottom: 1px solid var(--border)"
          >
            <div>
              <div style="font-weight: 500; font-size: 14px">Two-factor authentication</div>
              <div class="t-small secondary" style="margin-top: 2px">
                Require a code on every sign-in
              </div>
            </div>
            <app-toggle formControlName="twoFactor" />
          </div>
          <div class="row between" style="padding: 14px 0">
            <div>
              <div style="font-weight: 500; font-size: 14px">Transfer confirmation</div>
              <div class="t-small secondary" style="margin-top: 2px">
                Always show a confirmation dialog before sending
              </div>
            </div>
            <app-toggle formControlName="confirmTransfers" />
          </div>
        </form>
      </div>

      <div class="card card-pad">
        <div class="t-h3" style="margin-bottom: 8px">Account</div>
        <div class="col">
          <div
            class="row between"
            style="padding: 14px 0; border-bottom: 1px solid var(--border)"
          >
            <div>
              <div style="font-weight: 500; font-size: 14px">Sign out of this session</div>
              <div class="t-small secondary" style="margin-top: 2px">
                You'll need to sign in again to use EWallet
              </div>
            </div>
            <button type="button" class="btn btn-secondary btn-sm" (click)="openLogoutModal()">
              <app-icon name="logout" [size]="14" /> Log out
            </button>
          </div>
          <div class="row between" style="padding: 14px 0">
            <div>
              <div style="font-weight: 500; font-size: 14px; color: var(--danger)">
                Delete account
              </div>
              <div class="t-small secondary" style="margin-top: 2px">
                Permanently remove your account and all wallets
              </div>
            </div>
            <button
              type="button"
              class="btn btn-secondary btn-sm"
              style="color: var(--danger); border-color: var(--danger-bg)"
            >
              Delete
            </button>
          </div>
        </div>
      </div>

      <app-modal
        [open]="pwOpen()"
        title="Change password"
        (close)="closePwModal()"
      >
        <form [formGroup]="pwForm" class="col gap-3">
          <app-field label="Current password">
            <input type="password" class="input" formControlName="current" placeholder="Enter current password" />
          </app-field>
          <app-field label="New password" help="At least 8 characters with letters and numbers">
            <input type="password" class="input" formControlName="next" placeholder="Enter new password" />
          </app-field>
          <app-field label="Confirm new password">
            <input type="password" class="input" formControlName="confirm" placeholder="Re-enter new password" />
          </app-field>
        </form>
        <div modal-footer class="modal-footer">
          <button type="button" class="btn btn-secondary" (click)="closePwModal()">Cancel</button>
          <button type="button" class="btn btn-primary" (click)="updatePassword()">
            Update password
          </button>
        </div>
      </app-modal>

      <app-modal
        [open]="logoutOpen()"
        title="Log out?"
        [width]="400"
        (close)="closeLogoutModal()"
      >
        <p>You'll need to enter your email and password again to use EWallet.</p>
        <div modal-footer class="modal-footer">
          <button type="button" class="btn btn-secondary" (click)="closeLogoutModal()">
            Stay signed in
          </button>
          <button type="button" class="btn btn-danger" (click)="confirmLogout()">Log out</button>
        </div>
      </app-modal>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly state = inject(AppStateService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly notifPrefs = NOTIF_PREFS;

  protected readonly notifForm = this.fb.nonNullable.group({
    inbound: [true],
    outbound: [true],
    deposits: [true],
    security: [true],
    marketing: [false],
  });

  protected readonly securityForm = this.fb.nonNullable.group({
    twoFactor: [true],
    confirmTransfers: [true],
  });

  protected readonly pwForm = this.fb.nonNullable.group({
    current: [''],
    next: [''],
    confirm: [''],
  });

  protected readonly pwOpen = signal(false);
  protected readonly logoutOpen = signal(false);

  protected openPwModal(): void {
    this.pwForm.reset({ current: '', next: '', confirm: '' });
    this.pwOpen.set(true);
  }

  protected closePwModal(): void {
    this.pwOpen.set(false);
  }

  protected updatePassword(): void {
    this.pwOpen.set(false);
    this.state.pushToast({ kind: 'success', title: 'Password updated' });
  }

  protected openLogoutModal(): void {
    this.logoutOpen.set(true);
  }

  protected closeLogoutModal(): void {
    this.logoutOpen.set(false);
  }

  protected confirmLogout(): void {
    this.logoutOpen.set(false);
    this.auth.logout();
    void this.router.navigateByUrl('/login');
  }
}
