import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-callback',
  imports: [RouterLink],
  template: `
    <div
      style="display:flex;align-items:center;justify-content:center;height:100vh;flex-direction:column;gap:16px"
      role="status"
      aria-live="polite"
    >
      @if (error()) {
        <p style="color:var(--error)">{{ error() }}</p>
        <a routerLink="/login" style="color:var(--primary);font-weight:500">Return to sign in</a>
      } @else {
        <span
          class="spin"
          style="width:32px;height:32px;border:3px solid var(--border);border-top-color:var(--primary);border-radius:50%"
          aria-hidden="true"
        ></span>
        <p class="secondary">Completing sign-in…</p>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CallbackComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth   = inject(AuthService);

  protected readonly error = signal<string | null>(null);

  ngOnInit(): void {
    void this.exchange();
  }

  private async exchange(): Promise<void> {
    const code  = this.route.snapshot.queryParamMap.get('code');
    const state = this.route.snapshot.queryParamMap.get('state');

    if (!code || !state) {
      this.error.set('Missing authorization code or state parameter.');
      return;
    }

    try {
      const returnUrl = await this.auth.handleCallback(code, state);
      await this.router.navigateByUrl(returnUrl, { replaceUrl: true });
    } catch (err) {
      this.error.set(err instanceof Error ? err.message : 'Authentication failed.');
    }
  }
}
