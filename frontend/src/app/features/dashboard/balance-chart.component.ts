import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';

import { Transaction } from '../../core/services/app-state.service';
import { IconComponent } from '../../shared/icons/icon.component';

const W = 720;
const H = 200;
const GRID_LINES = 4;

interface ChartGeometry {
  line: string;
  area: string;
  endX: number;
  endY: number;
}

@Component({
  selector: 'app-balance-chart',
  imports: [IconComponent],
  template: `
    <div class="chart-card">
      <div class="row between" style="margin-bottom: 16px">
        <div>
          <div class="t-h3">Total balance over time</div>
          <div class="row gap-2" style="margin-top: 6px">
            <span class="t-num" style="font-size: 24px; font-weight: 700">{{ formattedTotal() }}</span>
            <span class="t-small secondary" style="margin-left: 4px">net flow</span>
          </div>
        </div>
        <div class="chart-tabs">
          @for (k of ranges; track k) {
            <button
              type="button"
              class="chart-tab"
              [attr.data-active]="range() === k"
              (click)="setRange(k)"
            >
              {{ k.toUpperCase() }}
            </button>
          }
        </div>
      </div>

      <svg
        [attr.viewBox]="'0 0 ' + width + ' ' + height"
        width="100%"
        [attr.height]="height"
        preserveAspectRatio="none"
      >
        <defs>
          <linearGradient id="dashArea" x1="0" x2="0" y1="0" y2="1">
            <stop offset="0%" stop-color="var(--primary)" stop-opacity="0.2" />
            <stop offset="100%" stop-color="var(--primary)" stop-opacity="0" />
          </linearGradient>
        </defs>
        @for (i of gridIndexes; track i) {
          <line
            [attr.x1]="0"
            [attr.x2]="width"
            [attr.y1]="(height / gridLines) * (i + 0.5)"
            [attr.y2]="(height / gridLines) * (i + 0.5)"
            stroke="var(--border)"
            stroke-dasharray="4 4"
          />
        }
        <path [attr.d]="geometry().area" fill="url(#dashArea)" />
        <path
          [attr.d]="geometry().line"
          fill="none"
          stroke="var(--primary)"
          stroke-width="2.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
        <circle [attr.cx]="geometry().endX" [attr.cy]="geometry().endY" r="5" fill="var(--primary)" />
        <circle
          [attr.cx]="geometry().endX"
          [attr.cy]="geometry().endY"
          r="9"
          fill="var(--primary)"
          fill-opacity="0.2"
        />
      </svg>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BalanceChartComponent {
  readonly transactions = input<Transaction[]>([]);

  protected readonly width = W;
  protected readonly height = H;
  protected readonly gridLines = GRID_LINES;
  protected readonly gridIndexes = Array.from({ length: GRID_LINES }, (_, i) => i);
  protected readonly ranges = ['7d', '30d', '90d'] as const;

  protected readonly range = signal<'7d' | '30d' | '90d'>('30d');

  protected readonly points = computed(() => {
    const txs = this.transactions();
    const r = this.range();
    const days = r === '7d' ? 7 : r === '30d' ? 30 : 90;
    const now = new Date();
    return Array.from({ length: days }, (_, i) => {
      const d = new Date(now.getFullYear(), now.getMonth(), now.getDate() - (days - 1 - i));
      const dayStr = d.toISOString().slice(0, 10);
      return txs
        .filter((tx) => tx.at.startsWith(dayStr) && tx.status === 'completed')
        .reduce(
          (sum, tx) => sum + (tx.type === 'in' || tx.type === 'deposit' ? tx.amount : -tx.amount),
          0
        );
    });
  });

  protected readonly geometry = computed<ChartGeometry>(() => {
    const points = this.points();
    const max = Math.max(...points);
    const min = Math.min(...points);
    const range = max - min || 1;
    const coords = points.map(
      (v, i) =>
        [
          (i / (points.length - 1)) * W,
          H - ((v - min) / range) * (H - 30) - 15,
        ] as const
    );
    const line = coords.map((p, i) => (i === 0 ? 'M' : 'L') + p[0] + ',' + p[1]).join(' ');
    const last = coords[coords.length - 1];
    return {
      line,
      area: `${line} L${W},${H} L0,${H} Z`,
      endX: last[0],
      endY: last[1],
    };
  });

  protected readonly formattedTotal = computed(() => {
    const txs = this.transactions();
    const r = this.range();
    const days = r === '7d' ? 7 : r === '30d' ? 30 : 90;
    const now = new Date();
    const cutoff = new Date(now.getFullYear(), now.getMonth(), now.getDate() - days);
    const net = txs
      .filter((tx) => new Date(tx.at) >= cutoff && tx.status === 'completed')
      .reduce(
        (sum, tx) => sum + (tx.type === 'in' || tx.type === 'deposit' ? tx.amount : -tx.amount),
        0
      );
    return net.toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  });

  protected setRange(r: '7d' | '30d' | '90d'): void {
    this.range.set(r);
  }
}
