import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../../core/services/auth.service';
import { IconComponent } from '../../../shared/icons/icon.component';
import { FieldComponent } from '../../../shared/ui/field.component';
import { AuthIllustrationComponent } from '../auth-illustration/auth-illustration.component';

const EMAIL_PATTERN = /^\S+@\S+\.\S+$/;

const STRENGTH_LABEL = ['Weak', 'Weak', 'Fair', 'Good', 'Strong'];
const STRENGTH_COLOR = ['#CBD5E1', '#DC2626', '#D97706', '#2563EB', '#16A34A'];

function passwordsMatch(group: AbstractControl): ValidationErrors | null {
  const password = group.get('password')?.value;
  const confirm = group.get('confirm')?.value;
  if (!confirm) return null;
  return password === confirm ? null : { passwordsMismatch: true };
}

function pwStrength(p: string): number {
  let s = 0;
  if (p.length >= 8) s++;
  if (/[A-Z]/.test(p)) s++;
  if (/[0-9]/.test(p)) s++;
  if (/[^A-Za-z0-9]/.test(p)) s++;
  return s;
}

@Component({
  selector: 'app-register',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    IconComponent,
    FieldComponent,
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
          <h1 class="t-display" style="margin-bottom: 8px">Create your account</h1>
          <p class="secondary" style="margin-bottom: 28px">Free forever. No card required.</p>
          <form [formGroup]="form" (ngSubmit)="submit()" class="col gap-4" novalidate>
            <app-field label="Full name" for="reg-name" [error]="nameError()">
              <div class="input-w-icon">
                <span class="input-icon"><app-icon name="user" [size]="16" /></span>
                <input
                  id="reg-name"
                  class="input"
                  [class.error]="!!nameError()"
                  formControlName="name"
                  placeholder="Yara Mansour"
                  autocomplete="name"
                  [attr.aria-invalid]="!!nameError()"
                />
              </div>
            </app-field>

            <app-field label="Email" for="reg-email" [error]="emailError()">
              <div class="input-w-icon">
                <span class="input-icon"><app-icon name="mail" [size]="16" /></span>
                <input
                  id="reg-email"
                  class="input"
                  [class.error]="!!emailError()"
                  type="email"
                  formControlName="email"
                  placeholder="you@example.com"
                  autocomplete="email"
                  [attr.aria-invalid]="!!emailError()"
                />
              </div>
            </app-field>

            <app-field label="Password" for="reg-password" [error]="passwordError()">
              <div class="input-w-icon">
                <span class="input-icon"><app-icon name="lock" [size]="16" /></span>
                <input
                  id="reg-password"
                  class="input"
                  [class.error]="!!passwordError()"
                  [type]="showPwd() ? 'text' : 'password'"
                  formControlName="password"
                  placeholder="At least 8 characters"
                  autocomplete="new-password"
                  [attr.aria-invalid]="!!passwordError()"
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
              @if (passwordValue()) {
                <div class="row gap-2" style="margin-top: 6px">
                  <div
                    style="flex: 1; height: 4px; background: var(--surface-3); border-radius: 999px; overflow: hidden; display: flex; gap: 2px"
                  >
                    @for (i of [0, 1, 2, 3]; track i) {
                      <div
                        style="flex: 1; transition: all .2s"
                        [style.background]="i < strength() ? strengthColor() : 'transparent'"
                      ></div>
                    }
                  </div>
                  <div
                    class="t-tiny"
                    style="font-weight: 600; min-width: 50px; text-align: right"
                    [style.color]="strengthColor()"
                  >
                    {{ strengthText() }}
                  </div>
                </div>
              }
            </app-field>

            <app-field label="Confirm password" for="reg-confirm" [error]="confirmError()">
              <div class="input-w-icon">
                <span class="input-icon"><app-icon name="lock" [size]="16" /></span>
                <input
                  id="reg-confirm"
                  class="input"
                  [class.error]="!!confirmError()"
                  [type]="showPwd() ? 'text' : 'password'"
                  formControlName="confirm"
                  placeholder="Re-enter your password"
                  autocomplete="new-password"
                  [attr.aria-invalid]="!!confirmError()"
                />
              </div>
            </app-field>

            <div class="row gap-2" style="align-items: flex-start">
              <input id="reg-terms" type="checkbox" formControlName="terms" style="margin-top: 3px" />
              <label for="reg-terms" class="t-small secondary" style="cursor: pointer">
                I agree to the
                <a href="#" (click)="$event.preventDefault()" style="color: var(--primary); font-weight: 500">Terms of Service</a>
                and
                <a href="#" (click)="$event.preventDefault()" style="color: var(--primary); font-weight: 500">Privacy Policy</a>.
              </label>
            </div>

            <button type="submit" class="btn btn-primary btn-lg" [disabled]="loading()">
              @if (loading()) {
                Creating account…
              } @else {
                Create account <app-icon name="arrow-right" [size]="16" />
              }
            </button>
          </form>
          <p class="t-small secondary" style="margin-top: 24px; text-align: center">
            Already have an account?
            <a routerLink="/login" style="color: var(--primary); font-weight: 500; cursor: pointer">Sign in</a>
          </p>
        </div>
        <div class="t-tiny muted">© 2026 EWallet · Privacy · Terms</div>
      </div>
      <app-auth-illustration
        headline="Up to 3 wallets. Two currencies. Zero hassle."
        sub="Each wallet has its own phone number. Send and receive instantly using only a number — no IBANs, no fees."
      />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly showPwd = signal(false);
  protected readonly loading = signal(false);
  protected readonly submitted = signal(false);

  protected readonly form = this.fb.nonNullable.group(
    {
      name: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.pattern(EMAIL_PATTERN)]],
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirm: ['', [Validators.required]],
      terms: [true, [Validators.requiredTrue]],
    },
    { validators: [passwordsMatch] }
  );

  protected readonly passwordValue = toSignal(this.form.controls.password.valueChanges, {
    initialValue: this.form.controls.password.value,
  });

  protected readonly strength = computed(() => pwStrength(this.passwordValue() ?? ''));
  protected readonly strengthText = computed(() => STRENGTH_LABEL[this.strength()]);
  protected readonly strengthColor = computed(() => STRENGTH_COLOR[this.strength()]);

  protected nameError(): string | null {
    if (!this.submitted()) return null;
    const c = this.form.controls.name;
    if (!c.errors) return null;
    if (c.errors['required'] || c.errors['minlength']) return 'Enter your full name';
    return null;
  }

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
    if (c.errors?.['required'] || c.errors?.['minlength']) return 'Min 8 characters';
    return null;
  }

  protected confirmError(): string | null {
    if (!this.submitted()) return null;
    if (this.form.errors?.['passwordsMismatch']) return "Passwords don't match";
    if (this.form.controls.confirm.errors?.['required']) return "Passwords don't match";
    return null;
  }

  protected togglePwd(): void {
    this.showPwd.update((v) => !v);
  }

  protected async submit(): Promise<void> {
    this.submitted.set(true);
    if (this.form.invalid) return;
    this.loading.set(true);
    const { name, email, password } = this.form.getRawValue();
    await this.auth.register({ fullName: name, email, password });
    this.loading.set(false);
    await this.router.navigateByUrl('/dashboard');
  }
}
