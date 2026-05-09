import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent, IconName } from '../icons/icon.component';

@Component({
  selector: 'app-stat-card',
  imports: [IconComponent],
  template: `
    <div class="stat-card">
      <div class="stat-card-icon" style="color: var(--primary)">
        <app-icon [name]="icon()" [size]="18" />
      </div>
      <div class="t-small secondary">{{ label() }}</div>
      <div class="t-num" style="font-size: 24px; font-weight: 600; margin-top: 2px">
        {{ value() }}
      </div>
      @if (trend()) {
        <div [class]="'stat-trend ' + (trendDir() === 'up' ? 'up' : 'down')">
          <app-icon [name]="trendDir() === 'up' ? 'trend-up' : 'trend-down'" [size]="12" />
          {{ trend() }}
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatCardComponent {
  readonly icon = input.required<IconName>();
  readonly label = input.required<string>();
  readonly value = input.required<string>();
  readonly trend = input<string>();
  readonly trendDir = input<'up' | 'down'>('up');
}
