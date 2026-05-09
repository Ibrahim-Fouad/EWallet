import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { SidebarComponent } from './sidebar.component';
import { TopBarComponent } from './topbar.component';

@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, SidebarComponent, TopBarComponent],
  template: `
    <div class="app-shell">
      <app-sidebar />
      <div class="col" style="min-width: 0">
        <app-topbar />
        <main
          class="grow"
          style="background: var(--surface-2); min-height: calc(100vh - 72px)"
        >
          <router-outlet />
        </main>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent {}
