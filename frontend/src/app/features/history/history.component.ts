import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';

import { AppStateService, Transaction } from '../../core/services/app-state.service';
import { IconComponent } from '../../shared/icons/icon.component';
import { DrawerComponent } from '../../shared/ui/drawer.component';
import { TxTableComponent } from '../../shared/ui/tx-table.component';
import { TxDetailComponent } from './tx-detail.component';

type TypeFilter = 'all' | 'in' | 'out';

const PAGE_SIZE = 8;

@Component({
  selector: 'app-history',
  imports: [
    IconComponent,
    DrawerComponent,
    TxTableComponent,
    TxDetailComponent,
  ],
  template: `
    <div class="page">
      <div class="page-header">
        <div>
          <div class="t-tiny secondary" style="text-transform: uppercase; letter-spacing: 0.08em">
            Activity
          </div>
          <h2 class="t-h1" style="margin-top: 4px">Transaction history</h2>
          <p class="secondary t-small" style="margin-top: 4px">
            {{ filtered().length }} of {{ allTransactions().length }} transactions
          </p>
        </div>
        <button type="button" class="btn btn-secondary">
          <app-icon name="arrow-down" [size]="14" /> Export CSV
        </button>
      </div>

      <div class="filter-bar">
        <div class="searchbar" style="min-width: 280px; flex: 1">
          <span style="color: var(--text-muted); display: inline-flex">
            <app-icon name="search" [size]="16" />
          </span>
          <input
            [value]="search()"
            (input)="onSearch($event)"
            placeholder="Search by name, phone, or transaction ID…"
          />
        </div>
        <select
          class="select"
          style="width: auto; min-width: 160px"
          [value]="walletFilter()"
          (change)="onWalletFilter($event)"
        >
          <option value="all">All wallets</option>
          @for (w of wallets(); track w.id) {
            <option [value]="w.id">{{ w.currency }} · {{ w.phone }}</option>
          }
        </select>
        <div class="row gap-1" style="background: var(--surface-2); padding: 3px; border-radius: 8px">
          @for (t of typeOptions; track t.id) {
            <button
              type="button"
              class="chart-tab"
              [attr.data-active]="typeFilter() === t.id"
              (click)="setTypeFilter(t.id)"
            >
              {{ t.label }}
            </button>
          }
        </div>
        <select
          class="select"
          style="width: auto; min-width: 130px"
          [value]="statusFilter()"
          (change)="onStatusFilter($event)"
        >
          <option value="all">Any status</option>
          <option value="completed">Completed</option>
          <option value="pending">Pending</option>
          <option value="failed">Failed</option>
        </select>
        <button type="button" class="btn btn-secondary btn-sm">
          <app-icon name="calendar" [size]="14" /> Date range
        </button>
      </div>

      <div class="card">
        <app-tx-table [rows]="pageRows()" (rowClick)="openDrawer($event)" />
        @if (filtered().length > pageSize) {
          <div class="pagination">
            <span class="t-small secondary">{{ rangeLabel() }}</span>
            <div class="row gap-1">
              <button
                type="button"
                class="page-btn"
                [disabled]="page() === 1"
                (click)="prevPage()"
              >
                <app-icon name="chevron-left" [size]="14" />
              </button>
              @for (n of pageNumbers(); track n) {
                <button
                  type="button"
                  class="page-btn"
                  [attr.data-active]="page() === n"
                  (click)="setPage(n)"
                >
                  {{ n }}
                </button>
              }
              <button
                type="button"
                class="page-btn"
                [disabled]="page() === totalPages()"
                (click)="nextPage()"
              >
                <app-icon name="chevron-right" [size]="14" />
              </button>
            </div>
          </div>
        }
      </div>

      <app-drawer
        [open]="!!activeTx()"
        title="Transaction details"
        [width]="460"
        (close)="closeDrawer()"
      >
        @if (activeTx(); as tx) {
          <app-tx-detail [tx]="tx" />
        }
      </app-drawer>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryComponent {
  private readonly state = inject(AppStateService);
  private readonly route = inject(ActivatedRoute);

  protected readonly wallets = this.state.wallets;
  protected readonly allTransactions = this.state.transactions;

  protected readonly walletFilter = signal('all');
  protected readonly typeFilter = signal<TypeFilter>('all');
  protected readonly statusFilter = signal('all');
  protected readonly search = signal('');
  protected readonly page = signal(1);
  protected readonly activeTx = signal<Transaction | null>(null);

  protected readonly pageSize = PAGE_SIZE;

  protected readonly typeOptions = [
    { id: 'all' as TypeFilter, label: 'All' },
    { id: 'in' as TypeFilter, label: 'Incoming' },
    { id: 'out' as TypeFilter, label: 'Outgoing' },
  ];

  private readonly initialTxId = toSignal(
    this.route.queryParamMap.pipe(map((p) => p.get('txId'))),
    { initialValue: null }
  );

  protected readonly filtered = computed(() => {
    const wf = this.walletFilter();
    const tf = this.typeFilter();
    const sf = this.statusFilter();
    const q = this.search().toLowerCase();
    return this.allTransactions().filter((t) => {
      if (wf !== 'all' && t.walletId !== wf) return false;
      if (tf === 'in' && t.type !== 'in' && t.type !== 'deposit') return false;
      if (tf === 'out' && t.type !== 'out') return false;
      if (sf !== 'all' && t.status !== sf) return false;
      if (q) {
        const hay =
          t.counter.toLowerCase() +
          ' ' +
          t.counterName.toLowerCase() +
          ' ' +
          t.id.toLowerCase();
        if (!hay.includes(q)) return false;
      }
      return true;
    });
  });

  protected readonly totalPages = computed(
    () => Math.ceil(this.filtered().length / PAGE_SIZE) || 1
  );

  protected readonly pageRows = computed(() => {
    const p = this.page();
    return this.filtered().slice((p - 1) * PAGE_SIZE, p * PAGE_SIZE);
  });

  protected readonly pageNumbers = computed(() => {
    const total = Math.min(this.totalPages(), 5);
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  protected readonly rangeLabel = computed(() => {
    const p = this.page();
    const start = (p - 1) * PAGE_SIZE + 1;
    const end = Math.min(p * PAGE_SIZE, this.filtered().length);
    return `Showing ${start}–${end} of ${this.filtered().length}`;
  });

  constructor() {
    queueMicrotask(() => {
      const id = this.initialTxId();
      if (id) {
        const tx = this.allTransactions().find((t) => t.id === id);
        if (tx) this.activeTx.set(tx);
      }
    });
  }

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
    this.page.set(1);
  }

  protected onWalletFilter(event: Event): void {
    this.walletFilter.set((event.target as HTMLSelectElement).value);
    this.page.set(1);
  }

  protected onStatusFilter(event: Event): void {
    this.statusFilter.set((event.target as HTMLSelectElement).value);
    this.page.set(1);
  }

  protected setTypeFilter(t: TypeFilter): void {
    this.typeFilter.set(t);
    this.page.set(1);
  }

  protected setPage(p: number): void {
    this.page.set(p);
  }

  protected prevPage(): void {
    this.page.update((p) => Math.max(1, p - 1));
  }

  protected nextPage(): void {
    this.page.update((p) => Math.min(this.totalPages(), p + 1));
  }

  protected openDrawer(tx: Transaction): void {
    this.activeTx.set(tx);
  }

  protected closeDrawer(): void {
    this.activeTx.set(null);
  }
}
