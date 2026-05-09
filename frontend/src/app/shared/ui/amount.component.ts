import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type AmountType = 'in' | 'out' | 'deposit';

@Component({
  selector: 'app-amount',
  template: `
    <span [class]="cls()">
      {{ sign() }}{{ formatted() }}<span class="amount-currency"> {{ currency() }}</span>
    </span>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AmountComponent {
  readonly value = input.required<number>();
  readonly currency = input.required<string>();
  readonly type = input<AmountType>('out');
  readonly large = input<boolean>(false);

  protected readonly positive = computed(
    () => this.type() === 'in' || this.type() === 'deposit'
  );
  protected readonly sign = computed(() => (this.positive() ? '+' : '−'));
  protected readonly cls = computed(() => {
    const tone = this.positive() ? 'amt-positive' : 'amt-negative';
    const size = this.large() ? 'amount-lg' : '';
    return `amount t-num ${tone} ${size}`.trim();
  });
  protected readonly formatted = computed(() =>
    this.value().toLocaleString('en-US', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })
  );
}
