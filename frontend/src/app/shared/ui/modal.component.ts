import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

import { IconComponent } from '../icons/icon.component';

@Component({
  selector: 'app-modal',
  imports: [IconComponent],
  template: `
    @if (open()) {
      <div class="modal-overlay" (click)="close.emit()">
        <div class="modal-card" [style.width.px]="width()" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <div class="t-h2">{{ title() }}</div>
            <button type="button" class="icon-btn" (click)="close.emit()" aria-label="Close">
              <app-icon name="x" [size]="18" />
            </button>
          </div>
          <div class="modal-body">
            <ng-content />
          </div>
          <ng-content select="[modal-footer]" />
        </div>
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ModalComponent {
  readonly open = input<boolean>(false);
  readonly title = input<string>('');
  readonly width = input<number>(480);
  readonly close = output<void>();
}
