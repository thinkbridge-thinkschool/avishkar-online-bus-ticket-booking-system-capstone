import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { AdminService } from '../../core/services/admin.service';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './admin-layout.html',
  styleUrl: './admin-layout.css',
})
export class AdminLayoutComponent implements OnInit {
  readonly auth = inject(AuthService);
  private readonly adminService = inject(AdminService);

  readonly pendingCount = signal(0);

  async ngOnInit(): Promise<void> {
    try {
      const pending = await this.adminService.getPendingVendors();
      this.pendingCount.set(pending.length);
    } catch {
      // Non-critical — sidebar badge just stays hidden on error.
    }
  }
}
