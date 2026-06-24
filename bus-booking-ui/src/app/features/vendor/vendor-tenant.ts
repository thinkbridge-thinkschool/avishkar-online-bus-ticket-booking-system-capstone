import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TenantService } from '../../core/services/tenant.service';
import { AuthService } from '../../core/services/auth.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Tenant } from '../../shared/models/tenant.model';

@Component({
  selector: 'app-vendor-tenant',
  imports: [RouterLink, DatePipe, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './vendor-tenant.html',
})
export class VendorTenantComponent implements OnInit {
  private readonly tenantService = inject(TenantService);
  private readonly auth = inject(AuthService);

  readonly tenant = signal<Tenant | null>(null);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly formError = signal<string | null>(null);

  // Form fields
  readonly name = signal('');
  readonly subdomain = signal('');

  readonly adminEmail = this.auth.email;

  async ngOnInit(): Promise<void> {
    try {
      this.tenant.set(await this.tenantService.getMyTenant());
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  async register(): Promise<void> {
    this.formError.set(null);
    const n = this.name().trim();
    const s = this.subdomain().trim().toLowerCase();
    const email = this.adminEmail() ?? '';

    if (!n) { this.formError.set('Company name is required.'); return; }
    if (!s) { this.formError.set('Subdomain is required.'); return; }

    this.submitting.set(true);
    try {
      await this.tenantService.registerTenant({ name: n, subdomain: s, adminEmail: email });
      this.tenant.set(await this.tenantService.getMyTenant());
    } catch (err: unknown) {
      this.formError.set((err as Error).message);
    } finally {
      this.submitting.set(false);
    }
  }

  setName(value: string): void  { this.name.set(value); }
  setSubdomain(value: string): void { this.subdomain.set(value.toLowerCase()); }
}
