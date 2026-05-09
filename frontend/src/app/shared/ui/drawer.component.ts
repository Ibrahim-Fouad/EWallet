import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { IconComponent } from '../icons/icon.component';

@Component({
  selector: 'app-drawer',
  imports: [IconComponent],
  template: `
    @if (open()) {
      <div class="modal-overlay" (click)="close.emit()">
        <div class="drawer-card" [style.width.px]="width()" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <div class="t-h2">{{ title() }}</div>
            <button type="button" class="icon-btn" (click)="close.emit()" aria-label="Close">
              <app-icon name="x" [size]="18" />
            </button>
          </div>
          <div class="drawer-body">
            <ng-content />
          </div>
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DrawerComponent {
  readonly open = input<boolean>(false);
  readonly title = input<string>('');
  readonly width = input<number>(480);
  readonly close = output<void>();
}
