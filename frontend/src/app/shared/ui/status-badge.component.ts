import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type Status = 'completed' | 'pending' | 'failed' | 'active' | 'inactive';

const STATUS_MAP: Record<Status, { cls: string; label: string }> = {
  completed: { cls: 'badge-success', label: 'Completed' },
  pending: { cls: 'badge-warning', label: 'Pending' },
  failed: { cls: 'badge-danger', label: 'Failed' },
  active: { cls: 'badge-success', label: 'Active' },
  inactive: { cls: 'badge-neutral', label: 'Inactive' },
};

@Component({
  selector: 'app-status-badge',
  template: `
    <span class="badge" [class]="'badge ' + cls()">
      <span class="badge-dot"></span>
      {{ label() }}
    </span>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusBadgeComponent {
  readonly status = input.required<Status>();

  protected readonly cls = computed(() => STATUS_MAP[this.status()]?.cls ?? 'badge-neutral');
  protected readonly label = computed(() => STATUS_MAP[this.status()]?.label ?? this.status());
}
