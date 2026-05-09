import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { IconComponent } from '../icons/icon.component';

@Component({
  selector: 'app-field',
  imports: [IconComponent],
  template: `
    <div class="field">
      @if (label()) {
        <label class="field-label" [attr.for]="for()">{{ label() }}</label>
      }
      <ng-content />
      @if (error()) {
        <div class="field-error" role="alert">
          <app-icon name="alert" [size]="12" /> {{ error() }}
        </div>
      } @else if (help()) {
        <div class="field-help">{{ help() }}</div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FieldComponent {
  readonly label = input<string>();
  readonly error = input<string | null | undefined>();
  readonly help = input<string>();
  readonly for = input<string>();
}
