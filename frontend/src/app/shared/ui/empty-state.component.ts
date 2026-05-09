import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent, IconName } from '../icons/icon.component';

@Component({
  selector: 'app-empty-state',
  imports: [IconComponent],
  template: `
    <div class="empty-state">
      <div class="empty-state-icon" style="color: var(--text-muted)">
        <app-icon [name]="icon()" [size]="28" />
      </div>
      <div class="t-h3" style="margin-top: 12px">{{ title() }}</div>
      <div class="t-small secondary" style="margin-top: 4px; max-width: 280px; text-align: center">
        {{ body() }}
      </div>
      <div style="margin-top: 16px">
        <ng-content />
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmptyStateComponent {
  readonly icon = input<IconName>('wallet');
  readonly title = input.required<string>();
  readonly body = input<string>('');
}
