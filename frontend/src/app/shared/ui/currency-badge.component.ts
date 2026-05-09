import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-currency-badge',
  template: `<span [class]="cls()">{{ currency() }}</span>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CurrencyBadgeComponent {
  readonly currency = input.required<string>();

  protected readonly cls = computed(
    () => `currency-badge currency-${this.currency().toLowerCase()}`
  );
}
