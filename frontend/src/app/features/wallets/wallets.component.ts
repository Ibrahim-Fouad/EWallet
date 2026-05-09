import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { AppStateService } from '../../core/services/app-state.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { WalletCardComponent } from '../../shared/ui/wallet-card.component';
import { CreateWalletModalComponent } from './create-wallet-modal.component';

@Component({
  selector: 'app-wallets',
  imports: [
    IconComponent,
    StatusBadgeComponent,
    WalletCardComponent,
    CreateWalletModalComponent,
  ],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <div class="t-tiny secondary" style="text-transform: uppercase; letter-spacing: 0.08em">
            Wallets
          </div>
          <h2 class="t-h1" style="margin-top: 4px">Your wallets · {{ wallets().length }}/3</h2>
          <p class="secondary t-small" style="margin-top: 6px">
            Each wallet has its own phone number and currency. Up to 3 wallets per account.
          </p>
        </div>
        @if (wallets().length < 3) {
          <button type="button" class="btn btn-primary" (click)="openCreate()">
            <app-icon name="plus" [size]="16" /> Open new wallet
          </button>
        }
      </div>

      <div style="display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px">
        @for (w of wallets(); track w.id) {
          <div class="card" style="overflow: hidden; display: flex; flex-direction: column">
            <app-wallet-card
              [wallet]="w"
              [compact]="true"
              [interactive]="true"
              (activate)="openWallet(w.id)"
            />
            <div style="padding: 16px; border-top: 1px solid var(--border)">
              <div class="row between" style="margin-bottom: 12px">
                <div>
                  <div
                    class="t-tiny muted"
                    style="text-transform: uppercase; letter-spacing: 0.06em"
                  >
                    Transactions
                  </div>
                  <div style="font-size: 18px; font-weight: 600">{{ txCount(w.id) }}</div>
                </div>
                <div>
                  <div
                    class="t-tiny muted"
                    style="text-transform: uppercase; letter-spacing: 0.06em"
                  >
                    Status
                  </div>
                  <app-status-badge [status]="$any(w.status)" />
                </div>
              </div>
              <div class="row gap-2">
                <button
                  type="button"
                  class="btn btn-secondary btn-sm"
                  style="flex: 1"
                  (click)="goDeposit(w.id)"
                >
                  <app-icon name="arrow-down" [size]="14" /> Deposit
                </button>
                <button
                  type="button"
                  class="btn btn-primary btn-sm"
                  style="flex: 1"
                  (click)="goTransfer(w.id)"
                >
                  <app-icon name="send" [size]="14" /> Send
                </button>
              </div>
            </div>
          </div>
        }

        @if (wallets().length < 3) {
          <button
            type="button"
            class="wallet-card-new"
            style="min-height: 360px"
            (click)="openCreate()"
          >
            <div
              style="width: 48px; height: 48px; border-radius: 999px; border: 2px dashed currentColor; display: grid; place-items: center"
            >
              <app-icon name="plus" [size]="24" />
            </div>
            <div style="font-weight: 600; font-size: 15px; margin-top: 8px">Open new wallet</div>
            <div class="t-small" style="max-width: 200px; text-align: center">
              {{ remainingText() }}. EGP or USD.
            </div>
          </button>
        }
      </div>

      <app-create-wallet-modal
        [open]="createOpen()"
        (closeModal)="closeCreate()"
        (created)="onCreated($event)"
      />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WalletsComponent {
  private readonly state = inject(AppStateService);
  private readonly router = inject(Router);

  protected readonly wallets = this.state.wallets;
  protected readonly createOpen = signal(false);

  protected readonly remainingText = computed(() => {
    const left = 3 - this.wallets().length;
    return `${left} of 3 slots remaining`;
  });

  protected txCount(walletId: string): number {
    return this.state.transactions().filter((t) => t.walletId === walletId).length;
  }

  protected openCreate(): void {
    this.createOpen.set(true);
  }

  protected closeCreate(): void {
    this.createOpen.set(false);
  }

  protected onCreated(payload: { id: string }): void {
    this.createOpen.set(false);
    void this.router.navigate(['/wallets', payload.id]);
  }

  protected openWallet(id: string): void {
    void this.router.navigate(['/wallets', id]);
  }

  protected goDeposit(id: string): void {
    void this.router.navigate(['/deposit'], { queryParams: { walletId: id } });
  }

  protected goTransfer(id: string): void {
    void this.router.navigate(['/transfer'], { queryParams: { fromWalletId: id } });
  }
}
