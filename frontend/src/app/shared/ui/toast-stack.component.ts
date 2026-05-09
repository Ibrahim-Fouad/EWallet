import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';

import { AppStateService, ToastKind } from '../../core/services/app-state.service';
import { IconComponent, IconName } from '../icons/icon.component';

const TOAST_ICONS: Record<ToastKind, IconName> = {
  received: 'arrow-down',
  success: 'check',
  error: 'x',
  info: 'info',
};

@Component({
  selector: 'app-toast-stack',
  imports: [IconComponent],
  template: `
    <div class="toast-stack">
      @for (t of toasts(); track t.id) {
        <div [class]="'toast toast-' + t.kind">
          <div class="toast-icon" style="color: #fff">
            <app-icon [name]="iconFor(t.kind)" [size]="16" />
          </div>
          <div class="grow">
            <div style="font-weight: 500; font-size: 13px">{{ t.title }}</div>
            @if (t.body) {
              <div class="t-small secondary">{{ t.body }}</div>
            }
          </div>
          <button type="button" class="icon-btn" (click)="dismiss(t.id)" aria-label="Dismiss">
            <app-icon name="x" [size]="14" />
          </button>
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastStackComponent {
  private readonly state = inject(AppStateService);

  protected readonly toasts = this.state.toasts;

  protected iconFor(kind: ToastKind): IconName {
    return TOAST_ICONS[kind];
  }

  protected dismiss(id: string): void {
    this.state.dismissToast(id);
  }
}
