import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type IconName =
  | 'wallet' | 'home' | 'send' | 'history' | 'bell' | 'user' | 'settings'
  | 'plus' | 'arrow-down' | 'arrow-up' | 'arrow-right' | 'arrow-left'
  | 'check' | 'check-circle' | 'x' | 'x-circle' | 'search' | 'eye' | 'eye-off'
  | 'mail' | 'lock' | 'phone' | 'trend-up' | 'trend-down'
  | 'filter' | 'calendar' | 'chevron-down' | 'chevron-right' | 'chevron-left'
  | 'more' | 'dot' | 'logout' | 'shield' | 'info' | 'alert' | 'copy'
  | 'sparkle' | 'camera' | 'edit'
  | 'badge-check' | 'clock-x';

@Component({
  selector: 'app-icon',
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      [attr.stroke-width]="strokeWidth()"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
    >
      @switch (name()) {
        @case ('wallet') {
          <path d="M3 7a2 2 0 0 1 2-2h13a1 1 0 0 1 1 1v3" />
          <path d="M3 7v11a2 2 0 0 0 2 2h15a1 1 0 0 0 1-1v-3" />
          <path d="M21 10h-4a2 2 0 0 0 0 4h4z" />
        }
        @case ('home') {
          <path d="M3 11.5 12 4l9 7.5" />
          <path d="M5 10v10h14V10" />
        }
        @case ('send') {
          <path d="M21 3 11 13" />
          <path d="m21 3-7 18-3-8-8-3z" />
        }
        @case ('history') {
          <path d="M3 12a9 9 0 1 0 3-6.7" />
          <path d="M3 4v5h5" />
          <path d="M12 7v5l3 2" />
        }
        @case ('bell') {
          <path d="M6 8a6 6 0 0 1 12 0c0 7 3 9 3 9H3s3-2 3-9" />
          <path d="M10 21a2 2 0 0 0 4 0" />
        }
        @case ('user') {
          <circle cx="12" cy="8" r="4" />
          <path d="M4 21a8 8 0 0 1 16 0" />
        }
        @case ('settings') {
          <circle cx="12" cy="12" r="3" />
          <path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z" />
        }
        @case ('plus') {
          <path d="M12 5v14" />
          <path d="M5 12h14" />
        }
        @case ('arrow-down') {
          <path d="M12 5v14" />
          <path d="m19 12-7 7-7-7" />
        }
        @case ('arrow-up') {
          <path d="M12 19V5" />
          <path d="m5 12 7-7 7 7" />
        }
        @case ('arrow-right') {
          <path d="M5 12h14" />
          <path d="m12 5 7 7-7 7" />
        }
        @case ('arrow-left') {
          <path d="M19 12H5" />
          <path d="m12 19-7-7 7-7" />
        }
        @case ('check') {
          <path d="M20 6 9 17l-5-5" />
        }
        @case ('check-circle') {
          <circle cx="12" cy="12" r="10" />
          <path d="m9 12 2 2 4-4" />
        }
        @case ('x') {
          <path d="M18 6 6 18" />
          <path d="m6 6 12 12" />
        }
        @case ('x-circle') {
          <circle cx="12" cy="12" r="10" />
          <path d="M15 9 9 15" />
          <path d="m9 9 6 6" />
        }
        @case ('search') {
          <circle cx="11" cy="11" r="7" />
          <path d="m20 20-3.5-3.5" />
        }
        @case ('eye') {
          <path d="M2 12s4-7 10-7 10 7 10 7-4 7-10 7S2 12 2 12Z" />
          <circle cx="12" cy="12" r="3" />
        }
        @case ('eye-off') {
          <path d="M9.9 5.1A10 10 0 0 1 12 5c6 0 10 7 10 7a16 16 0 0 1-3.3 3.9" />
          <path d="M6.6 6.6A16 16 0 0 0 2 12s4 7 10 7c1.7 0 3.3-.4 4.6-1" />
          <path d="m2 2 20 20" />
          <path d="M14.1 14.1a3 3 0 0 1-4.2-4.2" />
        }
        @case ('mail') {
          <rect x="3" y="5" width="18" height="14" rx="2" />
          <path d="m3 7 9 6 9-6" />
        }
        @case ('lock') {
          <rect x="4" y="11" width="16" height="10" rx="2" />
          <path d="M8 11V8a4 4 0 0 1 8 0v3" />
        }
        @case ('phone') {
          <path d="M22 16.9v3a2 2 0 0 1-2.2 2 19.8 19.8 0 0 1-8.6-3.1 19.5 19.5 0 0 1-6-6 19.8 19.8 0 0 1-3.1-8.7A2 2 0 0 1 4.1 2h3a2 2 0 0 1 2 1.7c.1 1 .4 1.9.7 2.8a2 2 0 0 1-.5 2.1L8 9.9a16 16 0 0 0 6 6l1.3-1.3a2 2 0 0 1 2.1-.5c.9.3 1.8.6 2.8.7a2 2 0 0 1 1.7 2z" />
        }
        @case ('trend-up') {
          <path d="m3 17 6-6 4 4 8-8" />
          <path d="M14 7h7v7" />
        }
        @case ('trend-down') {
          <path d="m3 7 6 6 4-4 8 8" />
          <path d="M14 17h7v-7" />
        }
        @case ('filter') {
          <path d="M22 3H2l8 9.5V19l4 2v-8.5z" />
        }
        @case ('calendar') {
          <rect x="3" y="5" width="18" height="16" rx="2" />
          <path d="M16 3v4M8 3v4M3 10h18" />
        }
        @case ('chevron-down') {
          <path d="m6 9 6 6 6-6" />
        }
        @case ('chevron-right') {
          <path d="m9 6 6 6-6 6" />
        }
        @case ('chevron-left') {
          <path d="m15 6-6 6 6 6" />
        }
        @case ('more') {
          <circle cx="12" cy="12" r="1" />
          <circle cx="19" cy="12" r="1" />
          <circle cx="5" cy="12" r="1" />
        }
        @case ('dot') {
          <circle cx="12" cy="12" r="4" fill="currentColor" stroke="none" />
        }
        @case ('logout') {
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
          <path d="m16 17 5-5-5-5" />
          <path d="M21 12H9" />
        }
        @case ('shield') {
          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10Z" />
        }
        @case ('info') {
          <circle cx="12" cy="12" r="10" />
          <path d="M12 16v-4M12 8h.01" />
        }
        @case ('alert') {
          <path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0Z" />
          <path d="M12 9v4M12 17h.01" />
        }
        @case ('copy') {
          <rect x="9" y="9" width="13" height="13" rx="2" />
          <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
        }
        @case ('sparkle') {
          <path d="m12 3 1.9 5.6L19 10l-5.1 1.4L12 17l-1.9-5.6L5 10l5.1-1.4z" />
        }
        @case ('camera') {
          <path d="M21 19a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h3l2-3h4l2 3h3a2 2 0 0 1 2 2z" />
          <circle cx="12" cy="13" r="4" />
        }
        @case ('edit') {
          <path d="M12 20h9" />
          <path d="M16.5 3.5a2.1 2.1 0 1 1 3 3L7 19l-4 1 1-4z" />
        }
        @case ('badge-check') {
          <path d="M3.85 8.62a4 4 0 0 1 4.78-4.77 4 4 0 0 1 6.74 0 4 4 0 0 1 4.78 4.78 4 4 0 0 1 0 6.74 4 4 0 0 1-4.77 4.78 4 4 0 0 1-6.75 0 4 4 0 0 1-4.78-4.77 4 4 0 0 1 0-6.76Z" />
          <path d="m9 12 2 2 4-4" />
        }
        @case ('clock-x') {
          <path d="M12 2a10 10 0 1 0 7.38 16.66" />
          <path d="M12 6v6l3 3" />
          <path d="m17 17 5 5" />
          <path d="m22 17-5 5" />
        }
      }
    </svg>
  `,
  styles: [`:host { display: inline-flex; line-height: 0; }`],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IconComponent {
  readonly name = input.required<IconName>();
  readonly size = input<number>(18);
  readonly strokeWidth = input<number>(2);
}
