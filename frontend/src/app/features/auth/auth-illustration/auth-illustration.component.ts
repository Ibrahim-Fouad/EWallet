import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { Wallet, WalletCardComponent } from '../../../shared/ui/wallet-card.component';

@Component({
  selector: 'app-auth-illustration',
  imports: [WalletCardComponent],
  template: `
    <aside class="auth-illus-side" aria-hidden="true">
      <div class="auth-illus-bg"></div>
      <div class="auth-illus-grid"></div>
      <div class="auth-illus-content">
        <div class="row gap-2" style="margin-bottom: 24px">
          <span class="live-dot"></span>
          <span
            class="t-tiny"
            style="color: rgba(255,255,255,0.7); letter-spacing: 0.1em; text-transform: uppercase"
          >
            REAL-TIME · MULTI-CURRENCY · SECURE
          </span>
        </div>
        <h2
          style="font-size: 36px; font-weight: 700; line-height: 1.15; letter-spacing: -0.02em; margin: 0; text-wrap: balance"
        >
          {{ headline() }}
        </h2>
        <p style="font-size: 15px; color: rgba(255,255,255,0.7); margin-top: 16px; line-height: 1.5">
          {{ sub() }}
        </p>
        <div class="auth-card-stack">
          @for (w of demoWallets; track w.phone) {
            <app-wallet-card [wallet]="w" />
          }
        </div>
        <div class="row gap-6" style="margin-top: 24px">
          <div>
            <div style="font-size: 24px; font-weight: 700">3</div>
            <div class="t-small" style="color: rgba(255,255,255,0.6)">wallets max</div>
          </div>
          <div>
            <div style="font-size: 24px; font-weight: 700">2</div>
            <div class="t-small" style="color: rgba(255,255,255,0.6)">currencies</div>
          </div>
          <div>
            <div style="font-size: 24px; font-weight: 700">0₣</div>
            <div class="t-small" style="color: rgba(255,255,255,0.6)">transfer fees</div>
          </div>
        </div>
      </div>
    </aside>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthIllustrationComponent {
  readonly headline = input.required<string>();
  readonly sub = input.required<string>();

  protected readonly demoWallets: Wallet[] = [
    { phone: '01098765432', currency: 'USD', balance: 1250.4, status: 'active', color: 'indigo' },
    { phone: '01187766554', currency: 'EGP', balance: 6420.0, status: 'active', color: 'slate' },
    { phone: '01012345678', currency: 'EGP', balance: 18420.75, status: 'active', primary: true, color: 'blue' },
  ];
}
