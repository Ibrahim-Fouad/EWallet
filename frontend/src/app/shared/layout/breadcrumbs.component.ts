import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

import { IconComponent } from '../icons/icon.component';

export interface BreadcrumbItem {
  label: string;
  link?: string | unknown[];
}

@Component({
  selector: 'app-breadcrumbs',
  imports: [IconComponent, RouterLink],
  template: `
    <div class="row gap-2 breadcrumbs">
      @for (it of items(); track it.label; let last = $last) {
        @if (it.link) {
          <a class="bc-link" [routerLink]="it.link">{{ it.label }}</a>
        } @else {
          <span style="color: var(--text-primary); font-weight: 500">{{ it.label }}</span>
        }
        @if (!last) {
          <app-icon name="chevron-right" [size]="14" style="color: var(--text-muted)" />
        }
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BreadcrumbsComponent {
  readonly items = input.required<BreadcrumbItem[]>();
}
