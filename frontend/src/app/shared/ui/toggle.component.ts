import { ChangeDetectionStrategy, Component, forwardRef, input, signal } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

@Component({
  selector: 'app-toggle',
  template: `
    <label class="toggle">
      <input
        type="checkbox"
        [checked]="checked()"
        [disabled]="disabled()"
        (change)="handleChange($event)"
        (blur)="onTouched()"
      />
      <span class="toggle-track"><span class="toggle-thumb"></span></span>
      @if (label()) {
        <span class="toggle-label">{{ label() }}</span>
      }
    </label>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => ToggleComponent),
      multi: true,
    },
  ],
})
export class ToggleComponent implements ControlValueAccessor {
  readonly label = input<string>();

  protected readonly checked = signal(false);
  protected readonly disabled = signal(false);

  private onChange: (value: boolean) => void = () => {};
  protected onTouched: () => void = () => {};

  protected handleChange(event: Event): void {
    const value = (event.target as HTMLInputElement).checked;
    this.checked.set(value);
    this.onChange(value);
  }

  writeValue(value: boolean): void {
    this.checked.set(!!value);
  }
  registerOnChange(fn: (value: boolean) => void): void {
    this.onChange = fn;
  }
  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }
  setDisabledState(isDisabled: boolean): void {
    this.disabled.set(isDisabled);
  }
}
