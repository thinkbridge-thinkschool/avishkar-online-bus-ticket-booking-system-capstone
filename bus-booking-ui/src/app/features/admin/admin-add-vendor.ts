import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AdminService } from '../../core/services/admin.service';

@Component({
  selector: 'app-admin-add-vendor',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './admin-add-vendor.html',
})
export class AdminAddVendorComponent {
  private readonly fb = inject(FormBuilder);
  private readonly adminService = inject(AdminService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    userEmail: ['', [Validators.required, Validators.email]],
    vendorName: ['', Validators.required],
    phoneNumber: ['', Validators.required],
    address: ['', Validators.required],
    licenseNumber: ['', Validators.required],
  });

  async addVendor(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    try {
      await this.adminService.addVendor(this.form.getRawValue());
      void this.router.navigate(['/admin/vendors']);
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      if (status === 404) {
        this.error.set('No user with that email exists. The user must sign up first.');
      } else if (status === 409) {
        this.error.set('This user is already registered as a vendor, or the email is already in use by another vendor.');
      } else {
        this.error.set((err as Error).message);
      }
    } finally {
      this.saving.set(false);
    }
  }
}
