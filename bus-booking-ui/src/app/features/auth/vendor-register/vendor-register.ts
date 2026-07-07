import { Component, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { VendorService } from '../../../core/services/vendor.service';

@Component({
  selector: 'app-vendor-register',
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './vendor-register.html',
  styleUrl: './vendor-register.css',
})
export class VendorRegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly vendorService = inject(VendorService);
  private readonly router = inject(Router);

  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly submitted = signal(false);

  readonly form = this.fb.nonNullable.group({
    vendorName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', Validators.required],
    password: ['', [Validators.required, Validators.minLength(8)]],
    confirmPassword: ['', Validators.required],
    address: ['', Validators.required],
    licenseNumber: ['', Validators.required],
  });

  async submit(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const { password, confirmPassword } = this.form.getRawValue();
    if (password !== confirmPassword) {
      this.error.set('Passwords do not match.');
      return;
    }

    this.saving.set(true);
    this.error.set(null);
    try {
      await this.vendorService.registerNew(this.form.getRawValue());
      this.submitted.set(true);
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      if (status === 409) {
        this.error.set('An account or vendor with this email already exists.');
      } else if (status === 400) {
        this.error.set((err as Error).message || 'Please check your details and try again.');
      } else {
        this.error.set('Registration failed. Please try again.');
      }
    } finally {
      this.saving.set(false);
    }
  }

  goToLogin(): void {
    void this.router.navigate(['/vendor/login']);
  }
}
