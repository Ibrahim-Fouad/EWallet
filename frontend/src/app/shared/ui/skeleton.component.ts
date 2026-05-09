import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-skeleton',
  template: `
    <div
      class="skeleton"
      [style.width]="w()"
      [style.height.px]="h()"
      [style.border-radius.px]="r()"
    ></div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonComponent {
  readonly w = input<string>('100%');
  readonly h = input<number>(14);
  readonly r = input<number>(6);
}
