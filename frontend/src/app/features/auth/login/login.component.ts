import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { IconComponent } from '../../../shared/icons/icon.component';
import { FieldComponent } from '../../../shared/ui/field.component';
import { ToggleComponent } from '../../../shared/ui/toggle.component';
import { AuthIllustrationComponent } from '../auth-illustration/auth-illustration.component';

const EMAIL_PATTERN = /^\S+@\S+\.\S+$/;

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    IconComponent,
    FieldComponent,
    ToggleComponent,
    AuthIllustrationComponent,
  ],
  template: `
    <div class="auth-shell">
      <div class="auth-form-side">
        <div class="auth-brand">
          <div class="auth-brand-logo" style="color: #fff">
            <app-icon name="wallet" [size]="20" />
          </div>
          <div style="font-weight: 600; font-size: 16px">EWallet</div>
        </div>
        <div class="auth-form-inner">
          <h1 class="t-display" style="margin-bottom: 8px">Welcome back</h1>
          <p class="secondary" style="margin-bottom: 32px">
            Sign in to manage your wallets and transfers.
          </p>
          <form [formGroup]="form" (ngSubmit)="submit()" class="col gap-4" novalidate>
            <app-field
              label="Email"
              for="login-email"
              [error]="emailError()"
            >
              <div class="input-w-icon">
                <span class="input-icon"><app-icon name="mail" [size]="16" /></span>
                <input
                  id="login-email"
                  class="input"
                  [class.error]="!!emailError()"
                  type="email"
                  autocomplete="email"
                  placeholder="you@example.com"
                  formControlName="email"
                  [attr.aria-invalid]="!!emailError()"
                  [attr.aria-describedby]="emailError() ? 'login-email-error' : null"
                />
              </div>
            </app-field>

            <app-field
              label="Password"
              for="login-password"
              [error]="passwordError()"
            >
              <div class="input-w-icon">
                <span class="input-icon"><app-icon name="lock" [size]="16" /></span>
                <input
                  id="login-password"
                  class="input"
                  [class.error]="!!passwordError()"
                  [type]="showPwd() ? 'text' : 'password'"
                  autocomplete="current-password"
                  placeholder="Enter your password"
                  formControlName="password"
                  [attr.aria-invalid]="!!passwordError()"
                  [attr.aria-describedby]="passwordError() ? 'login-password-error' : null"
                />
                <button
                  type="button"
                  class="input-action"
                  (click)="togglePwd()"
                  [attr.aria-label]="showPwd() ? 'Hide password' : 'Show password'"
                  [attr.aria-pressed]="showPwd()"
                >
                  <app-icon [name]="showPwd() ? 'eye-off' : 'eye'" [size]="16" />
                </button>
              </div>
            </app-field>

            <div class="row between">
              <app-toggle formControlName="rememberMe" label="Keep me signed in" />
              <a
                class="t-small"
                style="color: var(--primary); font-weight: 500"
                href="#"
                (click)="$event.preventDefault()"
              >
                Forgot password?
              </a>
            </div>

            <button
              type="submit"
              class="btn btn-primary btn-lg"
              [disabled]="loading()"
              style="margin-top: 8px"
            >
              @if (loading()) {
                <span
                  class="spin"
                  style="width: 14px; height: 14px; border: 2px solid #fff; border-top-color: transparent; border-radius: 50%"
                ></span>
                Signing in…
              } @else {
                Sign in <app-icon name="arrow-right" [size]="16" />
              }
            </button>

            <div class="row center gap-2" style="margin: 8px 0; color: var(--text-muted)">
              <div style="flex: 1; height: 1px; background: var(--border)"></div>
              <span class="t-tiny">OR</span>
              <div style="flex: 1; height: 1px; background: var(--border)"></div>
            </div>

            <button type="button" class="btn btn-secondary btn-lg">
              <app-icon name="shield" [size]="16" /> Sign in with SSO
            </button>
          </form>
          <p class="t-small secondary" style="margin-top: 32px; text-align: center">
            New to EWallet?
            <a routerLink="/register" style="color: var(--primary); font-weight: 500; cursor: pointer">
              Create an account
            </a>
          </p>
        </div>
        <div class="t-tiny muted">© 2026 EWallet · Privacy · Terms</div>
      </div>
      <app-auth-illustration
        headline="One inbox for every currency you spend in."
        sub="Open up to three wallets, each with its own number, switch between EGP and USD, and send instantly to any wallet by phone number."
      />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly showPwd = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitted = signal(false);

  protected readonly form = this.fb.nonNullable.group({
    email: ['yara.mansour@example.com', [Validators.required, Validators.pattern(EMAIL_PATTERN)]],
    password: ['••••••••••', [Validators.required]],
    rememberMe: [true],
  });

  protected emailError(): string | null {
    if (!this.submitted()) return null;
    const c = this.form.controls.email;
    if (!c.errors) return null;
    if (c.errors['required']) return 'Email is required';
    if (c.errors['pattern']) return 'Enter a valid email';
    return null;
  }

  protected passwordError(): string | null {
    if (!this.submitted()) return null;
    const c = this.form.controls.password;
    if (c.errors?.['required']) return 'Password is required';
    return null;
  }

  protected togglePwd(): void {
    this.showPwd.update((v) => !v);
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    if (this.form.invalid) return;
    this.loading.set(true);
    const { email, password, rememberMe } = this.form.getRawValue();
    await this.auth.login({ email, password, rememberMe });
    this.loading.set(false);
    await this.router.navigateByUrl('/dashboard');
  }
}
